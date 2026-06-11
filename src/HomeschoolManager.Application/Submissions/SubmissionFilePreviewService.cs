using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;

namespace HomeschoolManager.Application.Submissions;

public sealed class SubmissionFilePreviewService
{
    private readonly IHomeschoolRepository repository;
    private readonly ISubmissionFileStore fileStore;

    public SubmissionFilePreviewService(IHomeschoolRepository repository, ISubmissionFileStore fileStore)
    {
        this.repository = repository;
        this.fileStore = fileStore;
    }

    public async Task<OperationResult<SubmissionFileResponse>> GetPreviewAsync(
        UserContext user,
        Guid submissionId,
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var raw = await GetRawFileAsync(user, submissionId, fileId, cancellationToken);
        if (!raw.Succeeded)
        {
            return OperationResult<SubmissionFileResponse>.Failure(raw.Errors.ToArray());
        }

        var file = raw.Value!;
        if (IsPdf(file))
        {
            return OperationResult<SubmissionFileResponse>.Success(file);
        }

        if (IsImage(file))
        {
            return OperationResult<SubmissionFileResponse>.Success(file);
        }

        if (IsMarkdown(file))
        {
            return OperationResult<SubmissionFileResponse>.Success(new SubmissionFileResponse(
                $"{Path.GetFileNameWithoutExtension(file.OriginalFileName)}-preview.html",
                "text/html",
                Encoding.UTF8.GetBytes(MarkdownPreview.CreateHtml(file.OriginalFileName, DecodeText(file.Content))),
                true));
        }

        var previewText = TryExtractPreviewText(file);
        var previewPdf = SimplePdf.CreateTextPreview(
            file.OriginalFileName,
            previewText ?? "This file is attached to the submission, but an in-browser preview is not available yet. Use Download to open it with the matching app on this computer.");

        return OperationResult<SubmissionFileResponse>.Success(new SubmissionFileResponse(
            $"{Path.GetFileNameWithoutExtension(file.OriginalFileName)}-preview.pdf",
            "application/pdf",
            previewPdf,
            true));
    }

    public async Task<OperationResult<SubmissionFileResponse>> GetDownloadAsync(
        UserContext user,
        Guid submissionId,
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        return await GetRawFileAsync(user, submissionId, fileId, cancellationToken);
    }

    private async Task<OperationResult<SubmissionFileResponse>> GetRawFileAsync(
        UserContext user,
        Guid submissionId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<SubmissionFileResponse>.Failure(authorized.Errors.ToArray());
        }

        var submission = await repository.GetAssignmentSubmissionAsync(submissionId, cancellationToken);
        if (submission is null)
        {
            return OperationResult<SubmissionFileResponse>.Failure("Submission was not found.");
        }

        var file = submission.Attachments.FirstOrDefault(attachment => attachment.Id == fileId);
        if (file is null)
        {
            return OperationResult<SubmissionFileResponse>.Failure("Attachment was not found for this submission.");
        }

        var content = await fileStore.ReadStoredFileAsync(file, cancellationToken);
        if (content is null)
        {
            return OperationResult<SubmissionFileResponse>.Failure("Attachment file was not found in local storage.");
        }

        return OperationResult<SubmissionFileResponse>.Success(new SubmissionFileResponse(
            content.OriginalFileName,
            content.ContentType,
            content.Content,
            false));
    }

    private static bool IsPdf(SubmissionFileResponse file)
    {
        return file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
            file.OriginalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImage(SubmissionFileResponse file)
    {
        return file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextLike(SubmissionFileResponse file)
    {
        var name = file.OriginalFileName;
        var contentType = file.ContentType;
        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".htm", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMarkdown(SubmissionFileResponse file)
    {
        return file.ContentType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase) ||
            file.OriginalFileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            file.OriginalFileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryExtractPreviewText(SubmissionFileResponse file)
    {
        if (IsTextLike(file))
        {
            return DecodeText(file.Content);
        }

        if (file.OriginalFileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return TryExtractZippedXmlText(file.Content, "word/document.xml");
        }

        if (file.OriginalFileName.EndsWith(".odt", StringComparison.OrdinalIgnoreCase))
        {
            return TryExtractZippedXmlText(file.Content, "content.xml");
        }

        if (file.OriginalFileName.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase))
        {
            return PlainTextFromRtf(DecodeText(file.Content));
        }

        return null;
    }

    private static string? TryExtractZippedXmlText(byte[] content, string entryName)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = archive.GetEntry(entryName);
            if (entry is null)
            {
                return null;
            }

            using var entryStream = entry.Open();
            var document = XDocument.Load(entryStream);
            var text = string.Join(" ", document.DescendantNodes().OfType<XText>().Select(node => node.Value.Trim()));
            return string.IsNullOrWhiteSpace(text) ? null : Regex.Replace(text, @"\s+", " ").Trim();
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    private static string PlainTextFromRtf(string rtf)
    {
        var withoutEscapedCharacters = Regex.Replace(rtf, @"\\'[0-9a-fA-F]{2}", " ");
        var withoutControlWords = Regex.Replace(withoutEscapedCharacters, @"\\[a-zA-Z]+\d* ?", " ");
        var withoutSymbols = withoutControlWords
            .Replace(@"\{", "{", StringComparison.Ordinal)
            .Replace(@"\}", "}", StringComparison.Ordinal)
            .Replace(@"\~", " ", StringComparison.Ordinal);
        var plain = Regex.Replace(withoutSymbols, "[{}]", " ");
        return Regex.Replace(plain, @"\s+", " ").Trim();
    }

    private static string DecodeText(byte[] content)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Default.GetString(content);
        }
    }

    private static class SimplePdf
    {
        public static byte[] CreateTextPreview(string title, string text)
        {
            var lines = WrapLines($"{title}\n\n{text}".Replace("\r\n", "\n").Replace('\r', '\n'), 86);
            var pages = lines.Chunk(48).Select(pageLines => pageLines.ToArray()).ToArray();
            if (pages.Length == 0)
            {
                pages = [[]];
            }

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                ""
            };

            var pageObjectNumbers = new List<int>();
            for (var index = 0; index < pages.Length; index++)
            {
                var contentObjectNumber = 3 + index * 2;
                var pageObjectNumber = contentObjectNumber + 1;
                pageObjectNumbers.Add(pageObjectNumber);
                objects.Add(CreateContentObject(pages[index]));
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {3 + pages.Length * 2} 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pageObjectNumbers.Count} >>";
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            return BuildPdf(objects);
        }

        private static string CreateContentObject(IReadOnlyList<string> lines)
        {
            var builder = new StringBuilder();
            builder.AppendLine("BT");
            builder.AppendLine("/F1 11 Tf");
            builder.AppendLine("50 742 Td");
            builder.AppendLine("14 TL");
            foreach (var line in lines)
            {
                builder.Append('(').Append(EscapePdf(line)).AppendLine(") Tj");
                builder.AppendLine("T*");
            }

            builder.AppendLine("ET");
            var stream = builder.ToString();
            return $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream";
        }

        private static byte[] BuildPdf(IReadOnlyList<string> objects)
        {
            var builder = new StringBuilder();
            var offsets = new List<int> { 0 };
            builder.AppendLine("%PDF-1.4");
            for (var index = 0; index < objects.Count; index++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
                builder.Append(index + 1).AppendLine(" 0 obj");
                builder.AppendLine(objects[index]);
                builder.AppendLine("endobj");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
            builder.AppendLine("xref");
            builder.Append("0 ").AppendLine((objects.Count + 1).ToString());
            builder.AppendLine("0000000000 65535 f ");
            foreach (var offset in offsets.Skip(1))
            {
                builder.Append(offset.ToString("D10")).AppendLine(" 00000 n ");
            }

            builder.AppendLine("trailer");
            builder.Append("<< /Size ").Append(objects.Count + 1).AppendLine(" /Root 1 0 R >>");
            builder.AppendLine("startxref");
            builder.AppendLine(xrefOffset.ToString());
            builder.AppendLine("%%EOF");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private static IReadOnlyList<string> WrapLines(string text, int maxLength)
        {
            var lines = new List<string>();
            foreach (var sourceLine in text.Split('\n'))
            {
                var words = sourceLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    lines.Add("");
                    continue;
                }

                var current = new StringBuilder();
                foreach (var word in words)
                {
                    var safeWord = ToPdfText(word);
                    if (current.Length > 0 && current.Length + safeWord.Length + 1 > maxLength)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }

                    if (safeWord.Length > maxLength)
                    {
                        foreach (var chunk in safeWord.Chunk(maxLength))
                        {
                            if (current.Length > 0)
                            {
                                lines.Add(current.ToString());
                                current.Clear();
                            }

                            lines.Add(new string(chunk));
                        }

                        continue;
                    }

                    if (current.Length > 0)
                    {
                        current.Append(' ');
                    }

                    current.Append(safeWord);
                }

                lines.Add(current.ToString());
            }

            return lines;
        }

        private static string EscapePdf(string value)
        {
            return ToPdfText(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private static string ToPdfText(string value)
        {
            var characters = value.Select(character => character is >= ' ' and <= '~' ? character : '?').ToArray();
            return new string(characters);
        }
    }

    private static class MarkdownPreview
    {
        public static string CreateHtml(string title, string markdown)
        {
            var body = RenderBlocks(markdown.Replace("\r\n", "\n").Replace('\r', '\n'));
            var safeTitle = WebUtility.HtmlEncode(title);
            return $$"""
                <!doctype html>
                <html lang="en">
                <head>
                    <meta charset="utf-8">
                    <title>{{safeTitle}}</title>
                    <style>
                        :root {
                            color-scheme: light;
                            font-family: "Segoe UI", Arial, sans-serif;
                            color: #1f1933;
                            background: #ffffff;
                        }

                        body {
                            margin: 0;
                            padding: 2rem;
                            line-height: 1.6;
                        }

                        h1, h2, h3, h4, h5, h6 {
                            margin: 1.4rem 0 .65rem;
                            line-height: 1.2;
                            color: #21013f;
                        }

                        h1 {
                            font-size: 2rem;
                            border-bottom: 1px solid #ded2f3;
                            padding-bottom: .45rem;
                        }

                        h2 { font-size: 1.55rem; }
                        h3 { font-size: 1.25rem; }
                        p { margin: 0 0 1rem; }
                        ul, ol { margin: 0 0 1rem 1.5rem; padding: 0; }
                        li { margin: .25rem 0; }
                        blockquote {
                            margin: 0 0 1rem;
                            padding: .75rem 1rem;
                            border-left: 4px solid #5f259f;
                            background: #f7f2ff;
                        }
                        code {
                            font-family: Consolas, "Courier New", monospace;
                            background: #f3eefb;
                            border-radius: 4px;
                            padding: .1rem .3rem;
                        }
                        pre {
                            overflow-x: auto;
                            padding: 1rem;
                            border: 1px solid #ded2f3;
                            border-radius: 8px;
                            background: #fbf9ff;
                        }
                        pre code {
                            background: transparent;
                            padding: 0;
                        }
                    </style>
                </head>
                <body>
                {{body}}
                </body>
                </html>
                """;
        }

        private static string RenderBlocks(string markdown)
        {
            var html = new StringBuilder();
            var paragraph = new List<string>();
            var inCodeBlock = false;
            var code = new StringBuilder();
            string? listType = null;

            void FlushParagraph()
            {
                if (paragraph.Count == 0)
                {
                    return;
                }

                html.Append("<p>").Append(RenderInline(string.Join(' ', paragraph))).AppendLine("</p>");
                paragraph.Clear();
            }

            void FlushList()
            {
                if (listType is null)
                {
                    return;
                }

                html.Append("</").Append(listType).AppendLine(">");
                listType = null;
            }

            foreach (var sourceLine in markdown.Split('\n'))
            {
                var line = sourceLine.TrimEnd();
                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    FlushParagraph();
                    FlushList();
                    if (inCodeBlock)
                    {
                        html.Append("<pre><code>").Append(WebUtility.HtmlEncode(code.ToString().TrimEnd())).AppendLine("</code></pre>");
                        code.Clear();
                    }

                    inCodeBlock = !inCodeBlock;
                    continue;
                }

                if (inCodeBlock)
                {
                    code.AppendLine(line);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph();
                    FlushList();
                    continue;
                }

                var heading = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
                if (heading.Success)
                {
                    FlushParagraph();
                    FlushList();
                    var level = heading.Groups[1].Value.Length;
                    html.Append("<h").Append(level).Append('>')
                        .Append(RenderInline(heading.Groups[2].Value.Trim()))
                        .Append("</h").Append(level).AppendLine(">");
                    continue;
                }

                var unordered = Regex.Match(line, @"^\s*[-*+]\s+(.+)$");
                if (unordered.Success)
                {
                    FlushParagraph();
                    if (listType != "ul")
                    {
                        FlushList();
                        listType = "ul";
                        html.AppendLine("<ul>");
                    }

                    html.Append("<li>").Append(RenderInline(unordered.Groups[1].Value.Trim())).AppendLine("</li>");
                    continue;
                }

                var ordered = Regex.Match(line, @"^\s*\d+\.\s+(.+)$");
                if (ordered.Success)
                {
                    FlushParagraph();
                    if (listType != "ol")
                    {
                        FlushList();
                        listType = "ol";
                        html.AppendLine("<ol>");
                    }

                    html.Append("<li>").Append(RenderInline(ordered.Groups[1].Value.Trim())).AppendLine("</li>");
                    continue;
                }

                if (line.TrimStart().StartsWith(">", StringComparison.Ordinal))
                {
                    FlushParagraph();
                    FlushList();
                    html.Append("<blockquote>").Append(RenderInline(line.TrimStart().TrimStart('>').Trim())).AppendLine("</blockquote>");
                    continue;
                }

                FlushList();
                paragraph.Add(line.Trim());
            }

            if (inCodeBlock)
            {
                html.Append("<pre><code>").Append(WebUtility.HtmlEncode(code.ToString().TrimEnd())).AppendLine("</code></pre>");
            }

            FlushParagraph();
            FlushList();

            return html.ToString();
        }

        private static string RenderInline(string markdown)
        {
            var encoded = WebUtility.HtmlEncode(markdown);
            encoded = Regex.Replace(encoded, @"`([^`]+)`", "<code>$1</code>");
            encoded = Regex.Replace(encoded, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");
            encoded = Regex.Replace(encoded, @"__([^_]+)__", "<strong>$1</strong>");
            encoded = Regex.Replace(encoded, @"(?<!\*)\*([^*]+)\*(?!\*)", "<em>$1</em>");
            encoded = Regex.Replace(encoded, @"(?<!_)_([^_]+)_(?!_)", "<em>$1</em>");
            return encoded;
        }
    }
}

public sealed record SubmissionFileResponse(
    string OriginalFileName,
    string ContentType,
    byte[] Content,
    bool IsPreviewGenerated);
