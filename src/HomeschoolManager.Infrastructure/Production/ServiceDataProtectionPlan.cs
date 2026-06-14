namespace HomeschoolManager.Infrastructure.Production;

public sealed record ServiceDataPermissionEntry(
    string Identity,
    string Rights,
    string Reason);

public sealed record ServiceDataProtectionPlan(
    string DataRoot,
    IReadOnlyList<ServiceDataPermissionEntry> Entries,
    string Summary);

public static class ServiceDataProtectionPlanBuilder
{
    public const string DefaultServiceName = "HomeschoolManager";

    public static ServiceDataProtectionPlan Build(
        string dataRoot,
        string? parentWindowsAccount = null,
        string serviceName = DefaultServiceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var entries = new List<ServiceDataPermissionEntry>
        {
            new("NT AUTHORITY\\SYSTEM", "FullControl", "Allows Windows to run the background service before a parent signs in."),
            new("BUILTIN\\Administrators", "FullControl", "Allows the parent administrator to repair, back up, or uninstall the service."),
            new($"NT SERVICE\\{serviceName}", "Modify", "Allows the Homeschool Manager service to read and save family records.")
        };

        if (!string.IsNullOrWhiteSpace(parentWindowsAccount))
        {
            entries.Add(new(parentWindowsAccount.Trim(), "Modify", "Allows the parent setup account to view backups and support files."));
        }

        return new ServiceDataProtectionPlan(
            dataRoot,
            entries,
            "Service mode keeps one family record store in ProgramData and limits direct folder access to Windows, administrators, the service account, and the parent setup account when provided.");
    }
}
