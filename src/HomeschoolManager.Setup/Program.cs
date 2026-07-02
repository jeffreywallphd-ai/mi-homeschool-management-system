using Microsoft.Win32;
using System.Diagnostics;
using System.IO.Compression;

namespace HomeschoolManager.Setup;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MaintenanceForm(SetupArguments.Parse(args)));
    }
}

internal sealed class SetupArguments
{
    public bool OpenUninstall { get; private init; }
    public bool QuietUninstall { get; private init; }
    public string InstallerPath { get; private init; } = "";

    public static SetupArguments Parse(string[] args)
    {
        var builder = new SetupArgumentsBuilder();
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
            {
                builder.OpenUninstall = true;
            }
            else if (arg.Equals("--quiet", StringComparison.OrdinalIgnoreCase))
            {
                builder.QuietUninstall = true;
            }
            else if (arg.Equals("--installer", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                builder.InstallerPath = args[++index];
            }
        }

        return builder.Build();
    }

    private sealed class SetupArgumentsBuilder
    {
        public bool OpenUninstall { get; set; }
        public bool QuietUninstall { get; set; }
        public string InstallerPath { get; set; } = "";

        public SetupArguments Build()
        {
            return new SetupArguments
            {
                OpenUninstall = OpenUninstall,
                QuietUninstall = QuietUninstall,
                InstallerPath = InstallerPath
            };
        }
    }
}

internal sealed class MaintenanceForm : Form
{
    private const string AppId = "HomeschoolManager";
    private const string AppTitle = "Homeschool Manager";
    private const string ServiceName = "HomeschoolManager";
    private const string RemoveConfirmationText = "Remove Family Records";

    private readonly SetupArguments arguments;
    private readonly TabControl tabs = new();
    private readonly RadioButton alwaysAvailableOption = new();
    private readonly RadioButton openOnlyOption = new();
    private readonly RadioButton keepRecordsOption = new();
    private readonly RadioButton removeRecordsOption = new();
    private readonly CheckBox createBackupOption = new();
    private readonly TextBox removeConfirmation = new();
    private readonly TextBox logBox = new();
    private readonly Button installButton = new();
    private readonly Button uninstallButton = new();
    private readonly Button closeButton = new();

    public MaintenanceForm(SetupArguments arguments)
    {
        this.arguments = arguments;
        Text = "Homeschool Manager Setup & Maintenance";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 760;
        Height = 650;
        MinimumSize = new Size(720, 600);
        BuildLayout();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (arguments.OpenUninstall)
        {
            tabs.SelectedIndex = 1;
        }
        else if (tabs.SelectedIndex < 0)
        {
            tabs.SelectedIndex = 0;
        }

        UpdatePrimaryActionVisibility();

        if (arguments.QuietUninstall)
        {
            await RunUninstallAsync(keepRecords: true, createSafetyArchive: false);
        }
    }

    private void BuildLayout()
    {
        var header = new Label
        {
            Text = "Homeschool Manager Setup & Maintenance",
            Dock = DockStyle.Top,
            Height = 48,
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 18, 0)
        };

        tabs.Dock = DockStyle.Fill;
        tabs.TabPages.Add(BuildInstallTab());
        tabs.TabPages.Add(BuildUninstallTab());

        logBox.Dock = DockStyle.Bottom;
        logBox.Height = 145;
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Vertical;
        logBox.BackColor = Color.White;
        logBox.Font = new Font(FontFamily.GenericMonospace, 9);

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            Padding = new Padding(18, 8, 18, 8)
        };

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 420,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        closeButton.Text = "Close";
        closeButton.Width = 110;
        closeButton.Height = 36;
        closeButton.Click += (_, _) => Close();
        actionPanel.Controls.Add(closeButton);

        uninstallButton.Text = "Uninstall Homeschool Manager";
        uninstallButton.Width = 240;
        uninstallButton.Height = 36;
        uninstallButton.Click += async (_, _) =>
        {
            await RunUninstallAsync(removeRecordsOption.Checked, createBackupOption.Checked);
        };
        actionPanel.Controls.Add(uninstallButton);

        installButton.Text = "Install or repair Homeschool Manager";
        installButton.Width = 260;
        installButton.Height = 36;
        installButton.Click += async (_, _) => await RunInstallOrRepairAsync();
        actionPanel.Controls.Add(installButton);

        bottom.Controls.Add(actionPanel);

        tabs.SelectedIndexChanged += (_, _) => UpdatePrimaryActionVisibility();

        Controls.Add(tabs);
        Controls.Add(logBox);
        Controls.Add(bottom);
        Controls.Add(header);

        UpdatePrimaryActionVisibility();
    }

    private TabPage BuildInstallTab()
    {
        var page = new TabPage("Install or Repair")
        {
            Padding = new Padding(18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "Install Homeschool Manager, repair the app files, or turn on the recommended student access mode.",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 10),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var options = new GroupBox
        {
            Text = "How should Homeschool Manager run?",
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };

        alwaysAvailableOption.Text = "Always Available (recommended): students can use the student portal while this PC is on and awake.";
        alwaysAvailableOption.Checked = true;
        alwaysAvailableOption.Dock = DockStyle.Top;
        alwaysAvailableOption.Height = 52;

        openOnlyOption.Text = "Open Only: Homeschool Manager works only while a parent has it open.";
        openOnlyOption.Dock = DockStyle.Top;
        openOnlyOption.Height = 52;

        options.Controls.Add(openOnlyOption);
        options.Controls.Add(alwaysAvailableOption);
        layout.Controls.Add(options, 0, 1);

        layout.Controls.Add(new Label
        {
            Text = "Always Available may ask for Windows permission so Homeschool Manager can run in the background. Parent/admin and student Wi-Fi sharing still remain separate choices inside the app.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 2);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildUninstallTab()
    {
        var page = new TabPage("Uninstall")
        {
            Padding = new Padding(18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "Remove Homeschool Manager app files. Family records are kept by default so courses, gradebook records, transcripts, diplomas, and portfolio files are not deleted by surprise.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var options = new GroupBox
        {
            Text = "What should happen to family records?",
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };

        keepRecordsOption.Text = "Keep family records on this computer (recommended).";
        keepRecordsOption.Checked = true;
        keepRecordsOption.Dock = DockStyle.Top;
        keepRecordsOption.Height = 38;

        removeRecordsOption.Text = "Remove family records from this computer.";
        removeRecordsOption.Dock = DockStyle.Top;
        removeRecordsOption.Height = 38;
        removeRecordsOption.CheckedChanged += (_, _) => UpdateRemoveConfirmationState();

        options.Controls.Add(removeRecordsOption);
        options.Controls.Add(keepRecordsOption);
        layout.Controls.Add(options, 0, 1);

        createBackupOption.Text = "Create a safety archive before removing family records";
        createBackupOption.Checked = true;
        createBackupOption.Enabled = false;
        createBackupOption.Dock = DockStyle.Fill;
        layout.Controls.Add(createBackupOption, 0, 2);

        layout.Controls.Add(new Label
        {
            Text = $"To remove records, type \"{RemoveConfirmationText}\" below.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 3);

        removeConfirmation.Dock = DockStyle.Fill;
        removeConfirmation.Enabled = false;
        removeConfirmation.TextChanged += (_, _) => UpdateRemoveConfirmationState();
        layout.Controls.Add(removeConfirmation, 0, 4);

        page.Controls.Add(layout);
        return page;
    }

    private void UpdatePrimaryActionVisibility()
    {
        var showInstallAction = tabs.SelectedIndex <= 0;
        installButton.Visible = showInstallAction;
        uninstallButton.Visible = !showInstallAction;
        AcceptButton = installButton.Visible ? installButton : uninstallButton;
        installButton.Parent?.PerformLayout();
    }

    private async Task RunInstallOrRepairAsync()
    {
        await RunOperationAsync(async () =>
        {
            AppendLog("Starting install or repair.");
            var installer = FindVelopackSetupInstaller();
            if (installer is not null)
            {
                AppendLog($"Running app package installer: {installer}");
                await RunProcessAsync(installer, "--silent", requireSuccess: true, elevated: false);
            }
            else if (!Directory.Exists(GetInstalledAppRoot()))
            {
                throw new InvalidOperationException("Could not find HomeschoolManager-stable-Setup.exe. Run this setup tool from the release packages folder.");
            }
            else
            {
                AppendLog("No package installer was found. Using the existing installed app files.");
            }

            var appExe = GetInstalledAppExe();
            if (!File.Exists(appExe))
            {
                throw new InvalidOperationException($"Could not find the installed app at {appExe}.");
            }

            if (alwaysAvailableOption.Checked)
            {
                AppendLog("Turning on Always Available student access.");
                var helper = GetInstalledServiceHelper("enable-always-available.ps1");
                await RunPowerShellFileAsync(helper, "", elevated: true);
            }
            else
            {
                AppendLog("Configuring Open Only mode.");
                if (WindowsServiceExists())
                {
                    var disableHelper = GetInstalledServiceHelper("disable-always-available.ps1");
                    await RunPowerShellFileAsync(disableHelper, "", elevated: true);
                }

                await RunProcessAsync(
                    appExe,
                    "--dry-run --host-mode Desktop --availability-mode OpenOnly --no-browser --skip-update-check",
                    requireSuccess: true,
                    elevated: false);
            }

            var maintenanceExe = CopyMaintenanceTool();
            RegisterMaintenanceUninstall(maintenanceExe);
            AppendLog("Maintenance uninstall prompt is registered for this Windows account.");
            AppendLog("Install or repair finished.");
            MessageBox.Show(
                this,
                "Homeschool Manager is ready.",
                "Setup complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });
    }

    private async Task RunUninstallAsync(bool keepRecords, bool createSafetyArchive)
    {
        await RunOperationAsync(async () =>
        {
            if (!keepRecords && !RemoveConfirmationMatches())
            {
                MessageBox.Show(
                    this,
                    $"Type \"{RemoveConfirmationText}\" before removing family records.",
                    "Confirmation required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            AppendLog("Starting uninstall.");
            var disableHelper = GetInstalledServiceHelper("disable-always-available.ps1");
            if (File.Exists(disableHelper))
            {
                AppendLog(keepRecords
                    ? "Turning off Always Available while keeping family records."
                    : "Turning off Always Available before preparing records for removal.");
                await RunPowerShellFileAsync(disableHelper, "", elevated: true);
            }

            if (!keepRecords && createSafetyArchive)
            {
                var archive = CreateSafetyArchive();
                if (!string.IsNullOrWhiteSpace(archive))
                {
                    AppendLog($"Safety archive created: {archive}");
                }
            }

            if (!keepRecords && File.Exists(disableHelper))
            {
                AppendLog("Removing the protected Always Available records folder.");
                await RunPowerShellFileAsync(disableHelper, "-RemoveFamilyData", elevated: true);
            }

            if (!keepRecords)
            {
                DeleteDirectoryIfExists(GetDesktopDataRoot());
            }

            var originalUninstaller = GetOriginalUninstallCommand();
            if (!string.IsNullOrWhiteSpace(originalUninstaller))
            {
                AppendLog("Running app file uninstaller.");
                await RunProcessAsync("cmd.exe", "/c " + originalUninstaller, requireSuccess: false, elevated: false);
            }
            else
            {
                AppendLog("Could not find the app file uninstaller. App files may already be removed.");
            }

            AppendLog(keepRecords
                ? "Uninstall finished. Family records were kept."
                : "Uninstall finished. Family records selected for removal were removed.");
            MessageBox.Show(
                this,
                keepRecords
                    ? "Homeschool Manager was removed. Family records were kept on this computer."
                    : "Homeschool Manager was removed. Family records selected for removal were removed.",
                "Uninstall complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            Close();
        });
    }

    private async Task RunOperationAsync(Func<Task> operation)
    {
        SetBusy(true);
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            AppendLog("Error: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Homeschool Manager setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        installButton.Enabled = !busy;
        uninstallButton.Enabled = !busy && (!removeRecordsOption.Checked || RemoveConfirmationMatches());
        closeButton.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void UpdateRemoveConfirmationState()
    {
        createBackupOption.Enabled = removeRecordsOption.Checked;
        removeConfirmation.Enabled = removeRecordsOption.Checked;
        uninstallButton.Enabled = !removeRecordsOption.Checked || RemoveConfirmationMatches();
    }

    private bool RemoveConfirmationMatches()
    {
        return string.Equals(removeConfirmation.Text.Trim(), RemoveConfirmationText, StringComparison.Ordinal);
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendLog(message));
            return;
        }

        logBox.AppendText($"[{DateTime.Now:t}] {message}{Environment.NewLine}");
    }

    private string? FindVelopackSetupInstaller()
    {
        if (!string.IsNullOrWhiteSpace(arguments.InstallerPath) && File.Exists(arguments.InstallerPath))
        {
            return Path.GetFullPath(arguments.InstallerPath);
        }

        var currentExe = Path.GetFullPath(Application.ExecutablePath);
        var searchRoots = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.Combine(AppContext.BaseDirectory, "packages"),
            Path.Combine(AppContext.BaseDirectory, "..", "packages"),
            Path.Combine(AppContext.BaseDirectory, "..")
        };

        foreach (var root in searchRoots.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var candidate = Directory
                .EnumerateFiles(root, "HomeschoolManager*-Setup.exe", SearchOption.TopDirectoryOnly)
                .Where(path => !string.Equals(Path.GetFullPath(path), currentExe, StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path.Contains("stable", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .FirstOrDefault();

            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static string GetInstalledAppRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppId);
    }

    private static string GetInstalledAppExe()
    {
        return Path.Combine(GetInstalledAppRoot(), "current", "HomeschoolManager.exe");
    }

    private static string GetInstalledServiceHelper(string scriptName)
    {
        return Path.Combine(GetInstalledAppRoot(), "current", "tools", "service", scriptName);
    }

    private static string GetDesktopDataRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeschoolManagerData");
    }

    private static string GetServiceDataRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            AppId);
    }

    private static async Task RunPowerShellFileAsync(string scriptPath, string scriptArguments, bool elevated)
    {
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Could not find setup helper.", scriptPath);
        }

        var powerShell = FindPowerShell();
        var arguments = $"-NoProfile -ExecutionPolicy Bypass -File {Quote(scriptPath)}";
        if (!string.IsNullOrWhiteSpace(scriptArguments))
        {
            arguments += " " + scriptArguments;
        }

        await RunProcessAsync(powerShell, arguments, requireSuccess: true, elevated);
    }

    private static string FindPowerShell()
    {
        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var windowsPowerShell = Path.Combine(systemRoot, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        return File.Exists(windowsPowerShell) ? windowsPowerShell : "powershell.exe";
    }

    private static async Task RunProcessAsync(string fileName, string arguments, bool requireSuccess, bool elevated)
    {
        var start = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = elevated,
            Verb = elevated ? "runas" : "",
            CreateNoWindow = !elevated,
            WindowStyle = elevated ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
        };

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        await process.WaitForExitAsync();
        if (requireSuccess && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} exited with code {process.ExitCode}.");
        }
    }

    private static string CopyMaintenanceTool()
    {
        var maintenanceRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeschoolManagerMaintenance");
        Directory.CreateDirectory(maintenanceRoot);
        var sourceRoot = AppContext.BaseDirectory;
        var target = Path.Combine(maintenanceRoot, Path.GetFileName(Application.ExecutablePath));
        if (!Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar).Equals(
                Path.GetFullPath(maintenanceRoot).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            CopyDirectory(sourceRoot, maintenanceRoot);
        }

        return target;
    }

    private static void CopyDirectory(string sourceRoot, string targetRoot)
    {
        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var targetDirectory = Path.Combine(targetRoot, Path.GetRelativePath(sourceRoot, directory));
            Directory.CreateDirectory(targetDirectory);
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var targetFile = Path.Combine(targetRoot, Path.GetRelativePath(sourceRoot, file));
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? targetRoot);
            File.Copy(file, targetFile, overwrite: true);
        }
    }

    private static void RegisterMaintenanceUninstall(string maintenanceExe)
    {
        using var uninstallRoot = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
        var entry = FindHomeschoolManagerUninstallEntry(uninstallRoot) ?? uninstallRoot.CreateSubKey(AppId);
        if (entry is null)
        {
            return;
        }

        using (entry)
        {
            var currentUninstall = entry.GetValue("UninstallString")?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(currentUninstall)
                && !currentUninstall.Contains(Path.GetFileName(maintenanceExe), StringComparison.OrdinalIgnoreCase)
                && !currentUninstall.Contains("HomeschoolManagerMaintenance", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(entry.GetValue("HomeschoolManagerOriginalUninstallString")?.ToString()))
            {
                entry.SetValue("HomeschoolManagerOriginalUninstallString", currentUninstall, RegistryValueKind.String);
            }

            entry.SetValue("DisplayName", AppTitle, RegistryValueKind.String);
            entry.SetValue("Publisher", "Homeschool Manager", RegistryValueKind.String);
            entry.SetValue("DisplayIcon", maintenanceExe, RegistryValueKind.String);
            entry.SetValue("UninstallString", $"{Quote(maintenanceExe)} --uninstall", RegistryValueKind.String);
            entry.SetValue("QuietUninstallString", $"{Quote(maintenanceExe)} --uninstall --quiet", RegistryValueKind.String);
            entry.SetValue("HomeschoolManagerDataRetention", "Prompt before removing family records", RegistryValueKind.String);
        }
    }

    private static RegistryKey? FindHomeschoolManagerUninstallEntry(RegistryKey uninstallRoot)
    {
        foreach (var name in uninstallRoot.GetSubKeyNames())
        {
            using var candidate = uninstallRoot.OpenSubKey(name, writable: true);
            var displayName = candidate?.GetValue("DisplayName")?.ToString() ?? "";
            if (string.Equals(displayName, AppTitle, StringComparison.OrdinalIgnoreCase)
                || name.Contains(AppId, StringComparison.OrdinalIgnoreCase))
            {
                return uninstallRoot.OpenSubKey(name, writable: true);
            }
        }

        return null;
    }

    private static string GetOriginalUninstallCommand()
    {
        using var uninstallRoot = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
        if (uninstallRoot is not null)
        {
            using var entry = FindHomeschoolManagerUninstallEntry(uninstallRoot);
            var original = entry?.GetValue("HomeschoolManagerOriginalUninstallString")?.ToString();
            if (!string.IsNullOrWhiteSpace(original))
            {
                return original;
            }
        }

        var updateExe = Path.Combine(GetInstalledAppRoot(), "Update.exe");
        return File.Exists(updateExe) ? $"{Quote(updateExe)} --uninstall" : "";
    }

    private static bool WindowsServiceExists()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("sc.exe", $"query {ServiceName}")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string CreateSafetyArchive()
    {
        var roots = new[]
        {
            (Root: GetDesktopDataRoot(), Prefix: "OpenOnlyRecords"),
            (Root: GetServiceDataRoot(), Prefix: "AlwaysAvailableRecords")
        }.Where(item => Directory.Exists(item.Root)).ToList();

        if (roots.Count == 0)
        {
            return "";
        }

        var backupRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Homeschool Manager Backups");
        Directory.CreateDirectory(backupRoot);
        var archivePath = Path.Combine(backupRoot, $"pre-uninstall-safety-{DateTime.Now:yyyyMMdd-HHmmss}.zip");

        using var stream = File.Create(archivePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var root in roots)
        {
            AddDirectoryToArchive(archive, root.Root, root.Prefix);
        }

        return archivePath;
    }

    private static void AddDirectoryToArchive(ZipArchive archive, string root, string prefix)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            archive.CreateEntryFromFile(file, $"{prefix}/{relative}", CompressionLevel.Optimal);
        }
    }

    private void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        AppendLog($"Removing family records folder: {path}");
        Directory.Delete(path, recursive: true);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
