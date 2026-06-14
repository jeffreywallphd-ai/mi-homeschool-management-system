using System.Net;

namespace HomeschoolManager.Infrastructure.Production;

public static class PortalEndpointBuilder
{
    public static PortalEndpoint Build(ProductionPortalKind kind, PortalLaunchSettings settings)
    {
        var warnings = new List<string>();
        var port = NormalizePort(settings.Port, kind);
        if (!settings.Enabled)
        {
            return new PortalEndpoint(kind, false, settings.SharingMode, "", "", warnings);
        }

        if (settings.SharingMode == PortalSharingMode.Localhost)
        {
            return new PortalEndpoint(
                kind,
                true,
                PortalSharingMode.Localhost,
                $"http://127.0.0.1:{port}",
                $"http://127.0.0.1:{port}",
                warnings);
        }

        var host = settings.WifiHost.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            warnings.Add($"{kind} portal Wi-Fi sharing is enabled without a selected Wi-Fi address; binding all IPv4 addresses is less specific.");
            host = "0.0.0.0";
        }
        else if (!IPAddress.TryParse(host, out _))
        {
            warnings.Add($"{kind} portal Wi-Fi address is not a valid IP address. The portal will bind all IPv4 addresses until a valid address is selected.");
            host = "0.0.0.0";
        }

        var displayHost = host == "0.0.0.0" ? Environment.MachineName : host;
        return new PortalEndpoint(
            kind,
            true,
            PortalSharingMode.Wifi,
            $"http://{host}:{port}",
            $"http://{displayHost}:{port}",
            warnings);
    }

    private static int NormalizePort(int port, ProductionPortalKind kind)
    {
        if (port is >= 1024 and <= 65535)
        {
            return port;
        }

        return kind == ProductionPortalKind.Admin ? 5171 : 5172;
    }
}
