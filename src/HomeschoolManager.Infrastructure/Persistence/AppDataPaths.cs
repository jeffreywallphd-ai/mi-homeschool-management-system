using HomeschoolManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class AppDataPaths
{
    private readonly HomeschoolManagerOptions options;

    public AppDataPaths(IOptions<HomeschoolManagerOptions> options)
    {
        this.options = options.Value;
    }

    public string DataRoot
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(options.DataRoot))
            {
                return options.DataRoot;
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderName = options.UseDevelopmentDataRoot ? "HomeschoolManager-Dev" : "HomeschoolManager";
            return Path.Combine(localAppData, folderName);
        }
    }

    public string DataDirectory => Path.Combine(DataRoot, "data");

    public string DatabasePath => Path.Combine(DataDirectory, "homeschool.db");

    public string FilesDirectory => Path.Combine(DataRoot, "files");

    public string TemplatesDirectory => Path.Combine(DataRoot, "templates");

    public string BackupsDirectory => Path.Combine(DataRoot, "backups");

    public string AutomaticBackupsDirectory => Path.Combine(BackupsDirectory, "automatic");

    public string ManualBackupsDirectory => Path.Combine(BackupsDirectory, "manual");

    public string ExportsDirectory => Path.Combine(BackupsDirectory, "exports");

    public string LogsDirectory => Path.Combine(DataRoot, "logs");

    public string ConfigDirectory => Path.Combine(DataRoot, "config");

    public string SecretsDirectory => Path.Combine(DataRoot, "secrets");
}
