namespace HomeschoolManager.Application.Backups;

public sealed record CreateBackupCommand(BackupKind Kind = BackupKind.Manual);

public sealed record ValidateBackupCommand(string FileName, byte[] Content);

public sealed record RestoreBackupCommand(
    string FileName,
    byte[] Content,
    bool ConfirmReplaceCurrentRecords);

public sealed record DownloadBackupCommand(string BackupId);

public sealed record DeleteBackupCommand(string BackupId, bool ConfirmDelete);
