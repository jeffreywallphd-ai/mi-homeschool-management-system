using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomeschoolManager.Application.Backups;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Students;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class LocalBackupArchiveStore : IBackupArchiveStore
{
    private const string PackageType = "homeschool-manager.full-backup";
    private const int FormatVersion = 1;
    private const string ContentTypeZip = "application/zip";
    private static readonly string[] IncludedRootFolders = ["data", "files", "templates", "config"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly AppDataPaths paths;
    private readonly IHomeschoolRepository repository;
    private readonly SemaphoreSlim gate = new(1, 1);

    public LocalBackupArchiveStore(AppDataPaths paths, IHomeschoolRepository repository)
    {
        this.paths = paths;
        this.repository = repository;
    }

    public async Task<BackupDownloadFile> CreateBackupAsync(
        string createdBy,
        BackupKind kind,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await CreateBackupUnlockedAsync(createdBy, kind, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<BackupValidationReport> ValidateBackupAsync(
        byte[] content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        var errors = new List<string>();
        var warnings = new List<string>();
        try
        {
            using var stream = new MemoryStream(content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var manifest = ReadJsonEntry<BackupManifest>(archive, "manifest.json", errors);
            var checksums = ReadJsonEntry<List<BackupFileChecksum>>(archive, "checksums.json", errors) ?? [];

            if (manifest is null)
            {
                return Invalid(errors, warnings);
            }

            ValidateManifestBasics(manifest, errors);
            if (archive.GetEntry("data/homeschool.db") is null)
            {
                errors.Add("The backup does not include the main records file.");
            }

            var checksumByPath = checksums
                .GroupBy(checksum => NormalizeArchivePath(checksum.Path), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var checksum in checksums)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = NormalizeArchivePath(checksum.Path);
                var entry = archive.GetEntry(path);
                if (entry is null)
                {
                    errors.Add($"The backup is missing {path}.");
                    continue;
                }

                await using var entryStream = entry.Open();
                var actual = await Sha256Async(entryStream, cancellationToken);
                if (!string.Equals(actual, checksum.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"The backup file {path} is damaged or has changed.");
                }
            }

            foreach (var entry in archive.Entries.Where(entry => IsIncludedDataEntry(entry.FullName)))
            {
                var path = NormalizeArchivePath(entry.FullName);
                if (!checksumByPath.ContainsKey(path))
                {
                    warnings.Add($"The backup includes {path}, but it has no checksum.");
                }
            }

            return new BackupValidationReport(
                errors.Count == 0,
                manifest,
                errors,
                warnings.Concat(manifest.Warnings).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                checksums.Count,
                checksums.Sum(checksum => checksum.SizeBytes));
        }
        catch (InvalidDataException)
        {
            errors.Add("The selected file is not a readable backup ZIP.");
        }
        catch (JsonException)
        {
            errors.Add("The backup manifest could not be read.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add("The backup could not be opened. Check that the file is available and try again.");
        }

        return Invalid(errors, warnings);

        static BackupValidationReport Invalid(List<string> errors, List<string> warnings)
        {
            return new BackupValidationReport(false, null, errors, warnings, 0, 0);
        }
    }

    public async Task<BackupRestorePreview> PreviewRestoreAsync(
        byte[] content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateBackupAsync(content, fileName, cancellationToken);
        if (!validation.IsValid || validation.Manifest is null)
        {
            throw new InvalidOperationException("Backup must be valid before restore preview.");
        }

        return new BackupRestorePreview(
            validation.Manifest,
            paths.DataRoot,
            "A safety backup of the current records will be created before restore.",
            validation.Warnings);
    }

    public async Task<BackupRestoreResult> RestoreBackupAsync(
        byte[] content,
        string fileName,
        string restoredBy,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var validation = await ValidateBackupAsync(content, fileName, cancellationToken);
            if (!validation.IsValid || validation.Manifest is null)
            {
                throw new InvalidOperationException("Backup must be valid before restore.");
            }

            var safetyBackup = await CreateBackupUnlockedAsync(restoredBy, BackupKind.PreRestore, cancellationToken);
            var restoreRoot = Path.Combine(paths.DataRoot, "restore-working", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(restoreRoot);

            try
            {
                using (var stream = new MemoryStream(content))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false))
                {
                    foreach (var entry in archive.Entries.Where(entry => IsIncludedDataEntry(entry.FullName)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var destination = SafeRestorePath(restoreRoot, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        await using var entryStream = entry.Open();
                        await using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
                        await entryStream.CopyToAsync(fileStream, cancellationToken);
                    }
                }

                foreach (var folder in IncludedRootFolders)
                {
                    var source = Path.Combine(restoreRoot, folder);
                    var destination = Path.Combine(paths.DataRoot, folder);
                    ReplaceDirectory(source, destination);
                }

                return new BackupRestoreResult(
                    validation.Manifest,
                    safetyBackup.Manifest,
                    safetyBackup.FileName,
                    validation.Warnings);
            }
            finally
            {
                TryDeleteDirectory(restoreRoot);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<BackupDownloadFile> CreateBackupUnlockedAsync(
        string createdBy,
        BackupKind kind,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(TargetDirectory(kind));
        await repository.EnsureStoreCreatedAsync(cancellationToken);

        var assembly = BuildAssembly(kind);
        var manifest = await BuildManifestAsync(createdBy, kind, assembly.Files, assembly.Warnings, cancellationToken);
        var markdown = BuildManifestMarkdown(manifest);
        var checksums = assembly.Files
            .Select(file => new BackupFileChecksum(file.ArchivePath, file.Sha256, file.SizeBytes))
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipJson(archive, "manifest.json", manifest);
            WriteZipText(archive, "manifest.md", markdown);
            WriteZipJson(archive, "checksums.json", checksums);
            foreach (var file in assembly.Files.OrderBy(file => file.ArchivePath, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry(file.ArchivePath, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = File.OpenRead(file.FullPath);
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        var content = stream.ToArray();
        var fileName = BackupFileName(manifest);
        var finalPath = Path.Combine(TargetDirectory(kind), fileName);
        await File.WriteAllBytesAsync(finalPath, content, cancellationToken);
        return new BackupDownloadFile(fileName, ContentTypeZip, content, manifest);
    }

    public async Task<IReadOnlyList<BackupHistoryItem>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        var results = new List<BackupHistoryItem>();
        foreach (var directory in BackupDirectories())
        {
            if (!Directory.Exists(directory.FullPath))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory.FullPath, "*.zip", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var info = new FileInfo(file);
                var backupId = BackupIdFor(file);
                var history = await ReadHistoryItemAsync(backupId, file, directory.Kind, info.Length, cancellationToken);
                results.Add(history);
            }
        }

        return results
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<BackupDownloadFile?> ReadBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var path = ResolveBackupId(backupId);
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        var content = await File.ReadAllBytesAsync(path, cancellationToken);
        var validation = await ValidateBackupAsync(content, Path.GetFileName(path), cancellationToken);
        return validation.Manifest is null
            ? null
            : new BackupDownloadFile(Path.GetFileName(path), ContentTypeZip, content, validation.Manifest);
    }

    public Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var path = ResolveBackupId(backupId);
        if (path is null || !File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    private BackupAssembly BuildAssembly(BackupKind kind)
    {
        var warnings = new List<string>();
        var files = new List<BackupAssemblyFile>();
        foreach (var folder in IncludedRootFolders)
        {
            var fullFolder = Path.Combine(paths.DataRoot, folder);
            if (!Directory.Exists(fullFolder))
            {
                warnings.Add($"{folder} folder was not present.");
                continue;
            }

            foreach (var fullPath in Directory.EnumerateFiles(fullFolder, "*", SearchOption.AllDirectories))
            {
                if (IsTransientDataFile(fullPath))
                {
                    continue;
                }

                var relativePath = NormalizeArchivePath(Path.GetRelativePath(paths.DataRoot, fullPath));
                var bytes = File.ReadAllBytes(fullPath);
                files.Add(new BackupAssemblyFile(
                    fullPath,
                    relativePath,
                    bytes.LongLength,
                    Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()));
            }
        }

        if (!files.Any(file => string.Equals(file.ArchivePath, "data/homeschool.db", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("The main records file was not present when the backup was created.");
        }

        return new BackupAssembly(files, warnings);
    }

    private async Task<BackupManifest> BuildManifestAsync(
        string createdBy,
        BackupKind kind,
        IReadOnlyList<BackupAssemblyFile> files,
        IReadOnlyList<string> assemblyWarnings,
        CancellationToken cancellationToken)
    {
        var household = await repository.GetHouseholdAsync(cancellationToken);
        var school = await repository.GetSchoolProfileAsync(cancellationToken);
        var students = await repository.GetStudentsAsync(cancellationToken);
        var schemaVersion = ReadSchemaVersion();
        var warnings = assemblyWarnings.ToList();
        if (files.Count == 0)
        {
            warnings.Add("No source files were found for this backup.");
        }

        return new BackupManifest(
            PackageType,
            FormatVersion,
            kind,
            DateTimeOffset.UtcNow,
            createdBy,
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "development",
            schemaVersion,
            SourceDataRootMode(),
            household?.Name ?? "",
            school?.SchoolName ?? "",
            students.Select(ToStudentSummary).ToArray(),
            IncludedRootFolders,
            files.Count,
            files.Sum(file => file.SizeBytes),
            warnings);
    }

    private int ReadSchemaVersion()
    {
        try
        {
            if (!File.Exists(paths.DatabasePath))
            {
                return 0;
            }

            using var stream = File.OpenRead(paths.DatabasePath);
            var document = JsonSerializer.Deserialize<AppDataDocument>(stream, JsonOptions);
            return document?.SchemaVersion ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private string SourceDataRootMode()
    {
        var root = Path.GetFullPath(paths.DataRoot);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return !string.IsNullOrWhiteSpace(programData)
            && root.StartsWith(Path.GetFullPath(programData), StringComparison.OrdinalIgnoreCase)
            ? "Service"
            : "Desktop";
    }

    private string TargetDirectory(BackupKind kind)
    {
        return kind == BackupKind.Manual
            ? paths.ManualBackupsDirectory
            : paths.AutomaticBackupsDirectory;
    }

    private IEnumerable<(BackupKind Kind, string FullPath)> BackupDirectories()
    {
        yield return (BackupKind.Manual, paths.ManualBackupsDirectory);
        yield return (BackupKind.Automatic, paths.AutomaticBackupsDirectory);
    }

    private async Task<BackupHistoryItem> ReadHistoryItemAsync(
        string backupId,
        string path,
        BackupKind fallbackKind,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllBytesAsync(path, cancellationToken);
            var validation = await ValidateBackupAsync(content, Path.GetFileName(path), cancellationToken);
            if (validation.Manifest is not null)
            {
                return new BackupHistoryItem(
                    backupId,
                    Path.GetFileName(path),
                    validation.Manifest.BackupKind,
                    validation.Manifest.CreatedAtUtc,
                    validation.Manifest.HouseholdName,
                    validation.Manifest.SchoolName,
                    validation.Manifest.Students.Count,
                    sizeBytes,
                    validation.IsValid,
                    validation.Errors.Concat(validation.Warnings).ToArray());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
        }

        return new BackupHistoryItem(
            backupId,
            Path.GetFileName(path),
            fallbackKind,
            File.GetCreationTimeUtc(path),
            "",
            "",
            0,
            sizeBytes,
            false,
            ["This backup could not be read."]);
    }

    private static void ValidateManifestBasics(BackupManifest manifest, List<string> errors)
    {
        if (!string.Equals(manifest.PackageType, PackageType, StringComparison.Ordinal))
        {
            errors.Add("This ZIP is not a Homeschool Manager full backup.");
        }

        if (manifest.FormatVersion != FormatVersion)
        {
            errors.Add("This backup format is not supported by this version of Homeschool Manager.");
        }
    }

    private static T? ReadJsonEntry<T>(ZipArchive archive, string path, List<string> errors)
    {
        var entry = archive.GetEntry(path);
        if (entry is null)
        {
            errors.Add($"The backup is missing {path}.");
            return default;
        }

        using var stream = entry.Open();
        return JsonSerializer.Deserialize<T>(stream, JsonOptions);
    }

    private string BackupIdFor(string fullPath)
    {
        return NormalizeArchivePath(Path.GetRelativePath(paths.BackupsDirectory, fullPath));
    }

    private string? ResolveBackupId(string backupId)
    {
        if (string.IsNullOrWhiteSpace(backupId))
        {
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(paths.BackupsDirectory, backupId.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(paths.BackupsDirectory);
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetExtension(candidate), ".zip", StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }

    private static async Task<string> Sha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BackupFileName(BackupManifest manifest)
    {
        var prefix = manifest.BackupKind switch
        {
            BackupKind.PreRestore => "pre-restore-safety-backup",
            BackupKind.Automatic => "automatic-backup",
            _ => "manual-backup"
        };
        return $"{prefix}-{manifest.CreatedAtUtc:yyyyMMdd-HHmmss}.zip";
    }

    private static void WriteZipJson<T>(ZipArchive archive, string path, T value)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(JsonSerializer.Serialize(value, JsonOptions));
    }

    private static void WriteZipText(ZipArchive archive, string path, string value)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(value);
    }

    private static string BuildManifestMarkdown(BackupManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Homeschool Manager Full Backup");
        builder.AppendLine();
        builder.AppendLine($"- Created: {manifest.CreatedAtUtc.ToLocalTime():g}");
        builder.AppendLine($"- Created by: {manifest.CreatedBy}");
        builder.AppendLine($"- Backup type: {manifest.BackupKind}");
        builder.AppendLine($"- Household: {Blank(manifest.HouseholdName)}");
        builder.AppendLine($"- School: {Blank(manifest.SchoolName)}");
        builder.AppendLine($"- Students: {manifest.Students.Count}");
        builder.AppendLine($"- Files: {manifest.FileCount}");
        builder.AppendLine($"- Size: {manifest.TotalBytes} bytes");
        builder.AppendLine();
        builder.AppendLine("This is a full local app backup for restore in Homeschool Manager. It is different from a student archive export.");
        if (manifest.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Warnings");
            foreach (var warning in manifest.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        return builder.ToString();
    }

    private static string Blank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not recorded" : value;
    }

    private static BackupStudentSummary ToStudentSummary(Student student)
    {
        return new BackupStudentSummary(student.Id, $"{student.FirstName} {student.LastName}".Trim(), student.GradeLevel);
    }

    private static bool IsIncludedDataEntry(string path)
    {
        var normalized = NormalizeArchivePath(path);
        return !string.IsNullOrWhiteSpace(normalized)
            && IncludedRootFolders.Any(folder => normalized.StartsWith($"{folder}/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTransientDataFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".tmp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".wal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".shm", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeRestorePath(string root, string archivePath)
    {
        var normalized = NormalizeArchivePath(archivePath);
        var destination = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var rootPath = Path.GetFullPath(root);
        if (!destination.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Backup contains an unsafe file path.");
        }

        return destination;
    }

    private static string NormalizeArchivePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static void ReplaceDirectory(string source, string destination)
    {
        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, recursive: true);
        }

        if (Directory.Exists(source))
        {
            CopyDirectory(source, destination);
        }
        else
        {
            Directory.CreateDirectory(destination);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record BackupAssembly(
        IReadOnlyList<BackupAssemblyFile> Files,
        IReadOnlyList<string> Warnings);

    private sealed record BackupAssemblyFile(
        string FullPath,
        string ArchivePath,
        long SizeBytes,
        string Sha256);
}
