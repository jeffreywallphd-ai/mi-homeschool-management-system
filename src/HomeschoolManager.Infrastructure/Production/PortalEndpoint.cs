namespace HomeschoolManager.Infrastructure.Production;

public sealed record PortalEndpoint(
    ProductionPortalKind Kind,
    bool Enabled,
    PortalSharingMode SharingMode,
    string BindUrl,
    string DisplayUrl,
    IReadOnlyList<string> Warnings);
