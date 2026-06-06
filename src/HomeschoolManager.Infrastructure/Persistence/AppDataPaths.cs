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
}
