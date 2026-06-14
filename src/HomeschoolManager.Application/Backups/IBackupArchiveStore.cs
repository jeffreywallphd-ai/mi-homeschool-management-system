namespace HomeschoolManager.Application.Backups;

public interface IBackupArchiveStore
{
    Task<BackupDownloadFile> CreateBackupAsync(
        string createdBy,
        BackupKind kind,
        CancellationToken cancellationToken = default);

    Task<BackupValidationReport> ValidateBackupAsync(
        byte[] content,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<BackupRestorePreview> PreviewRestoreAsync(
        byte[] content,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<BackupRestoreResult> RestoreBackupAsync(
        byte[] content,
        string fileName,
        string restoredBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupHistoryItem>> ListBackupsAsync(CancellationToken cancellationToken = default);

    Task<BackupDownloadFile?> ReadBackupAsync(string backupId, CancellationToken cancellationToken = default);

    Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);
}
