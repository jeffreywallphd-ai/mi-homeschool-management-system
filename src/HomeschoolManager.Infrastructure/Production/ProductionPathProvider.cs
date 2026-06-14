namespace HomeschoolManager.Infrastructure.Production;

public sealed class ProductionPathProvider
{
    public ProductionPathProvider(string? rootOverride = null, ProductionHostMode hostMode = ProductionHostMode.Desktop)
    {
        HostMode = hostMode;
        Root = string.IsNullOrWhiteSpace(rootOverride)
            ? GetDefaultRoot(hostMode)
            : rootOverride;
    }

    public ProductionHostMode HostMode { get; }

    public string Root { get; }

    public string DataDirectory => Path.Combine(Root, "data");

    public string FilesDirectory => Path.Combine(Root, "files");

    public string TemplatesDirectory => Path.Combine(Root, "templates");

    public string BackupsDirectory => Path.Combine(Root, "backups");

    public string AutomaticBackupsDirectory => Path.Combine(BackupsDirectory, "automatic");

    public string ManualBackupsDirectory => Path.Combine(BackupsDirectory, "manual");

    public string ExportsDirectory => Path.Combine(BackupsDirectory, "exports");

    public string LogsDirectory => Path.Combine(Root, "logs");

    public string ConfigDirectory => Path.Combine(Root, "config");

    public string RuntimeSettingsPath => Path.Combine(ConfigDirectory, "production-settings.json");

    public static string GetDefaultRoot(ProductionHostMode hostMode)
    {
        var folder = hostMode == ProductionHostMode.Service
            ? Environment.SpecialFolder.CommonApplicationData
            : Environment.SpecialFolder.LocalApplicationData;

        return Path.Combine(Environment.GetFolderPath(folder), "HomeschoolManager");
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(FilesDirectory);
        Directory.CreateDirectory(TemplatesDirectory);
        Directory.CreateDirectory(AutomaticBackupsDirectory);
        Directory.CreateDirectory(ManualBackupsDirectory);
        Directory.CreateDirectory(ExportsDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ConfigDirectory);
    }
}
