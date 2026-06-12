namespace HomeschoolManager.Application.Portfolio;

public sealed record PortfolioExportPreview(
    Guid StudentId,
    string StudentName,
    Guid ApprovalSnapshotId,
    int Version,
    string Title,
    DateTimeOffset ApprovedAtUtc,
    string ApprovedBy,
    string FileName,
    string HtmlFileName,
    string ManifestJsonName,
    string ManifestMarkdownName,
    int SectionCount,
    int ItemCount,
    int AttachedFileCount,
    int MissingFileCount,
    IReadOnlyList<PortfolioExportSectionPreview> Sections,
    PortfolioPreviewView Preview,
    IReadOnlyList<string> Warnings);

public sealed record PortfolioExportSectionPreview(
    Guid SectionId,
    string Heading,
    int SortOrder,
    int ItemCount);

public sealed record PortfolioExportDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content,
    PortfolioArchiveManifest Manifest);

public sealed record PortfolioArchiveManifest(
    string Schema,
    int SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    Guid StudentId,
    string StudentName,
    Guid ApprovalSnapshotId,
    int SnapshotVersion,
    string PortfolioTitle,
    DateTimeOffset ApprovedAtUtc,
    string ApprovedBy,
    int SectionCount,
    int ItemCount,
    int AttachedFileCount,
    int MissingFileCount,
    IReadOnlyList<PortfolioArchiveSection> Sections,
    IReadOnlyList<PortfolioArchiveFile> Files,
    IReadOnlyList<string> Warnings);

public sealed record PortfolioArchiveSection(
    Guid SectionId,
    string Heading,
    string Introduction,
    int SortOrder,
    IReadOnlyList<PortfolioArchiveItem> Items);

public sealed record PortfolioArchiveItem(
    Guid PortfolioDraftItemId,
    Guid EvidenceRecordId,
    string DisplayTitle,
    string CourseTitle,
    string ModuleTitle,
    string AssignmentTitle,
    string StudentReflection,
    string ChosenReason,
    IReadOnlyList<string> SkillsShown,
    string ParentReviewNotes,
    int SortOrder,
    IReadOnlyList<PortfolioArchiveFile> Files);

public sealed record PortfolioArchiveFile(
    Guid FileId,
    Guid EvidenceRecordId,
    Guid SubmissionId,
    string OriginalFileName,
    string ArchivePath,
    string ContentType,
    long SizeBytes,
    string ChecksumSha256,
    bool Included,
    string Warning);
