namespace HomeschoolManager.Web.Services;

public sealed record ProductionStatus(
    string HostMode,
    bool IsServiceMode,
    string ServiceName,
    string DataRoot,
    string SettingsPath,
    string AdminPortalUrl,
    string StudentPortalUrl,
    string AdminSharing,
    string StudentSharing);

public sealed class ProductionStatusService
{
    private readonly IConfiguration configuration;

    public ProductionStatusService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public ProductionStatus GetStatus()
    {
        var hostMode = configuration["HomeschoolManager:ProductionHostMode"] ?? "Desktop";
        var dataRoot = configuration["HomeschoolManager:DataRoot"] ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeschoolManager");
        var settingsPath = configuration["HomeschoolManager:ProductionSettingsPath"] ?? Path.Combine(dataRoot, "config", "production-settings.json");
        var adminPortalUrl = configuration["HomeschoolManager:AdminPortalUrl"] ?? "";
        var studentPortalUrl = configuration["HomeschoolManager:StudentPortalUrl"] ?? configuration["HomeschoolManager:StudentPortalBaseUrl"] ?? "";
        var isServiceMode = string.Equals(hostMode, "Service", StringComparison.OrdinalIgnoreCase);

        return new ProductionStatus(
            isServiceMode ? "Background service" : "Desktop launcher",
            isServiceMode,
            configuration["HomeschoolManager:ProductionServiceName"] ?? "HomeschoolManager",
            dataRoot,
            settingsPath,
            adminPortalUrl,
            studentPortalUrl,
            SharingLabel(adminPortalUrl),
            SharingLabel(studentPortalUrl));
    }

    private static string SharingLabel(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "Not reported";
        }

        return string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            ? "This computer only"
            : "Wi-Fi sharing";
    }
}
