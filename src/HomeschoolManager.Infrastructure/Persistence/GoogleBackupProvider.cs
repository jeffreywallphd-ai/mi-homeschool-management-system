using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeschoolManager.Application.Backups;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class GoogleBackupProvider : IGoogleBackupProvider
{
    private const string DriveScope = "https://www.googleapis.com/auth/drive.file";
    private const string GmailScope = "https://www.googleapis.com/auth/gmail.compose";
    private const string BackupFolderName = "Homeschool Manager Backups";
    private const string BackupContentType = "application/octet-stream";
    private const long GmailAttachmentSafetyLimit = 18L * 1024L * 1024L;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public GoogleBackupProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public string BuildAuthorizationUrl(
        string clientId,
        string redirectUri,
        string state,
        string codeChallenge)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = $"{DriveScope} {GmailScope}",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        return "https://accounts.google.com/o/oauth2/v2/auth?" + BuildQuery(query);
    }

    public async Task<GoogleConnectionResult> CompleteConnectionAsync(
        string clientId,
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var response = await PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        }, cancellationToken);

        if (string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            throw new InvalidOperationException("Google did not return a refresh token. Disconnect and connect again with consent.");
        }

        var tokenSet = new GoogleOAuthTokenSet(
            response.AccessToken,
            response.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
            response.Scope);
        return new GoogleConnectionResult(DateTimeOffset.UtcNow, response.Scope, tokenSet);
    }

    public async Task<GoogleOAuthTokenSet> RefreshAccessTokenAsync(
        string clientId,
        GoogleOAuthTokenSet tokenSet,
        CancellationToken cancellationToken = default)
    {
        var response = await PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["refresh_token"] = tokenSet.RefreshToken,
            ["grant_type"] = "refresh_token"
        }, cancellationToken);

        return tokenSet with
        {
            AccessToken = response.AccessToken,
            AccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
            Scope = string.IsNullOrWhiteSpace(response.Scope) ? tokenSet.Scope : response.Scope
        };
    }

    public async Task<GoogleDriveUploadResult> UploadDriveBackupAsync(
        GoogleOAuthTokenSet tokenSet,
        EncryptedBackupDownloadFile encryptedBackup,
        CancellationToken cancellationToken = default)
    {
        var folderId = await GetOrCreateBackupFolderAsync(tokenSet, cancellationToken);
        var metadata = new
        {
            name = encryptedBackup.FileName,
            parents = new[] { folderId },
            mimeType = BackupContentType,
            appProperties = new Dictionary<string, string>
            {
                ["homeschoolManagerBackup"] = "true",
                ["encryptedBackup"] = "true",
                ["packageType"] = "homeschool-manager.encrypted-full-backup"
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable&fields=id,name,size,createdTime");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSet.AccessToken);
        request.Headers.Add("X-Upload-Content-Type", BackupContentType);
        request.Headers.Add("X-Upload-Content-Length", encryptedBackup.Content.LongLength.ToString());
        request.Content = JsonContent(metadata);

        using var sessionResponse = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(sessionResponse, "Google Drive upload could not be started.", cancellationToken);
        var uploadUri = sessionResponse.Headers.Location
            ?? throw new InvalidOperationException("Google Drive did not return an upload address.");

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUri);
        uploadRequest.Content = new ByteArrayContent(encryptedBackup.Content);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(BackupContentType);
        using var uploadResponse = await httpClient.SendAsync(uploadRequest, cancellationToken);
        await EnsureSuccessAsync(uploadResponse, "Google Drive upload did not finish.", cancellationToken);
        var file = await ReadJsonAsync<DriveFileResponse>(uploadResponse, cancellationToken);

        return new GoogleDriveUploadResult(
            file.Id,
            file.Name,
            ParseLong(file.Size),
            file.CreatedTime ?? DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<GoogleDriveBackupFile>> ListDriveBackupsAsync(
        GoogleOAuthTokenSet tokenSet,
        CancellationToken cancellationToken = default)
    {
        var query = "appProperties has { key='homeschoolManagerBackup' and value='true'} and trashed=false";
        var url = "https://www.googleapis.com/drive/v3/files?" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = query,
            ["fields"] = "files(id,name,size,createdTime)",
            ["orderBy"] = "createdTime desc",
            ["pageSize"] = "25"
        });
        using var request = Authorized(HttpMethod.Get, url, tokenSet);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Google Drive backups could not be listed.", cancellationToken);
        var result = await ReadJsonAsync<DriveFileListResponse>(response, cancellationToken);
        return result.Files
            .Select(file => new GoogleDriveBackupFile(
                file.Id,
                file.Name,
                ParseLong(file.Size),
                file.CreatedTime ?? DateTimeOffset.UtcNow))
            .ToArray();
    }

    public async Task<RemoteEncryptedBackupFile> DownloadDriveBackupAsync(
        GoogleOAuthTokenSet tokenSet,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        var metadataUrl = $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(fileId)}?fields=id,name,size,createdTime";
        using var metadataRequest = Authorized(HttpMethod.Get, metadataUrl, tokenSet);
        using var metadataResponse = await httpClient.SendAsync(metadataRequest, cancellationToken);
        await EnsureSuccessAsync(metadataResponse, "Google Drive backup details could not be read.", cancellationToken);
        var metadata = await ReadJsonAsync<DriveFileResponse>(metadataResponse, cancellationToken);

        var downloadUrl = $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(fileId)}?alt=media";
        using var downloadRequest = Authorized(HttpMethod.Get, downloadUrl, tokenSet);
        using var downloadResponse = await httpClient.SendAsync(downloadRequest, cancellationToken);
        await EnsureSuccessAsync(downloadResponse, "Google Drive backup could not be downloaded.", cancellationToken);
        var content = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        return new RemoteEncryptedBackupFile(
            metadata.Name,
            BackupContentType,
            content,
            content.LongLength,
            metadata.CreatedTime ?? DateTimeOffset.UtcNow);
    }

    public async Task<GmailDraftResult> CreateGmailDraftAsync(
        GoogleOAuthTokenSet tokenSet,
        EncryptedBackupDownloadFile encryptedBackup,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        if (encryptedBackup.Content.LongLength > GmailAttachmentSafetyLimit)
        {
            throw new InvalidOperationException("This encrypted backup is too large for a reliable Gmail attachment. Use Google Drive backup instead.");
        }

        var mime = BuildMimeMessage(recipientEmail, encryptedBackup);
        var raw = Base64Url(Encoding.UTF8.GetBytes(mime));
        var body = new
        {
            message = new
            {
                raw
            }
        };

        using var request = Authorized(HttpMethod.Post, "https://gmail.googleapis.com/gmail/v1/users/me/drafts", tokenSet);
        request.Content = JsonContent(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Gmail draft could not be created.", cancellationToken);
        var draft = await ReadJsonAsync<GmailDraftResponse>(response, cancellationToken);
        return new GmailDraftResult(
            draft.Id,
            encryptedBackup.FileName,
            encryptedBackup.Content.LongLength,
            DateTimeOffset.UtcNow);
    }

    private async Task<string> GetOrCreateBackupFolderAsync(
        GoogleOAuthTokenSet tokenSet,
        CancellationToken cancellationToken)
    {
        var query = $"mimeType='application/vnd.google-apps.folder' and name='{BackupFolderName.Replace("'", "\\'", StringComparison.Ordinal)}' and trashed=false";
        var listUrl = "https://www.googleapis.com/drive/v3/files?" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = query,
            ["fields"] = "files(id,name)",
            ["pageSize"] = "1"
        });
        using (var listRequest = Authorized(HttpMethod.Get, listUrl, tokenSet))
        using (var listResponse = await httpClient.SendAsync(listRequest, cancellationToken))
        {
            await EnsureSuccessAsync(listResponse, "Google Drive backup folder could not be checked.", cancellationToken);
            var list = await ReadJsonAsync<DriveFileListResponse>(listResponse, cancellationToken);
            var existing = list.Files.FirstOrDefault();
            if (existing is not null)
            {
                return existing.Id;
            }
        }

        using var createRequest = Authorized(HttpMethod.Post, "https://www.googleapis.com/drive/v3/files?fields=id,name", tokenSet);
        createRequest.Content = JsonContent(new
        {
            name = BackupFolderName,
            mimeType = "application/vnd.google-apps.folder"
        });
        using var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
        await EnsureSuccessAsync(createResponse, "Google Drive backup folder could not be created.", cancellationToken);
        var created = await ReadJsonAsync<DriveFileResponse>(createResponse, cancellationToken);
        return created.Id;
    }

    private async Task<TokenResponse> PostTokenAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(values),
            cancellationToken);
        await EnsureSuccessAsync(response, "Google connection could not be completed.", cancellationToken);
        return await ReadJsonAsync<TokenResponse>(response, cancellationToken);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, GoogleOAuthTokenSet tokenSet)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSet.AccessToken);
        return request;
    }

    private static HttpContent JsonContent<T>(T value)
    {
        return new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Google returned an unreadable response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string userMessage,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"{userMessage} Google returned {(int)response.StatusCode}. {TrimDetails(details)}");
    }

    private static string TrimDetails(string details)
    {
        return string.IsNullOrWhiteSpace(details)
            ? ""
            : details.Length > 240
                ? details[..240]
                : details;
    }

    private static string BuildMimeMessage(string recipientEmail, EncryptedBackupDownloadFile encryptedBackup)
    {
        var boundary = "hm-backup-" + Guid.NewGuid().ToString("N");
        var attachment = Convert.ToBase64String(encryptedBackup.Content, Base64FormattingOptions.InsertLineBreaks);
        var builder = new StringBuilder();
        builder.AppendLine($"To: {recipientEmail}");
        builder.AppendLine("Subject: Homeschool Manager encrypted backup");
        builder.AppendLine("MIME-Version: 1.0");
        builder.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        builder.AppendLine();
        builder.AppendLine($"--{boundary}");
        builder.AppendLine("Content-Type: text/plain; charset=\"UTF-8\"");
        builder.AppendLine();
        builder.AppendLine("This Gmail draft contains an encrypted Homeschool Manager backup. Keep the backup passphrase in a separate safe place; it is required to restore this file.");
        builder.AppendLine();
        builder.AppendLine($"--{boundary}");
        builder.AppendLine($"Content-Type: {BackupContentType}; name=\"{encryptedBackup.FileName}\"");
        builder.AppendLine("Content-Transfer-Encoding: base64");
        builder.AppendLine($"Content-Disposition: attachment; filename=\"{encryptedBackup.FileName}\"");
        builder.AppendLine();
        builder.AppendLine(attachment);
        builder.AppendLine($"--{boundary}--");
        return builder.ToString();
    }

    private static string BuildQuery(IReadOnlyDictionary<string, string?> values)
    {
        return string.Join("&", values
            .Where(pair => pair.Value is not null)
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static long ParseLong(string value)
    {
        return long.TryParse(value, out var parsed) ? parsed : 0;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("scope")] string Scope);

    private sealed record DriveFileListResponse(IReadOnlyList<DriveFileResponse> Files);

    private sealed record DriveFileResponse(
        string Id,
        string Name,
        string Size,
        DateTimeOffset? CreatedTime);

    private sealed record GmailDraftResponse(string Id);
}
