# Always Available Mode

- Status: accepted
- Last reviewed: 2026-07-02
- Canonical for: recommended Always Available production mode, Windows background installation, and service-mode data location
- Related ADRs: [ADR-0007](../adr/ADR-0007-background-service-mode-and-machine-data-root.md)
- Related docs: [Local Installation and Data Location](local-installation-and-data-location.md), [Upgrades Migrations and Recovery](upgrades-migrations-and-recovery.md)
- Related tests: `Production service mode uses ProgramData and persists protected-root intent`
- Supersedes: none

## Parent-Friendly Summary

Always Available mode is the recommended production setup for families who want the student portal to work whenever the family PC is on and awake. Windows runs Homeschool Manager in the background, so the student can use the student portal even when the parent is not signed in.

Open Only mode remains available. In Open Only mode, the student portal works only while the parent has started Homeschool Manager.

## What Changes

Normal desktop mode stores family records under the parent account:

```text
%LOCALAPPDATA%/HomeschoolManagerData
```

Always Available mode stores one shared service copy under the computer-level data folder:

```text
%PROGRAMDATA%/HomeschoolManager
```

The service data folder is protected for Windows, administrators, the Homeschool Manager service account, and the parent setup account when provided. Student access must still be controlled by the app sign-in and portal routes, not by direct folder access alone.

## Before Turning On Always Available Access

1. Install Homeschool Manager normally.
2. Complete Setup in the parent/admin area.
3. Make a backup or use the Always Available helper, which creates a backup before copying records.
4. Decide whether the parent/admin portal and student portal should be this-computer-only or Wi-Fi shared. They can be configured independently.

Recommended default:

- Parent/Admin portal: this computer only.
- Student portal: this computer only until the parent intentionally enables Wi-Fi sharing.

## Turn On Always Available Access

Run from an Administrator PowerShell window:

```powershell
.\tools\service\enable-always-available.ps1
```

The helper copies family records into `%PROGRAMDATA%/HomeschoolManager`, protects that folder, installs the Windows background runner, and starts Homeschool Manager.

To allow the student portal on home Wi-Fi, pass the selected local network address:

```powershell
.\tools\service\enable-always-available.ps1 -StudentMode Wifi -StudentWifiHost "192.168.1.25"
```

Do not enable Wi-Fi sharing for the parent/admin portal unless the parent intentionally wants admin access from another household device.

## Advanced: Moving Existing Records

If the family already used desktop mode, run the migration helper before installing the service:

```powershell
.\tools\service\move-to-service-data-root.ps1
```

The helper copies the existing parent-account data folder to `%PROGRAMDATA%/HomeschoolManager` and creates a backup first. It does not delete the original folder.

If the newer desktop data folder is not present, the helper falls back to the legacy prerelease desktop folder at `%LOCALAPPDATA%/HomeschoolManager`.

## Advanced: Installing The Windows Service Directly

Run from an Administrator PowerShell window:

```powershell
.\tools\service\install-homeschool-service.ps1 -ParentWindowsAccount "FAMILYPC\Parent" -Start
```

To allow the student portal on home Wi-Fi, pass the selected local network address:

```powershell
.\tools\service\install-homeschool-service.ps1 -ParentWindowsAccount "FAMILYPC\Parent" -StudentMode Wifi -StudentWifiHost "192.168.1.25" -Start
```

## Checking Status

The Setup page shows:

- Whether student access is set to Always Available or Open Only.
- Whether the app is currently running through the Windows background runner or only through the parent-opened launcher.
- The active family records folder.
- Whether the parent/admin portal and student portal are this-computer-only or Wi-Fi shared.

Windows Services can also show whether the service named `HomeschoolManager` is running.

## Updates

Desktop mode checks for app updates when the parent launches Homeschool Manager.

Service mode should be updated intentionally:

1. Make sure family records are backed up.
2. Stop the `HomeschoolManager` service.
3. Install the newer Homeschool Manager package.
4. Start the `HomeschoolManager` service again.
5. Open Setup and confirm the mode, records folder, and portal sharing are still correct.

Updates must replace application files only. They must not delete `%PROGRAMDATA%/HomeschoolManager`.

## Turn Off Always Available Access

Run from an Administrator PowerShell window:

```powershell
.\tools\service\disable-always-available.ps1
```

By default, this removes the Windows background runner and leaves family records in place. Use `-RemoveFamilyData` only when the parent intentionally wants to remove the service-mode records folder.

The lower-level uninstall helper remains available for advanced use:

```powershell
.\tools\service\uninstall-homeschool-service.ps1
```
