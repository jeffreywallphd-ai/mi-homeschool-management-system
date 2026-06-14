using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomeschoolManager.Application.Backups;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class LocalBackupEncryptionService : IBackupEncryptionService
{
    private const string PackageType = "homeschool-manager.encrypted-full-backup";
    private const int FormatVersion = 1;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int KdfIterations = 210_000;
    private const string ContentType = "application/octet-stream";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<EncryptedBackupDownloadFile> EncryptBackupAsync(
        BackupDownloadFile sourceBackup,
        string passphrase,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[sourceBackup.Content.Length];
        var key = DeriveKey(passphrase, salt);

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, sourceBackup.Content, ciphertext, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        var metadata = new EncryptedBackupPackageMetadata(
            PackageType,
            FormatVersion,
            sourceBackup.FileName,
            DateTimeOffset.UtcNow,
            "PBKDF2-HMAC-SHA256",
            KdfIterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag));

        using var package = new MemoryStream();
        using (var archive = new ZipArchive(package, ZipArchiveMode.Create, leaveOpen: true))
        {
            var metadataEntry = archive.CreateEntry("encrypted-backup.json", CompressionLevel.Optimal);
            await using (var metadataStream = metadataEntry.Open())
            {
                await JsonSerializer.SerializeAsync(metadataStream, metadata, JsonOptions, cancellationToken);
            }

            var payloadEntry = archive.CreateEntry("payload.bin", CompressionLevel.NoCompression);
            await using (var payloadStream = payloadEntry.Open())
            {
                await payloadStream.WriteAsync(ciphertext, cancellationToken);
            }
        }

        return new EncryptedBackupDownloadFile(
            EncryptedFileName(sourceBackup.Manifest),
            ContentType,
            package.ToArray(),
            sourceBackup.Manifest,
            metadata.CreatedAtUtc);
    }

    public async Task<DecryptedBackupFile> DecryptBackupAsync(
        byte[] encryptedContent,
        string passphrase,
        string encryptedFileName,
        CancellationToken cancellationToken = default)
    {
        using var package = new MemoryStream(encryptedContent);
        using var archive = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: false);
        var metadataEntry = archive.GetEntry("encrypted-backup.json")
            ?? throw new InvalidDataException("Encrypted backup metadata is missing.");
        var payloadEntry = archive.GetEntry("payload.bin")
            ?? throw new InvalidDataException("Encrypted backup payload is missing.");

        EncryptedBackupPackageMetadata metadata;
        await using (var metadataStream = metadataEntry.Open())
        {
            metadata = await JsonSerializer.DeserializeAsync<EncryptedBackupPackageMetadata>(
                    metadataStream,
                    JsonOptions,
                    cancellationToken)
                ?? throw new InvalidDataException("Encrypted backup metadata could not be read.");
        }

        ValidateMetadata(metadata);
        var salt = Convert.FromBase64String(metadata.SaltBase64);
        var nonce = Convert.FromBase64String(metadata.NonceBase64);
        var tag = Convert.FromBase64String(metadata.TagBase64);
        byte[] ciphertext;
        await using (var payloadStream = payloadEntry.Open())
        using (var memory = new MemoryStream())
        {
            await payloadStream.CopyToAsync(memory, cancellationToken);
            ciphertext = memory.ToArray();
        }

        var plaintext = new byte[ciphertext.Length];
        var key = DeriveKey(passphrase, salt, metadata.KdfIterations);
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(ciphertext);
        }

        return new DecryptedBackupFile(metadata.SourceFileName, plaintext);
    }

    private static byte[] DeriveKey(string passphrase, byte[] salt, int iterations = KdfIterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeySize);
    }

    private static void ValidateMetadata(EncryptedBackupPackageMetadata metadata)
    {
        if (!string.Equals(metadata.PackageType, PackageType, StringComparison.Ordinal))
        {
            throw new InvalidDataException("This is not a Homeschool Manager encrypted backup.");
        }

        if (metadata.FormatVersion != FormatVersion)
        {
            throw new InvalidDataException("This encrypted backup format is not supported.");
        }

        if (metadata.KdfIterations < 100_000)
        {
            throw new InvalidDataException("This encrypted backup uses an unsupported key-strength setting.");
        }
    }

    private static string EncryptedFileName(BackupManifest manifest)
    {
        return $"encrypted-full-backup-{manifest.CreatedAtUtc:yyyyMMdd-HHmmss}.hsmbak";
    }
}
