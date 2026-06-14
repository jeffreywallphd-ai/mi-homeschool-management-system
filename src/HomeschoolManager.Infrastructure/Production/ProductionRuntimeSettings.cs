namespace HomeschoolManager.Infrastructure.Production;

public sealed class ProductionRuntimeSettings
{
    public ProductionHostMode HostMode { get; set; } = ProductionHostMode.Desktop;

    public PortalLaunchSettings AdminPortal { get; set; } = new()
    {
        Enabled = true,
        SharingMode = PortalSharingMode.Localhost,
        Port = 5171
    };

    public PortalLaunchSettings StudentPortal { get; set; } = new()
    {
        Enabled = true,
        SharingMode = PortalSharingMode.Localhost,
        Port = 5172
    };

    public string UpdateChannel { get; set; } = "stable";

    public string UpdateFeedUrl { get; set; } = "";

    public bool OpenAdminPortalOnLaunch { get; set; } = true;

    public bool BackupBeforeUpdate { get; set; } = true;

    public string ParentWindowsAccount { get; set; } = "";
}
