namespace HomeschoolManager.Application.Backups;

public sealed record BackupManifest(
    string PackageType,
    int FormatVersion,
    BackupKind BackupKind,
    DateTimeOffset CreatedAtUtc,
    string CreatedBy,
    string AppVersion,
    int DataSchemaVersion,
    string SourceDataRootMode,
    string HouseholdName,
    string SchoolName,
    IReadOnlyList<BackupStudentSummary> Students,
    IReadOnlyList<string> IncludedFolders,
    int FileCount,
    long TotalBytes,
    IReadOnlyList<string> Warnings);

public sealed record BackupStudentSummary(
    Guid StudentId,
    string DisplayName,
    int GradeLevel);

public sealed record BackupFileChecksum(
    string Path,
    string Sha256,
    long SizeBytes);

public sealed record BackupDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content,
    BackupManifest Manifest);

public sealed record BackupValidationReport(
    bool IsValid,
    BackupManifest? Manifest,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    int CheckedFileCount,
    long TotalBytes);

public sealed record BackupRestorePreview(
    BackupManifest Manifest,
    string CurrentDataRoot,
    string SafetyBackupMessage,
    IReadOnlyList<string> Warnings);

public sealed record BackupRestoreResult(
    BackupManifest RestoredManifest,
    BackupManifest SafetyBackupManifest,
    string SafetyBackupFileName,
    IReadOnlyList<string> Warnings);

public sealed record BackupHistoryItem(
    string BackupId,
    string FileName,
    BackupKind BackupKind,
    DateTimeOffset CreatedAtUtc,
    string HouseholdName,
    string SchoolName,
    int StudentCount,
    long SizeBytes,
    bool IsReadable,
    IReadOnlyList<string> Warnings);
