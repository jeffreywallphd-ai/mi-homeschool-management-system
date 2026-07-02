using System.Text.Json;

namespace HomeschoolManager.Infrastructure.Production;

public sealed record ProductionDataRootMigrationResult(
    bool Migrated,
    string SourceRoot,
    string TargetRoot,
    string Message);

public static class ProductionDataRootMigrator
{
    private static readonly string[] FamilyDataFolders =
    [
        "data",
        "files",
        "templates",
        "backups",
        "config",
        "secrets",
        "logs"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static ProductionDataRootMigrationResult MigrateLegacyDesktopRootIfNeeded(
        ProductionPathProvider paths,
        string? legacyRootOverride = null)
    {
        var sourceRoot = legacyRootOverride ?? ProductionPathProvider.GetLegacyDesktopRoot();
        var targetRoot = paths.Root;
        if (paths.HostMode != ProductionHostMode.Desktop)
        {
            return Skipped(sourceRoot, targetRoot, "Legacy desktop data migration applies only to desktop mode.");
        }

        if (SamePath(sourceRoot, targetRoot))
        {
            return Skipped(sourceRoot, targetRoot, "Legacy and target data folders are the same.");
        }

        if (!Directory.Exists(sourceRoot) || !HasFamilyData(sourceRoot))
        {
            return Skipped(sourceRoot, targetRoot, "No legacy desktop family data was found.");
        }

        if (HasFamilyData(targetRoot))
        {
            return Skipped(sourceRoot, targetRoot, "The new desktop data folder already contains family data.");
        }

        Directory.CreateDirectory(targetRoot);
        foreach (var folder in FamilyDataFolders)
        {
            var source = Path.Combine(sourceRoot, folder);
            if (!Directory.Exists(source))
            {
                continue;
            }

            var destination = Path.Combine(targetRoot, folder);
            CopyDirectory(source, destination, targetRoot);
        }

        WriteMarker(sourceRoot, targetRoot);
        return new ProductionDataRootMigrationResult(
            true,
            sourceRoot,
            targetRoot,
            $"Copied legacy desktop family data from {sourceRoot} to {targetRoot}. The legacy folder was left in place.");
    }

    public static bool HasFamilyData(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return false;
        }

        return FamilyDataFolders
            .Select(folder => Path.Combine(root, folder))
            .Any(directory => Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any());
    }

    private static ProductionDataRootMigrationResult Skipped(string sourceRoot, string targetRoot, string message)
    {
        return new ProductionDataRootMigrationResult(false, sourceRoot, targetRoot, message);
    }

    private static void CopyDirectory(string source, string destination, string targetRoot)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            var target = SafeTargetPath(destination, relative, targetRoot);
            Directory.CreateDirectory(target);
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = SafeTargetPath(destination, relative, targetRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static string SafeTargetPath(string destinationRoot, string relativePath, string targetRoot)
    {
        var target = Path.GetFullPath(Path.Combine(destinationRoot, relativePath));
        var root = Path.GetFullPath(targetRoot);
        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Legacy data migration could not prepare a safe target path.");
        }

        return target;
    }

    private static void WriteMarker(string sourceRoot, string targetRoot)
    {
        var config = Path.Combine(targetRoot, "config");
        Directory.CreateDirectory(config);
        var marker = new
        {
            migratedAtUtc = DateTimeOffset.UtcNow,
            sourceRoot,
            targetRoot,
            note = "Legacy desktop family data was copied to the update-safe desktop data folder. The original folder was left in place."
        };
        File.WriteAllText(
            Path.Combine(config, "legacy-desktop-data-migration.json"),
            JsonSerializer.Serialize(marker, JsonOptions));
    }

    private static bool SamePath(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
