namespace HomeschoolManager.Application.Backups;

public sealed record SaveGoogleBackupSettingsCommand(string GoogleOAuthClientId);

public sealed record StartGoogleConnectionCommand(string RedirectUri);

public sealed record CompleteGoogleConnectionCommand(
    string State,
    string Code,
    string RedirectUri);

public sealed record CreateEncryptedBackupCommand(
    string Passphrase,
    BackupKind Kind = BackupKind.Manual);

public sealed record UploadGoogleDriveBackupCommand(string Passphrase);

public sealed record CreateSyncedFolderBackupCommand(
    bool EncryptBackup,
    string Passphrase);

public sealed record RecordSyncedFolderBackupCommand(
    string FileName,
    long SizeBytes,
    bool IsEncrypted,
    string FolderName);

public sealed record CreateGmailBackupDraftCommand(
    string Passphrase,
    string RecipientEmail);

public sealed record PreviewEncryptedBackupRestoreCommand(
    string FileName,
    byte[] Content,
    string Passphrase);

public sealed record RestoreEncryptedBackupCommand(
    string FileName,
    byte[] Content,
    string Passphrase,
    bool ConfirmReplaceCurrentRecords);

public sealed record PreviewGoogleDriveRestoreCommand(
    string DriveFileId,
    string Passphrase);

public sealed record RestoreGoogleDriveBackupCommand(
    string DriveFileId,
    string Passphrase,
    bool ConfirmReplaceCurrentRecords);
