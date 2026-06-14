namespace HomeschoolManager.Application.Backups;

public interface IRemoteBackupStore
{
    Task<RemoteBackupConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);

    Task SaveGoogleClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    Task SaveGoogleConnectionAsync(
        GoogleOAuthTokenSet tokenSet,
        DateTimeOffset connectedAtUtc,
        CancellationToken cancellationToken = default);

    Task<GoogleOAuthTokenSet?> GetGoogleTokensAsync(CancellationToken cancellationToken = default);

    Task SaveGoogleTokensAsync(GoogleOAuthTokenSet tokenSet, CancellationToken cancellationToken = default);

    Task DisconnectGoogleAsync(CancellationToken cancellationToken = default);

    Task SavePendingGoogleConnectionAsync(
        GoogleOAuthPendingConnection pending,
        CancellationToken cancellationToken = default);

    Task<GoogleOAuthPendingConnection?> GetPendingGoogleConnectionAsync(CancellationToken cancellationToken = default);

    Task ClearPendingGoogleConnectionAsync(CancellationToken cancellationToken = default);

    Task AddHistoryAsync(RemoteBackupHistoryItem item, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RemoteBackupHistoryItem>> ListHistoryAsync(CancellationToken cancellationToken = default);
}
