namespace HomeschoolManager.Infrastructure.Production;

public sealed class PortalLaunchSettings
{
    public bool Enabled { get; set; } = true;

    public PortalSharingMode SharingMode { get; set; } = PortalSharingMode.Localhost;

    public int Port { get; set; }

    public string WifiHost { get; set; } = "";
}
