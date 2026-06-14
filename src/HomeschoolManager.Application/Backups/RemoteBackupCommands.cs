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

public sealed record CreateGmailBackupDraftCommand(
    string Passphrase,
    string RecipientEmail);

public sealed record PreviewGoogleDriveRestoreCommand(
    string DriveFileId,
    string Passphrase);

public sealed record RestoreGoogleDriveBackupCommand(
    string DriveFileId,
    string Passphrase,
    bool ConfirmReplaceCurrentRecords);
