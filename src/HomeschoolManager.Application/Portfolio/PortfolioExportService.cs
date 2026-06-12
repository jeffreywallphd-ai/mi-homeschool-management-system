using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Application.Submissions;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Portfolio;

public sealed class PortfolioExportService
{
    private const string ContentTypeZip = "application/zip";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IHomeschoolRepository repository;
    private readonly ISubmissionFileStore fileStore;

    public PortfolioExportService(IHomeschoolRepository repository, ISubmissionFileStore fileStore)
    {
        this.repository = repository;
        this.fileStore = fileStore;
    }

    public async Task<OperationResult<PortfolioExportPreview>> GetExportPreviewAsync(
        UserContext user,
        Guid studentId,
        Guid? approvalSnapshotId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireApprovedPreviewAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<PortfolioExportPreview>.Failure(authorized.Errors.ToArray());
        }

        var context = await ResolveExportContextAsync(studentId, approvalSnapshotId, cancellationToken);
        if (!context.Succeeded)
        {
            return OperationResult<PortfolioExportPreview>.Failure(context.Errors.ToArray());
        }

        var manifest = await BuildManifestAsync(context.Value!, includeFileContent: false, cancellationToken);
        return OperationResult<PortfolioExportPreview>.Success(ToPreview(context.Value!, manifest.Manifest));
    }

    public async Task<OperationResult<PortfolioExportDownloadFile>> CreateArchivePacketAsync(
        UserContext user,
        CreatePortfolioExportCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<PortfolioExportDownloadFile>.Failure(authorized.Errors.ToArray());
        }

        var context = await ResolveExportContextAsync(command.StudentId, command.ApprovalSnapshotId, cancellationToken);
        if (!context.Succeeded)
        {
            return OperationResult<PortfolioExportDownloadFile>.Failure(context.Errors.ToArray());
        }

        var export = await BuildManifestAsync(context.Value!, includeFileContent: true, cancellationToken);
        var html = BuildPortfolioHtml(context.Value!, export.Manifest);
        var markdown = BuildManifestMarkdown(export.Manifest);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipText(archive, "portfolio.html", html);
            WriteZipJson(archive, "manifest.json", export.Manifest);
            WriteZipText(archive, "manifest.md", markdown);

            foreach (var file in export.Files.Where(file => file.Content is not null))
            {
                var entry = archive.CreateEntry(ShortenZipPath(file.ArchivePath));
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(file.Content!, cancellationToken);
            }
        }

        return OperationResult<PortfolioExportDownloadFile>.Success(new PortfolioExportDownloadFile(
            ArchiveFileName(context.Value!),
            ContentTypeZip,
            stream.ToArray(),
            export.Manifest));
    }

    private async Task<OperationResult<PortfolioExportContext>> ResolveExportContextAsync(
        Guid studentId,
        Guid? approvalSnapshotId,
        CancellationToken cancellationToken)
    {
        var student = await repository.GetStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<PortfolioExportContext>.Failure("Student was not found.");
        }

        var design = (await repository.GetPortfolioDesignsAsync(cancellationToken))
            .Where(item => item.StudentId == student.Id)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefault();
        if (design is null || design.ApprovalSnapshots.Count == 0)
        {
            return OperationResult<PortfolioExportContext>.Failure("Approve a portfolio design before creating an archive packet.");
        }

        var snapshot = approvalSnapshotId.HasValue
            ? design.ApprovalSnapshots.FirstOrDefault(item => item.Id == approvalSnapshotId.Value)
            : design.ApprovalSnapshots.OrderByDescending(item => item.ApprovedAtUtc).FirstOrDefault();
        if (snapshot is null)
        {
            return OperationResult<PortfolioExportContext>.Failure("Approved portfolio snapshot was not found.");
        }

        var evidence = await repository.GetEvidenceRecordsAsync(cancellationToken);
        var submissions = await repository.GetAssignmentSubmissionsAsync(cancellationToken);

        return OperationResult<PortfolioExportContext>.Success(new PortfolioExportContext(
            student,
            design,
            snapshot,
            evidence.Where(item => item.StudentId == student.Id).ToArray(),
            submissions.Where(item => item.StudentId == student.Id).ToArray()));
    }

    private async Task<PortfolioExportAssembly> BuildManifestAsync(
        PortfolioExportContext context,
        bool includeFileContent,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var exportFiles = new List<PortfolioExportFileAssembly>();
        var sectionRecords = new List<PortfolioArchiveSection>();

        foreach (var section in context.Snapshot.Sections.OrderBy(item => item.SortOrder))
        {
            var itemRecords = new List<PortfolioArchiveItem>();
            foreach (var item in section.Items.OrderBy(item => item.SortOrder))
            {
                var itemFiles = await BuildFilesForItemAsync(context, section, item, includeFileContent, warnings, cancellationToken);
                exportFiles.AddRange(itemFiles);
                itemRecords.Add(new PortfolioArchiveItem(
                    item.PortfolioDraftItemId,
                    item.EvidenceRecordId,
                    item.DisplayTitle,
                    item.CourseTitle,
                    item.ModuleTitle,
                    item.AssignmentTitle,
                    item.StudentReflection,
                    item.ChosenReason,
                    item.SkillsShown,
                    item.ParentReviewNotes,
                    item.SortOrder,
                    itemFiles.Select(file => file.ManifestFile).ToArray()));
            }

            sectionRecords.Add(new PortfolioArchiveSection(
                section.SectionId,
                section.Heading,
                section.Introduction,
                section.SortOrder,
                itemRecords));
        }

        var manifestFiles = exportFiles.Select(file => file.ManifestFile).ToArray();
        var manifest = new PortfolioArchiveManifest(
            "homeschool-manager.portfolio-export",
            1,
            DateTimeOffset.UtcNow,
            context.Student.Id,
            StudentName(context.Student),
            context.Snapshot.Id,
            context.Snapshot.Version,
            context.Snapshot.Title,
            context.Snapshot.ApprovedAtUtc,
            context.Snapshot.ApprovedBy,
            sectionRecords.Count,
            sectionRecords.Sum(section => section.Items.Count),
            manifestFiles.Count(file => file.Included),
            manifestFiles.Count(file => !file.Included),
            sectionRecords,
            manifestFiles,
            warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

        return new PortfolioExportAssembly(manifest, exportFiles);
    }

    private async Task<IReadOnlyList<PortfolioExportFileAssembly>> BuildFilesForItemAsync(
        PortfolioExportContext context,
        PortfolioApprovedSectionSnapshot section,
        PortfolioApprovedItemSnapshot item,
        bool includeFileContent,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var evidence = context.EvidenceRecords.FirstOrDefault(candidate => candidate.Id == item.EvidenceRecordId);
        if (evidence is null)
        {
            warnings.Add($"Evidence record for \"{item.DisplayTitle}\" was not found.");
            return [];
        }

        var submission = context.Submissions.FirstOrDefault(candidate => candidate.Id == evidence.SubmissionId);
        if (submission is null)
        {
            warnings.Add($"Submission for \"{item.DisplayTitle}\" was not found.");
            return [];
        }

        var files = new List<PortfolioExportFileAssembly>();
        foreach (var fileId in evidence.StoredFileIds)
        {
            var storedFile = submission.Attachments.FirstOrDefault(candidate => candidate.Id == fileId);
            if (storedFile is null)
            {
                var warning = $"File metadata for \"{item.DisplayTitle}\" was not found.";
                warnings.Add(warning);
                files.Add(new PortfolioExportFileAssembly(new PortfolioArchiveFile(
                    fileId,
                    evidence.Id,
                    evidence.SubmissionId,
                    "missing-file",
                    "",
                    "application/octet-stream",
                    0,
                    "",
                    false,
                    warning), null));
                continue;
            }

            var archivePath = EvidenceFilePath(section, item, storedFile);
            byte[]? content = null;
            var included = true;
            var warningText = "";
            if (includeFileContent)
            {
                var storedContent = await fileStore.ReadStoredFileAsync(storedFile, cancellationToken);
                if (storedContent is null)
                {
                    included = false;
                    warningText = $"Stored file \"{storedFile.OriginalFileName}\" for \"{item.DisplayTitle}\" was not found.";
                    warnings.Add(warningText);
                }
                else
                {
                    content = storedContent.Content;
                }
            }

            files.Add(new PortfolioExportFileAssembly(new PortfolioArchiveFile(
                storedFile.Id,
                evidence.Id,
                evidence.SubmissionId,
                storedFile.OriginalFileName,
                included ? archivePath : "",
                storedFile.ContentType,
                storedFile.SizeBytes,
                storedFile.ChecksumSha256,
                included,
                warningText), content));
        }

        return files;
    }

    private static PortfolioExportPreview ToPreview(PortfolioExportContext context, PortfolioArchiveManifest manifest)
    {
        return new PortfolioExportPreview(
            context.Student.Id,
            manifest.StudentName,
            context.Snapshot.Id,
            context.Snapshot.Version,
            context.Snapshot.Title,
            context.Snapshot.ApprovedAtUtc,
            context.Snapshot.ApprovedBy,
            ArchiveFileName(context),
            "portfolio.html",
            "manifest.json",
            "manifest.md",
            manifest.SectionCount,
            manifest.ItemCount,
            manifest.AttachedFileCount,
            manifest.MissingFileCount,
            manifest.Sections
                .OrderBy(item => item.SortOrder)
                .Select(item => new PortfolioExportSectionPreview(item.SectionId, item.Heading, item.SortOrder, item.Items.Count))
                .ToArray(),
            BuildPreviewFromSnapshot(context.Student, context.Snapshot),
            manifest.Warnings);
    }

    private static PortfolioPreviewView BuildPreviewFromSnapshot(Student student, PortfolioApprovalSnapshot snapshot)
    {
        return new PortfolioPreviewView(
            student.Id,
            StudentName(student),
            snapshot.Title,
            snapshot.Purpose,
            PortfolioDesignStatus.Approved,
            snapshot.Version,
            snapshot.ApprovedAtUtc,
            snapshot.Narratives
                .OrderBy(item => item.SortOrder)
                .Select(item => new PortfolioPreviewNarrativeView(item.Prompt, item.Response, item.SortOrder))
                .ToArray(),
            snapshot.Sections
                .OrderBy(item => item.SortOrder)
                .Select(section => new PortfolioPreviewSectionView(
                    section.SectionId,
                    section.Heading,
                    section.Introduction,
                    section.SortOrder,
                    section.Items
                        .OrderBy(item => item.SortOrder)
                        .Select(item => new PortfolioPreviewItemView(
                            item.PortfolioDraftItemId,
                            item.EvidenceRecordId,
                            item.DisplayTitle,
                            item.CourseTitle,
                            item.ModuleTitle,
                            item.AssignmentTitle,
                            item.StudentReflection,
                            item.ChosenReason,
                            item.SkillsShown,
                            item.ParentReviewNotes,
                            item.FileCount,
                            item.SortOrder))
                        .ToArray()))
                .ToArray());
    }

    private static string BuildPortfolioHtml(PortfolioExportContext context, PortfolioArchiveManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine($"<title>{Html(context.Snapshot.Title)} - Portfolio</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:Arial,Helvetica,sans-serif;color:#15002f;margin:0;background:#f7f3fb;}main{max-width:880px;margin:0 auto;background:white;min-height:100vh;padding:48px;}h1{font-size:34px;margin:0 0 8px;}h2{font-size:22px;border-bottom:2px solid #6d28d9;padding-bottom:8px;margin-top:34px;}h3{font-size:17px;margin-bottom:4px;}p{line-height:1.5;}small,.muted{color:#5d5270;}.cover{border-bottom:1px solid #ded3ee;padding-bottom:28px;margin-bottom:28px;}.meta{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;margin:20px 0;}.meta div{border:1px solid #ded3ee;padding:10px;border-radius:6px;}.item{border:1px solid #ded3ee;border-radius:6px;padding:14px;margin:14px 0;break-inside:avoid;}.skills span{display:inline-block;border:1px solid #d7c8f5;border-radius:999px;padding:3px 8px;margin:3px;font-size:12px;}.files li{margin-bottom:4px;}@media print{body{background:white;}main{padding:0;max-width:none}.item,.meta div{border-color:#aaa}.cover{break-after:page;}}</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body><main>");
        builder.AppendLine("<section class=\"cover\">");
        builder.AppendLine($"<h1>{Html(context.Snapshot.Title)}</h1>");
        builder.AppendLine($"<p>{Html(manifest.StudentName)}</p>");
        if (!string.IsNullOrWhiteSpace(context.Snapshot.Purpose))
        {
            builder.AppendLine($"<p>{Html(context.Snapshot.Purpose)}</p>");
        }

        builder.AppendLine("<div class=\"meta\">");
        builder.AppendLine($"<div><small>Approved</small><br>{Html(context.Snapshot.ApprovedAtUtc.ToLocalTime().ToString("g"))}</div>");
        builder.AppendLine($"<div><small>Approved by</small><br>{Html(context.Snapshot.ApprovedBy)}</div>");
        builder.AppendLine($"<div><small>Snapshot version</small><br>Version {context.Snapshot.Version}</div>");
        builder.AppendLine($"<div><small>Included evidence</small><br>{manifest.ItemCount} item{Plural(manifest.ItemCount)}, {manifest.AttachedFileCount} file{Plural(manifest.AttachedFileCount)}</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<p class=\"muted\">This packet is a parent-owned homeschool record generated from an approved portfolio snapshot.</p>");
        builder.AppendLine("</section>");

        foreach (var narrative in context.Snapshot.Narratives.OrderBy(item => item.SortOrder))
        {
            builder.AppendLine("<section>");
            builder.AppendLine($"<h2>{Html(narrative.Prompt)}</h2>");
            builder.AppendLine($"<p>{Html(string.IsNullOrWhiteSpace(narrative.Response) ? "No response was added." : narrative.Response)}</p>");
            builder.AppendLine("</section>");
        }

        if (manifest.Sections.Count > 1)
        {
            builder.AppendLine("<section>");
            builder.AppendLine("<h2>Contents</h2><ol>");
            foreach (var section in manifest.Sections.OrderBy(item => item.SortOrder))
            {
                builder.AppendLine($"<li>{Html(section.Heading)}</li>");
            }

            builder.AppendLine("</ol></section>");
        }

        foreach (var section in manifest.Sections.OrderBy(item => item.SortOrder))
        {
            builder.AppendLine("<section>");
            builder.AppendLine($"<h2>{Html(section.Heading)}</h2>");
            if (!string.IsNullOrWhiteSpace(section.Introduction))
            {
                builder.AppendLine($"<p>{Html(section.Introduction)}</p>");
            }

            foreach (var item in section.Items.OrderBy(item => item.SortOrder))
            {
                builder.AppendLine("<article class=\"item\">");
                builder.AppendLine($"<h3>{Html(item.DisplayTitle)}</h3>");
                builder.AppendLine($"<small>{Html(item.CourseTitle)} | {Html(item.ModuleTitle)} | {Html(item.AssignmentTitle)}</small>");
                AppendParagraph(builder, "Student reflection", item.StudentReflection);
                AppendParagraph(builder, "Why this belongs", item.ChosenReason);
                AppendParagraph(builder, "Parent notes", item.ParentReviewNotes);
                if (item.SkillsShown.Count > 0)
                {
                    builder.AppendLine("<div class=\"skills\"><strong>Skills shown</strong><br>");
                    foreach (var skill in item.SkillsShown)
                    {
                        builder.AppendLine($"<span>{Html(skill)}</span>");
                    }

                    builder.AppendLine("</div>");
                }

                if (item.Files.Count > 0)
                {
                    builder.AppendLine("<div class=\"files\"><strong>Files</strong><ul>");
                    foreach (var file in item.Files)
                    {
                        var label = file.Included
                            ? $"<a href=\"{Html(file.ArchivePath)}\">{Html(file.OriginalFileName)}</a>"
                            : $"{Html(file.OriginalFileName)} - {Html(file.Warning)}";
                        builder.AppendLine($"<li>{label}</li>");
                    }

                    builder.AppendLine("</ul></div>");
                }

                builder.AppendLine("</article>");
            }

            builder.AppendLine("</section>");
        }

        if (manifest.Warnings.Count > 0)
        {
            builder.AppendLine("<section><h2>Packet Notes</h2><ul>");
            foreach (var warning in manifest.Warnings)
            {
                builder.AppendLine($"<li>{Html(warning)}</li>");
            }

            builder.AppendLine("</ul></section>");
        }

        builder.AppendLine("</main></body></html>");
        return builder.ToString();
    }

    private static string BuildManifestMarkdown(PortfolioArchiveManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {manifest.PortfolioTitle}");
        builder.AppendLine();
        builder.AppendLine($"- Student: {manifest.StudentName}");
        builder.AppendLine($"- Approved: {manifest.ApprovedAtUtc.ToLocalTime():g}");
        builder.AppendLine($"- Approved by: {manifest.ApprovedBy}");
        builder.AppendLine($"- Snapshot version: {manifest.SnapshotVersion}");
        builder.AppendLine($"- Sections: {manifest.SectionCount}");
        builder.AppendLine($"- Items: {manifest.ItemCount}");
        builder.AppendLine($"- Included files: {manifest.AttachedFileCount}");
        builder.AppendLine($"- Missing files: {manifest.MissingFileCount}");
        builder.AppendLine();
        builder.AppendLine("This packet is a parent-owned homeschool portfolio archive generated from an approved snapshot.");
        builder.AppendLine();

        foreach (var section in manifest.Sections.OrderBy(item => item.SortOrder))
        {
            builder.AppendLine($"## {section.Heading}");
            if (!string.IsNullOrWhiteSpace(section.Introduction))
            {
                builder.AppendLine(section.Introduction);
                builder.AppendLine();
            }

            foreach (var item in section.Items.OrderBy(item => item.SortOrder))
            {
                builder.AppendLine($"### {item.DisplayTitle}");
                builder.AppendLine($"- Source: {item.CourseTitle} / {item.ModuleTitle} / {item.AssignmentTitle}");
                builder.AppendLine($"- Files: {item.Files.Count(file => file.Included)} included");
                foreach (var file in item.Files)
                {
                    builder.AppendLine(file.Included
                        ? $"  - {file.ArchivePath} ({file.OriginalFileName})"
                        : $"  - Missing: {file.OriginalFileName} ({file.Warning})");
                }

                builder.AppendLine();
            }
        }

        if (manifest.Warnings.Count > 0)
        {
            builder.AppendLine("## Packet Notes");
            foreach (var warning in manifest.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        return builder.ToString();
    }

    private static void AppendParagraph(StringBuilder builder, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"<p><strong>{Html(label)}:</strong> {Html(value)}</p>");
        }
    }

    private static void WriteZipJson<T>(ZipArchive archive, string path, T value)
    {
        var entry = archive.CreateEntry(ShortenZipPath(path));
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(JsonSerializer.Serialize(value, JsonOptions));
    }

    private static void WriteZipText(ZipArchive archive, string path, string value)
    {
        var entry = archive.CreateEntry(ShortenZipPath(path));
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(value);
    }

    private static OperationResult RequireApprovedPreviewAccess(UserContext user)
    {
        return user.Role is UserRole.ParentAdmin or UserRole.Student
            ? OperationResult.Success()
            : OperationResult.Failure("Sign in to view approved portfolio information.");
    }

    private static string ArchiveFileName(PortfolioExportContext context)
    {
        return $"{SafeFileName(context.Snapshot.Title)}-v{context.Snapshot.Version}-portfolio.zip";
    }

    private static string EvidenceFilePath(
        PortfolioApprovedSectionSnapshot section,
        PortfolioApprovedItemSnapshot item,
        StoredFileReference file)
    {
        var extension = Path.GetExtension(file.OriginalFileName);
        var baseName = SafeFileName(Path.GetFileNameWithoutExtension(file.OriginalFileName));
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "file";
        }

        return ShortenZipPath(
            $"evidence-files/{section.SortOrder:00}-{SafeFileName(section.Heading)}/{item.SortOrder:00}-{SafeFileName(item.DisplayTitle)}/{file.Id:N}-{baseName}{extension}");
    }

    private static string ShortenZipPath(string path)
    {
        return path.Length <= 180 ? path : path[..180];
    }

    private static string SafeFileName(string value)
    {
        var safe = new string((value ?? "")
            .Trim()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray());
        while (safe.Contains("--", StringComparison.Ordinal))
        {
            safe = safe.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(safe.Trim('-')) ? "portfolio" : safe.Trim('-');
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value ?? "");
    }

    private static string Plural(int count)
    {
        return count == 1 ? "" : "s";
    }

    private static string StudentName(Student student)
    {
        return $"{student.FirstName} {student.LastName}".Trim();
    }

    private sealed record PortfolioExportContext(
        Student Student,
        PortfolioDesign Design,
        PortfolioApprovalSnapshot Snapshot,
        IReadOnlyList<EvidenceRecord> EvidenceRecords,
        IReadOnlyList<AssignmentSubmission> Submissions);

    private sealed record PortfolioExportAssembly(
        PortfolioArchiveManifest Manifest,
        IReadOnlyList<PortfolioExportFileAssembly> Files);

    private sealed record PortfolioExportFileAssembly(
        PortfolioArchiveFile ManifestFile,
        byte[]? Content)
    {
        public string ArchivePath => ManifestFile.ArchivePath;
    }
}
