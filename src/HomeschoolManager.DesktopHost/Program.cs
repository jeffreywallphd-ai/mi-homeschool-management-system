using HomeschoolManager.Infrastructure.Production;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Velopack;

VelopackApp.Build().Run();

var command = HostCommand.Parse(args);
var rootOverride = Environment.GetEnvironmentVariable("HOMESCHOOL_MANAGER_PRODUCTION_ROOT");
var paths = new ProductionPathProvider(rootOverride, command.HostMode);
var store = new ProductionRuntimeSettingsStore(paths);
var settings = store.LoadOrCreate();
command.Apply(settings);
store.Save(settings);

var adminEndpoint = PortalEndpointBuilder.Build(ProductionPortalKind.Admin, settings.AdminPortal);
var studentEndpoint = PortalEndpointBuilder.Build(ProductionPortalKind.Student, settings.StudentPortal);
var servicePlan = command.HostMode == ProductionHostMode.Service
    ? ServiceDataProtectionPlanBuilder.Build(paths.Root, settings.ParentWindowsAccount, command.ServiceName)
    : null;
var summary = new HostSummary(
    command.HostMode,
    command.ServiceName,
    paths.Root,
    paths.RuntimeSettingsPath,
    adminEndpoint,
    studentEndpoint,
    settings.UpdateChannel,
    settings.UpdateFeedUrl,
    settings.BackupBeforeUpdate,
    servicePlan);

if (command.PrintConfig || command.DryRun)
{
    Console.WriteLine(JsonSerializer.Serialize(summary, HostJson.Options));
}

if (command.DryRun)
{
    return;
}

if (command.HostMode == ProductionHostMode.Desktop
    && !command.SkipUpdateCheck
    && !string.IsNullOrWhiteSpace(settings.UpdateFeedUrl))
{
    await TryApplyUpdatesAsync(settings.UpdateFeedUrl);
}

var context = new ProductionRunContext(
    command.HostMode,
    command.ServiceName,
    settings,
    paths,
    adminEndpoint,
    studentEndpoint);

if (command.HostMode == ProductionHostMode.Service)
{
    await RunAsWindowsServiceAsync(args, context);
}
else
{
    RunAsDesktopHost(context, command);
}

static void RunAsDesktopHost(ProductionRunContext context, HostCommand command)
{
    var runtime = new ProductionPortalRuntime(context, createNoWindow: false);
    try
    {
        runtime.Start();
        if (context.Settings.OpenAdminPortalOnLaunch && !command.NoBrowser && context.AdminEndpoint.Enabled)
        {
            OpenBrowser(context.AdminEndpoint.DisplayUrl);
        }

        Console.WriteLine("Homeschool Manager is running. Close this window or press Ctrl+C to stop the portals.");
        using var stop = new ManualResetEventSlim(false);
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stop.Set();
        };
        stop.Wait();
    }
    finally
    {
        runtime.Stop();
    }
}

static async Task RunAsWindowsServiceAsync(string[] args, ProductionRunContext context)
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = context.ServiceName;
    });
    builder.Services.AddSingleton(context);
    builder.Services.AddSingleton(new ProductionPortalRuntime(context, createNoWindow: true));
    builder.Services.AddHostedService<ProductionPortalHostedService>();

    await builder.Build().RunAsync();
}

static void OpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch
    {
        Console.WriteLine($"Open this address in a browser: {url}");
    }
}

static async Task TryApplyUpdatesAsync(string updateFeedUrl)
{
    try
    {
        var manager = new UpdateManager(updateFeedUrl);
        var update = await manager.CheckForUpdatesAsync();
        if (update is null)
        {
            return;
        }

        await manager.DownloadUpdatesAsync(update);
        manager.ApplyUpdatesAndRestart(update);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Update check skipped: {ex.Message}");
    }
}

internal sealed record HostSummary(
    ProductionHostMode HostMode,
    string ServiceName,
    string DataRoot,
    string SettingsPath,
    PortalEndpoint AdminPortal,
    PortalEndpoint StudentPortal,
    string UpdateChannel,
    string UpdateFeedUrl,
    bool BackupBeforeUpdate,
    ServiceDataProtectionPlan? ServiceDataProtectionPlan);

internal sealed record ProductionRunContext(
    ProductionHostMode HostMode,
    string ServiceName,
    ProductionRuntimeSettings Settings,
    ProductionPathProvider Paths,
    PortalEndpoint AdminEndpoint,
    PortalEndpoint StudentEndpoint);

internal sealed class ProductionPortalHostedService : BackgroundService
{
    private readonly ProductionPortalRuntime runtime;

    public ProductionPortalHostedService(ProductionPortalRuntime runtime)
    {
        this.runtime = runtime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        runtime.Start();
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            runtime.Stop();
        }
    }
}

internal sealed class ProductionPortalRuntime
{
    private readonly ProductionRunContext context;
    private readonly bool createNoWindow;
    private readonly List<Process> running = [];
    private bool started;

    public ProductionPortalRuntime(ProductionRunContext context, bool createNoWindow)
    {
        this.context = context;
        this.createNoWindow = createNoWindow;
    }

    public void Start()
    {
        if (started)
        {
            return;
        }

        started = true;
        var locator = new PortalProcessLocator(AppContext.BaseDirectory, context);
        if (context.AdminEndpoint.Enabled)
        {
            var adminSpec = locator.ResolveAdmin(context.AdminEndpoint.BindUrl, context.StudentEndpoint.DisplayUrl);
            running.Add(StartPortal(adminSpec));
            Console.WriteLine($"Parent/Admin portal: {context.AdminEndpoint.DisplayUrl}");
        }

        if (context.StudentEndpoint.Enabled)
        {
            var studentSpec = locator.ResolveStudent(context.StudentEndpoint.BindUrl);
            running.Add(StartPortal(studentSpec));
            Console.WriteLine($"Student portal: {context.StudentEndpoint.DisplayUrl}");
        }

        foreach (var warning in context.AdminEndpoint.Warnings.Concat(context.StudentEndpoint.Warnings))
        {
            Console.WriteLine($"Warning: {warning}");
        }
    }

    public void Stop()
    {
        foreach (var process in running)
        {
            StopPortal(process);
        }

        running.Clear();
        started = false;
    }

    private Process StartPortal(PortalLaunchSpec spec)
    {
        var start = new ProcessStartInfo(spec.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = createNoWindow,
            WorkingDirectory = spec.WorkingDirectory
        };
        foreach (var argument in spec.Arguments)
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var item in spec.Environment)
        {
            start.Environment[item.Key] = item.Value;
        }

        return Process.Start(start) ?? throw new InvalidOperationException($"Could not start {spec.Name}.");
    }

    private static void StopPortal(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.CloseMainWindow();
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Shutdown is best-effort; startup logs contain actionable failure details.
        }
    }
}

internal sealed class HostCommand
{
    public ProductionHostMode HostMode { get; private set; } = ProductionHostMode.Desktop;
    public string ServiceName { get; private set; } = ServiceDataProtectionPlanBuilder.DefaultServiceName;
    public bool DryRun { get; private set; }
    public bool PrintConfig { get; private set; }
    public bool NoBrowser { get; private set; }
    public bool SkipUpdateCheck { get; private set; }
    private readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

    public static HostCommand Parse(string[] args)
    {
        var command = new HostCommand();
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg.Equals("--service", StringComparison.OrdinalIgnoreCase))
            {
                command.HostMode = ProductionHostMode.Service;
                command.NoBrowser = true;
                command.SkipUpdateCheck = true;
            }
            else if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                command.DryRun = true;
            }
            else if (arg.Equals("--print-config", StringComparison.OrdinalIgnoreCase))
            {
                command.PrintConfig = true;
            }
            else if (arg.Equals("--no-browser", StringComparison.OrdinalIgnoreCase))
            {
                command.NoBrowser = true;
            }
            else if (arg.Equals("--skip-update-check", StringComparison.OrdinalIgnoreCase))
            {
                command.SkipUpdateCheck = true;
            }
            else if (arg.StartsWith("--", StringComparison.Ordinal) && index + 1 < args.Length)
            {
                command.values[arg[2..]] = args[++index];
            }
        }

        if (command.values.TryGetValue("host-mode", out var hostMode)
            && Enum.TryParse<ProductionHostMode>(hostMode, ignoreCase: true, out var parsedHostMode))
        {
            command.HostMode = parsedHostMode;
            if (parsedHostMode == ProductionHostMode.Service)
            {
                command.NoBrowser = true;
                command.SkipUpdateCheck = true;
            }
        }

        if (command.values.TryGetValue("service-name", out var serviceName) && !string.IsNullOrWhiteSpace(serviceName))
        {
            command.ServiceName = serviceName.Trim();
        }

        return command;
    }

    public void Apply(ProductionRuntimeSettings settings)
    {
        settings.HostMode = HostMode;
        ApplyPortal("admin", settings.AdminPortal);
        ApplyPortal("student", settings.StudentPortal);
        if (values.TryGetValue("update-feed", out var feed))
        {
            settings.UpdateFeedUrl = feed.Trim();
        }

        if (values.TryGetValue("update-channel", out var channel))
        {
            settings.UpdateChannel = string.IsNullOrWhiteSpace(channel) ? "stable" : channel.Trim();
        }

        if (values.TryGetValue("parent-windows-account", out var parentWindowsAccount))
        {
            settings.ParentWindowsAccount = parentWindowsAccount.Trim();
        }
    }

    private void ApplyPortal(string prefix, PortalLaunchSettings portal)
    {
        if (values.TryGetValue($"{prefix}-enabled", out var enabled) && bool.TryParse(enabled, out var enabledValue))
        {
            portal.Enabled = enabledValue;
        }

        if (values.TryGetValue($"{prefix}-mode", out var mode) && Enum.TryParse<PortalSharingMode>(mode, ignoreCase: true, out var parsedMode))
        {
            portal.SharingMode = parsedMode;
        }

        if (values.TryGetValue($"{prefix}-port", out var port) && int.TryParse(port, out var parsedPort))
        {
            portal.Port = parsedPort;
        }

        if (values.TryGetValue($"{prefix}-wifi-host", out var host))
        {
            portal.WifiHost = host.Trim();
        }
    }
}

internal sealed record PortalLaunchSpec(
    string Name,
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment);

internal sealed class PortalProcessLocator
{
    private readonly string baseDirectory;
    private readonly ProductionRunContext context;

    public PortalProcessLocator(string baseDirectory, ProductionRunContext context)
    {
        this.baseDirectory = baseDirectory;
        this.context = context;
    }

    public PortalLaunchSpec ResolveAdmin(string url, string studentPortalBaseUrl)
    {
        var environment = BaseEnvironment();
        environment["HomeschoolManager__StudentPortalBaseUrl"] = studentPortalBaseUrl;
        environment["HomeschoolManager__AdminPortalUrl"] = context.AdminEndpoint.DisplayUrl;
        environment["HomeschoolManager__StudentPortalUrl"] = context.StudentEndpoint.DisplayUrl;
        return Resolve(
            "Parent/Admin portal",
            "admin",
            "HomeschoolManager.Web",
            url,
            environment);
    }

    public PortalLaunchSpec ResolveStudent(string url)
    {
        var environment = BaseEnvironment();
        environment["HomeschoolManager__StudentPortalUrl"] = context.StudentEndpoint.DisplayUrl;
        return Resolve(
            "Student portal",
            "student",
            "HomeschoolManager.StudentPortal.Web",
            url,
            environment);
    }

    private PortalLaunchSpec Resolve(
        string name,
        string folder,
        string assemblyName,
        string url,
        Dictionary<string, string> environment)
    {
        var publishFolder = Path.Combine(baseDirectory, folder);
        var executable = Path.Combine(publishFolder, $"{assemblyName}.exe");
        if (File.Exists(executable))
        {
            return new PortalLaunchSpec(name, executable, ["--urls", url], publishFolder, environment);
        }

        var dll = Path.Combine(publishFolder, $"{assemblyName}.dll");
        if (File.Exists(dll))
        {
            return new PortalLaunchSpec(name, "dotnet", [dll, "--urls", url], publishFolder, environment);
        }

        var project = FindProject(assemblyName);
        return new PortalLaunchSpec(name, "dotnet", ["run", "--project", project, "--no-launch-profile", "--urls", url], Directory.GetCurrentDirectory(), environment);
    }

    private string FindProject(string assemblyName)
    {
        var current = new DirectoryInfo(baseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", assemblyName, $"{assemblyName}.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine("src", assemblyName, $"{assemblyName}.csproj");
    }

    private Dictionary<string, string> BaseEnvironment()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["HomeschoolManager__DataRoot"] = context.Paths.Root,
            ["HomeschoolManager__UseDevelopmentDataRoot"] = "false",
            ["HomeschoolManager__ProductionHostMode"] = context.HostMode.ToString(),
            ["HomeschoolManager__ProductionSettingsPath"] = context.Paths.RuntimeSettingsPath,
            ["HomeschoolManager__ProductionServiceName"] = context.ServiceName
        };
    }
}

internal static class HostJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static HostJson()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }
}
