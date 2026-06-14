namespace HomeschoolManager.Application.Backups;

public sealed record EncryptedBackupDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content,
    BackupManifest SourceManifest,
    DateTimeOffset CreatedAtUtc);

public sealed record DecryptedBackupFile(
    string FileName,
    byte[] Content);

public sealed record EncryptedBackupPackageMetadata(
    string PackageType,
    int FormatVersion,
    string SourceFileName,
    DateTimeOffset CreatedAtUtc,
    string Kdf,
    int KdfIterations,
    string SaltBase64,
    string NonceBase64,
    string TagBase64);

public interface IBackupEncryptionService
{
    Task<EncryptedBackupDownloadFile> EncryptBackupAsync(
        BackupDownloadFile sourceBackup,
        string passphrase,
        CancellationToken cancellationToken = default);

    Task<DecryptedBackupFile> DecryptBackupAsync(
        byte[] encryptedContent,
        string passphrase,
        string encryptedFileName,
        CancellationToken cancellationToken = default);
}
