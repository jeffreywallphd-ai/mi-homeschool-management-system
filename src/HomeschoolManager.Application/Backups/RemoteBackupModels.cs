namespace HomeschoolManager.Application.Backups;

public sealed record RemoteBackupConfiguration(
    string GoogleOAuthClientId,
    DateTimeOffset? GoogleConnectedAtUtc,
    string GrantedScopes,
    DateTimeOffset? LastDriveUploadAtUtc,
    DateTimeOffset? LastGmailDraftAtUtc)
{
    public bool HasGoogleClientId => !string.IsNullOrWhiteSpace(GoogleOAuthClientId);

    public bool IsGoogleConnected => GoogleConnectedAtUtc is not null;
}

public sealed record RemoteBackupStatus(
    RemoteBackupConfiguration Configuration,
    IReadOnlyList<RemoteBackupHistoryItem> History,
    IReadOnlyList<string> Warnings);

public sealed record RemoteBackupHistoryItem(
    string Destination,
    string FileName,
    string ProviderId,
    DateTimeOffset CreatedAtUtc,
    long SizeBytes,
    string Note);

public sealed record GoogleOAuthPendingConnection(
    string State,
    string CodeVerifier,
    string RedirectUri,
    DateTimeOffset CreatedAtUtc);

public sealed record GoogleOAuthTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string Scope);

public sealed record GoogleConnectionResult(
    DateTimeOffset ConnectedAtUtc,
    string GrantedScopes,
    GoogleOAuthTokenSet TokenSet);

public sealed record GoogleDriveUploadResult(
    string FileId,
    string FileName,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record GoogleDriveBackupFile(
    string FileId,
    string FileName,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record RemoteEncryptedBackupFile(
    string FileName,
    string ContentType,
    byte[] Content,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record GmailDraftResult(
    string DraftId,
    string FileName,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);
