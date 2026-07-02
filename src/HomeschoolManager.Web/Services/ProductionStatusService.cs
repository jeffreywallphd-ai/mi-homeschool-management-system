using HomeschoolManager.Infrastructure.Production;

namespace HomeschoolManager.Web.Services;

public sealed record ProductionStatus(
    string HostMode,
    string AvailabilityMode,
    bool IsServiceMode,
    bool IsAlwaysAvailablePreferred,
    bool StudentAccessIsAlwaysAvailable,
    string StudentAccessSummary,
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
        var isServiceMode = string.Equals(hostMode, "Service", StringComparison.OrdinalIgnoreCase);
        var availabilityMode = ResolveAvailabilityMode(isServiceMode);
        var isAlwaysAvailablePreferred = availabilityMode == ProductionAvailabilityMode.AlwaysAvailable;
        var studentAccessIsAlwaysAvailable = isServiceMode && isAlwaysAvailablePreferred;
        var dataRoot = configuration["HomeschoolManager:DataRoot"] ?? ProductionPathProvider.GetDefaultRoot(
            isServiceMode ? ProductionHostMode.Service : ProductionHostMode.Desktop);
        var settingsPath = configuration["HomeschoolManager:ProductionSettingsPath"] ?? Path.Combine(dataRoot, "config", "production-settings.json");
        var adminPortalUrl = configuration["HomeschoolManager:AdminPortalUrl"] ?? "";
        var studentPortalUrl = configuration["HomeschoolManager:StudentPortalUrl"] ?? configuration["HomeschoolManager:StudentPortalBaseUrl"] ?? "";

        return new ProductionStatus(
            ResolveHostModeLabel(isServiceMode, isAlwaysAvailablePreferred),
            ResolveAvailabilityLabel(availabilityMode),
            isServiceMode,
            isAlwaysAvailablePreferred,
            studentAccessIsAlwaysAvailable,
            ResolveStudentAccessSummary(isServiceMode, isAlwaysAvailablePreferred),
            configuration["HomeschoolManager:ProductionServiceName"] ?? "HomeschoolManager",
            dataRoot,
            settingsPath,
            adminPortalUrl,
            studentPortalUrl,
            SharingLabel(adminPortalUrl),
            SharingLabel(studentPortalUrl));
    }

    private ProductionAvailabilityMode ResolveAvailabilityMode(bool isServiceMode)
    {
        if (isServiceMode)
        {
            return ProductionAvailabilityMode.AlwaysAvailable;
        }

        var configured = configuration["HomeschoolManager:ProductionAvailabilityMode"];
        if (!string.IsNullOrWhiteSpace(configured)
            && Enum.TryParse<ProductionAvailabilityMode>(configured, ignoreCase: true, out var availabilityMode))
        {
            return availabilityMode;
        }

        return ProductionAvailabilityMode.AlwaysAvailable;
    }

    private static string ResolveHostModeLabel(bool isServiceMode, bool isAlwaysAvailablePreferred)
    {
        if (isServiceMode)
        {
            return "Always Available";
        }

        return isAlwaysAvailablePreferred
            ? "Open Only until Always Available is turned on"
            : "Open Only";
    }

    private static string ResolveAvailabilityLabel(ProductionAvailabilityMode availabilityMode)
    {
        return availabilityMode == ProductionAvailabilityMode.AlwaysAvailable
            ? "Always Available (recommended)"
            : "Open Only";
    }

    private static string ResolveStudentAccessSummary(bool isServiceMode, bool isAlwaysAvailablePreferred)
    {
        if (isServiceMode)
        {
            return "Student access can stay available while this PC is on and awake, even when no parent is signed in.";
        }

        if (isAlwaysAvailablePreferred)
        {
            return "Always Available is the recommended setup, but Windows has not been set up to run it in the background yet. Until then, students can use the student portal only while a parent has Homeschool Manager open.";
        }

        return "Students can use the student portal only while a parent has Homeschool Manager open.";
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
