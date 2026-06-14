using System.Text.Json;
using HomeschoolManager.Application.Backups;
using Microsoft.AspNetCore.DataProtection;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class LocalRemoteBackupStore : IRemoteBackupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly AppDataPaths paths;
    private readonly IDataProtector protector;
    private readonly SemaphoreSlim gate = new(1, 1);

    public LocalRemoteBackupStore(AppDataPaths paths, IDataProtectionProvider dataProtectionProvider)
    {
        this.paths = paths;
        protector = dataProtectionProvider.CreateProtector("HomeschoolManager.RemoteBackups.GoogleTokens.v1");
    }

    public async Task<RemoteBackupConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        return ToConfiguration(document);
    }

    public async Task SaveGoogleClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            document.GoogleOAuthClientId = clientId;
            await WriteDocumentAsync(document, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveGoogleConnectionAsync(
        GoogleOAuthTokenSet tokenSet,
        DateTimeOffset connectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            document.GoogleConnectedAtUtc = connectedAtUtc;
            document.GrantedScopes = tokenSet.Scope;
            await WriteDocumentAsync(document, cancellationToken);
            await WriteTokenDocumentAsync(ToProtectedToken(tokenSet), cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<GoogleOAuthTokenSet?> GetGoogleTokensAsync(CancellationToken cancellationToken = default)
    {
        var path = TokenPath();
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        var token = await JsonSerializer.DeserializeAsync<GoogleTokenDocument>(stream, JsonOptions, cancellationToken);
        return token is null ? null : FromProtectedToken(token);
    }

    public async Task SaveGoogleTokensAsync(GoogleOAuthTokenSet tokenSet, CancellationToken cancellationToken = default)
    {
        await WriteTokenDocumentAsync(ToProtectedToken(tokenSet), cancellationToken);
    }

    public async Task DisconnectGoogleAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            document.GoogleConnectedAtUtc = null;
            document.GrantedScopes = "";
            await WriteDocumentAsync(document, cancellationToken);
            var tokenPath = TokenPath();
            if (File.Exists(tokenPath))
            {
                File.Delete(tokenPath);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SavePendingGoogleConnectionAsync(
        GoogleOAuthPendingConnection pending,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            document.PendingGoogleConnection = pending;
            await WriteDocumentAsync(document, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<GoogleOAuthPendingConnection?> GetPendingGoogleConnectionAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        return document.PendingGoogleConnection;
    }

    public async Task ClearPendingGoogleConnectionAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            document.PendingGoogleConnection = null;
            await WriteDocumentAsync(document, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AddHistoryAsync(RemoteBackupHistoryItem item, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadDocumentAsync(cancellationToken);
            document.History.Insert(0, item);
            if (item.Destination.Equals("Google Drive", StringComparison.OrdinalIgnoreCase))
            {
                document.LastDriveUploadAtUtc = item.CreatedAtUtc;
            }
            else if (item.Destination.Equals("Gmail draft", StringComparison.OrdinalIgnoreCase))
            {
                document.LastGmailDraftAtUtc = item.CreatedAtUtc;
            }

            await WriteDocumentAsync(document, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<RemoteBackupHistoryItem>> ListHistoryAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        return document.History
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArray();
    }

    private async Task<RemoteBackupDocument> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        var path = SettingsPath();
        if (!File.Exists(path))
        {
            return new RemoteBackupDocument();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<RemoteBackupDocument>(stream, JsonOptions, cancellationToken)
            ?? new RemoteBackupDocument();
    }

    private async Task WriteDocumentAsync(RemoteBackupDocument document, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.ConfigDirectory);
        await using var stream = new FileStream(SettingsPath(), FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private async Task WriteTokenDocumentAsync(GoogleTokenDocument token, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(SecretsDirectory());
        await using var stream = new FileStream(TokenPath(), FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, token, JsonOptions, cancellationToken);
    }

    private RemoteBackupConfiguration ToConfiguration(RemoteBackupDocument document)
    {
        return new RemoteBackupConfiguration(
            document.GoogleOAuthClientId,
            document.GoogleConnectedAtUtc,
            document.GrantedScopes,
            document.LastDriveUploadAtUtc,
            document.LastGmailDraftAtUtc);
    }

    private GoogleTokenDocument ToProtectedToken(GoogleOAuthTokenSet tokenSet)
    {
        return new GoogleTokenDocument(
            protector.Protect(tokenSet.AccessToken),
            protector.Protect(tokenSet.RefreshToken),
            tokenSet.AccessTokenExpiresAtUtc,
            tokenSet.Scope);
    }

    private GoogleOAuthTokenSet FromProtectedToken(GoogleTokenDocument token)
    {
        return new GoogleOAuthTokenSet(
            protector.Unprotect(token.ProtectedAccessToken),
            protector.Unprotect(token.ProtectedRefreshToken),
            token.AccessTokenExpiresAtUtc,
            token.Scope);
    }

    private string SettingsPath() => Path.Combine(paths.ConfigDirectory, "remote-backup-settings.json");

    private string SecretsDirectory() => paths.SecretsDirectory;

    private string TokenPath() => Path.Combine(SecretsDirectory(), "google-backup-token.json");

    private sealed class RemoteBackupDocument
    {
        public string GoogleOAuthClientId { get; set; } = "";

        public DateTimeOffset? GoogleConnectedAtUtc { get; set; }

        public string GrantedScopes { get; set; } = "";

        public DateTimeOffset? LastDriveUploadAtUtc { get; set; }

        public DateTimeOffset? LastGmailDraftAtUtc { get; set; }

        public GoogleOAuthPendingConnection? PendingGoogleConnection { get; set; }

        public List<RemoteBackupHistoryItem> History { get; set; } = [];
    }

    private sealed record GoogleTokenDocument(
        string ProtectedAccessToken,
        string ProtectedRefreshToken,
        DateTimeOffset AccessTokenExpiresAtUtc,
        string Scope);
}
