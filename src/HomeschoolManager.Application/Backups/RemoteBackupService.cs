using System.Security.Cryptography;
using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Domain.Access;

namespace HomeschoolManager.Application.Backups;

public sealed class RemoteBackupService
{
    private const int MinimumPassphraseLength = 8;

    private readonly BackupService backupService;
    private readonly IBackupEncryptionService encryptionService;
    private readonly IRemoteBackupStore remoteStore;
    private readonly IGoogleBackupProvider googleProvider;

    public RemoteBackupService(
        BackupService backupService,
        IBackupEncryptionService encryptionService,
        IRemoteBackupStore remoteStore,
        IGoogleBackupProvider googleProvider)
    {
        this.backupService = backupService;
        this.encryptionService = encryptionService;
        this.remoteStore = remoteStore;
        this.googleProvider = googleProvider;
    }

    public async Task<OperationResult<RemoteBackupStatus>> GetStatusAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<RemoteBackupStatus>.Failure(authorized.Errors.ToArray());
        }

        var configuration = await remoteStore.GetConfigurationAsync(cancellationToken);
        var history = await remoteStore.ListHistoryAsync(cancellationToken);
        var warnings = new List<string>();
        if (!configuration.HasGoogleClientId)
        {
            warnings.Add("Add a Google OAuth client ID before connecting Google Drive or Gmail.");
        }

        if (!configuration.IsGoogleConnected)
        {
            warnings.Add("Google is not connected yet.");
        }

        return OperationResult<RemoteBackupStatus>.Success(new RemoteBackupStatus(configuration, history, warnings));
    }

    public async Task<OperationResult> SaveGoogleSettingsAsync(
        UserContext user,
        SaveGoogleBackupSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (string.IsNullOrWhiteSpace(command.GoogleOAuthClientId))
        {
            return OperationResult.Failure("Enter the Google OAuth client ID before connecting Google.");
        }

        await remoteStore.SaveGoogleClientIdAsync(command.GoogleOAuthClientId.Trim(), cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult<string>> StartGoogleConnectionAsync(
        UserContext user,
        StartGoogleConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<string>.Failure(authorized.Errors.ToArray());
        }

        var configuration = await remoteStore.GetConfigurationAsync(cancellationToken);
        if (!configuration.HasGoogleClientId)
        {
            return OperationResult<string>.Failure("Add and save the Google OAuth client ID before connecting.");
        }

        if (string.IsNullOrWhiteSpace(command.RedirectUri))
        {
            return OperationResult<string>.Failure("The Google connection callback address could not be prepared.");
        }

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        await remoteStore.SavePendingGoogleConnectionAsync(
            new GoogleOAuthPendingConnection(state, verifier, command.RedirectUri, DateTimeOffset.UtcNow),
            cancellationToken);

        var url = googleProvider.BuildAuthorizationUrl(
            configuration.GoogleOAuthClientId,
            command.RedirectUri,
            state,
            challenge);
        return OperationResult<string>.Success(url);
    }

    public async Task<OperationResult> CompleteGoogleConnectionAsync(
        UserContext user,
        CompleteGoogleConnectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var configuration = await remoteStore.GetConfigurationAsync(cancellationToken);
        if (!configuration.HasGoogleClientId)
        {
            return OperationResult.Failure("Google backup is missing its OAuth client ID.");
        }

        var pending = await remoteStore.GetPendingGoogleConnectionAsync(cancellationToken);
        if (pending is null || !string.Equals(pending.State, command.State, StringComparison.Ordinal))
        {
            return OperationResult.Failure("The Google connection request could not be matched. Please start the connection again.");
        }

        if (!string.Equals(pending.RedirectUri, command.RedirectUri, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Failure("The Google callback address changed. Please start the connection again.");
        }

        var result = await googleProvider.CompleteConnectionAsync(
            configuration.GoogleOAuthClientId,
            command.Code,
            pending.CodeVerifier,
            command.RedirectUri,
            cancellationToken);
        await remoteStore.SaveGoogleConnectionAsync(
            result.TokenSet,
            result.ConnectedAtUtc,
            cancellationToken);
        await remoteStore.ClearPendingGoogleConnectionAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DisconnectGoogleAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        await remoteStore.DisconnectGoogleAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult<EncryptedBackupDownloadFile>> CreateEncryptedBackupAsync(
        UserContext user,
        CreateEncryptedBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var passphraseError = ValidatePassphrase(command.Passphrase);
        if (passphraseError is not null)
        {
            return OperationResult<EncryptedBackupDownloadFile>.Failure(passphraseError);
        }

        var backup = await backupService.CreateBackupAsync(user, new CreateBackupCommand(command.Kind), cancellationToken);
        if (!backup.Succeeded || backup.Value is null)
        {
            return OperationResult<EncryptedBackupDownloadFile>.Failure(backup.Errors.ToArray());
        }

        return OperationResult<EncryptedBackupDownloadFile>.Success(
            await encryptionService.EncryptBackupAsync(backup.Value, command.Passphrase, cancellationToken));
    }

    public async Task<OperationResult<GoogleDriveUploadResult>> UploadGoogleDriveBackupAsync(
        UserContext user,
        UploadGoogleDriveBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var encrypted = await CreateEncryptedBackupAsync(
            user,
            new CreateEncryptedBackupCommand(command.Passphrase),
            cancellationToken);
        if (!encrypted.Succeeded || encrypted.Value is null)
        {
            return OperationResult<GoogleDriveUploadResult>.Failure(encrypted.Errors.ToArray());
        }

        var tokenResult = await GetFreshGoogleTokenAsync(user, cancellationToken);
        if (!tokenResult.Succeeded || tokenResult.Value is null)
        {
            return OperationResult<GoogleDriveUploadResult>.Failure(tokenResult.Errors.ToArray());
        }

        var upload = await googleProvider.UploadDriveBackupAsync(tokenResult.Value, encrypted.Value, cancellationToken);
        await remoteStore.AddHistoryAsync(
            new RemoteBackupHistoryItem("Google Drive", upload.FileName, upload.FileId, upload.CreatedAtUtc, upload.SizeBytes, "Encrypted backup uploaded."),
            cancellationToken);
        return OperationResult<GoogleDriveUploadResult>.Success(upload);
    }

    public async Task<OperationResult<GmailDraftResult>> CreateGmailBackupDraftAsync(
        UserContext user,
        CreateGmailBackupDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RecipientEmail) || !command.RecipientEmail.Contains('@', StringComparison.Ordinal))
        {
            return OperationResult<GmailDraftResult>.Failure("Enter the email address that should receive the backup draft.");
        }

        var encrypted = await CreateEncryptedBackupAsync(
            user,
            new CreateEncryptedBackupCommand(command.Passphrase),
            cancellationToken);
        if (!encrypted.Succeeded || encrypted.Value is null)
        {
            return OperationResult<GmailDraftResult>.Failure(encrypted.Errors.ToArray());
        }

        var tokenResult = await GetFreshGoogleTokenAsync(user, cancellationToken);
        if (!tokenResult.Succeeded || tokenResult.Value is null)
        {
            return OperationResult<GmailDraftResult>.Failure(tokenResult.Errors.ToArray());
        }

        var draft = await googleProvider.CreateGmailDraftAsync(
            tokenResult.Value,
            encrypted.Value,
            command.RecipientEmail.Trim(),
            cancellationToken);
        await remoteStore.AddHistoryAsync(
            new RemoteBackupHistoryItem("Gmail draft", draft.FileName, draft.DraftId, draft.CreatedAtUtc, draft.SizeBytes, "Encrypted backup attached to a Gmail draft."),
            cancellationToken);
        return OperationResult<GmailDraftResult>.Success(draft);
    }

    public async Task<OperationResult<IReadOnlyList<GoogleDriveBackupFile>>> ListGoogleDriveBackupsAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var tokenResult = await GetFreshGoogleTokenAsync(user, cancellationToken);
        if (!tokenResult.Succeeded || tokenResult.Value is null)
        {
            return OperationResult<IReadOnlyList<GoogleDriveBackupFile>>.Failure(tokenResult.Errors.ToArray());
        }

        return OperationResult<IReadOnlyList<GoogleDriveBackupFile>>.Success(
            await googleProvider.ListDriveBackupsAsync(tokenResult.Value, cancellationToken));
    }

    public async Task<OperationResult<BackupRestorePreview>> PreviewGoogleDriveRestoreAsync(
        UserContext user,
        PreviewGoogleDriveRestoreCommand command,
        CancellationToken cancellationToken = default)
    {
        var decrypted = await DownloadAndDecryptAsync(user, command.DriveFileId, command.Passphrase, cancellationToken);
        if (!decrypted.Succeeded || decrypted.Value is null)
        {
            return OperationResult<BackupRestorePreview>.Failure(decrypted.Errors.ToArray());
        }

        return await backupService.PreviewRestoreAsync(
            user,
            new ValidateBackupCommand(decrypted.Value.FileName, decrypted.Value.Content),
            cancellationToken);
    }

    public async Task<OperationResult<BackupRestoreResult>> RestoreGoogleDriveBackupAsync(
        UserContext user,
        RestoreGoogleDriveBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var decrypted = await DownloadAndDecryptAsync(user, command.DriveFileId, command.Passphrase, cancellationToken);
        if (!decrypted.Succeeded || decrypted.Value is null)
        {
            return OperationResult<BackupRestoreResult>.Failure(decrypted.Errors.ToArray());
        }

        return await backupService.RestoreBackupAsync(
            user,
            new RestoreBackupCommand(decrypted.Value.FileName, decrypted.Value.Content, command.ConfirmReplaceCurrentRecords),
            cancellationToken);
    }

    private async Task<OperationResult<DecryptedBackupFile>> DownloadAndDecryptAsync(
        UserContext user,
        string driveFileId,
        string passphrase,
        CancellationToken cancellationToken)
    {
        var passphraseError = ValidatePassphrase(passphrase);
        if (passphraseError is not null)
        {
            return OperationResult<DecryptedBackupFile>.Failure(passphraseError);
        }

        var tokenResult = await GetFreshGoogleTokenAsync(user, cancellationToken);
        if (!tokenResult.Succeeded || tokenResult.Value is null)
        {
            return OperationResult<DecryptedBackupFile>.Failure(tokenResult.Errors.ToArray());
        }

        var encrypted = await googleProvider.DownloadDriveBackupAsync(tokenResult.Value, driveFileId, cancellationToken);
        try
        {
            return OperationResult<DecryptedBackupFile>.Success(
                await encryptionService.DecryptBackupAsync(encrypted.Content, passphrase, encrypted.FileName, cancellationToken));
        }
        catch (InvalidDataException)
        {
            return OperationResult<DecryptedBackupFile>.Failure("The encrypted backup could not be opened with that passphrase.");
        }
        catch (CryptographicException)
        {
            return OperationResult<DecryptedBackupFile>.Failure("The encrypted backup could not be opened with that passphrase.");
        }
    }

    private async Task<OperationResult<GoogleOAuthTokenSet>> GetFreshGoogleTokenAsync(
        UserContext user,
        CancellationToken cancellationToken)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<GoogleOAuthTokenSet>.Failure(authorized.Errors.ToArray());
        }

        var configuration = await remoteStore.GetConfigurationAsync(cancellationToken);
        if (!configuration.HasGoogleClientId)
        {
            return OperationResult<GoogleOAuthTokenSet>.Failure("Add and save the Google OAuth client ID before using Google backup.");
        }

        var token = await remoteStore.GetGoogleTokensAsync(cancellationToken);
        if (token is null)
        {
            return OperationResult<GoogleOAuthTokenSet>.Failure("Connect Google before using Google Drive or Gmail backup.");
        }

        if (token.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return OperationResult<GoogleOAuthTokenSet>.Success(token);
        }

        var refreshed = await googleProvider.RefreshAccessTokenAsync(configuration.GoogleOAuthClientId, token, cancellationToken);
        await remoteStore.SaveGoogleTokensAsync(refreshed, cancellationToken);
        return OperationResult<GoogleOAuthTokenSet>.Success(refreshed);
    }

    private static string? ValidatePassphrase(string passphrase)
    {
        return passphrase.Length < MinimumPassphraseLength
            ? "Use a backup passphrase that is at least 8 characters long."
            : null;
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
