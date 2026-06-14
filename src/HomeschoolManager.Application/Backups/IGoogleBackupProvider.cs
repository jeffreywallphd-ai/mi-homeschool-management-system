namespace HomeschoolManager.Application.Backups;

public interface IGoogleBackupProvider
{
    string BuildAuthorizationUrl(
        string clientId,
        string redirectUri,
        string state,
        string codeChallenge);

    Task<GoogleConnectionResult> CompleteConnectionAsync(
        string clientId,
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<GoogleOAuthTokenSet> RefreshAccessTokenAsync(
        string clientId,
        GoogleOAuthTokenSet tokenSet,
        CancellationToken cancellationToken = default);

    Task<GoogleDriveUploadResult> UploadDriveBackupAsync(
        GoogleOAuthTokenSet tokenSet,
        EncryptedBackupDownloadFile encryptedBackup,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoogleDriveBackupFile>> ListDriveBackupsAsync(
        GoogleOAuthTokenSet tokenSet,
        CancellationToken cancellationToken = default);

    Task<RemoteEncryptedBackupFile> DownloadDriveBackupAsync(
        GoogleOAuthTokenSet tokenSet,
        string fileId,
        CancellationToken cancellationToken = default);

    Task<GmailDraftResult> CreateGmailDraftAsync(
        GoogleOAuthTokenSet tokenSet,
        EncryptedBackupDownloadFile encryptedBackup,
        string recipientEmail,
        CancellationToken cancellationToken = default);
}
