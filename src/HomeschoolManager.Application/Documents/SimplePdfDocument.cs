using System.Text;

namespace HomeschoolManager.Application.Documents;

internal static class SimplePdfDocument
{
    public static byte[] CreateTextDocument(string title, IEnumerable<string> lines)
    {
        var documentLines = WrapLines(string.Join('\n', new[] { title, "" }.Concat(lines)), 86);
        var pages = documentLines.Chunk(48).Select(pageLines => pageLines.ToArray()).ToArray();
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
        foreach (var sourceLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
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
