# Background Service Mode

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: optional Windows background service installation and service-mode data location
- Related ADRs: [ADR-0007](../adr/ADR-0007-background-service-mode-and-machine-data-root.md)
- Related docs: [Local Installation and Data Location](local-installation-and-data-location.md), [Upgrades Migrations and Recovery](upgrades-migrations-and-recovery.md)
- Related tests: `Production service mode uses ProgramData and persists protected-root intent`
- Supersedes: none

## Parent-Friendly Summary

Background service mode is optional. It lets Homeschool Manager start with Windows and keep the student portal available while the computer is on, even when the parent is not signed in.

This mode is intended for families who want a child to use the student portal from another device on the home Wi-Fi network without the parent opening the app each time.

## What Changes

Normal desktop mode stores family records under the parent account:

```text
%LOCALAPPDATA%/HomeschoolManager
```

Background service mode stores one shared service copy under the computer-level data folder:

```text
%PROGRAMDATA%/HomeschoolManager
```

The service data folder is protected for Windows, administrators, the Homeschool Manager service account, and the parent setup account when provided. Student access must still be controlled by the app sign-in and portal routes, not by direct folder access alone.

## Before Installing The Service

1. Install Homeschool Manager normally.
2. Complete Setup in the parent/admin area.
3. Make a backup or use the migration helper, which creates a backup before copying records.
4. Decide whether the parent/admin portal and student portal should be this-computer-only or Wi-Fi shared. They can be configured independently.

Recommended default:

- Parent/Admin portal: this computer only.
- Student portal: this computer only until the parent intentionally enables Wi-Fi sharing.

## Moving Existing Records

If the family already used desktop mode, run the migration helper before installing the service:

```powershell
.\tools\service\move-to-service-data-root.ps1
```

The helper copies the existing parent-account data folder to `%PROGRAMDATA%/HomeschoolManager` and creates a backup first. It does not delete the original folder.

## Installing The Service

Run from an Administrator PowerShell window:

```powershell
.\tools\service\install-homeschool-service.ps1 -ParentWindowsAccount "FAMILYPC\Parent" -Start
```

To allow the student portal on home Wi-Fi, pass the selected local network address:

```powershell
.\tools\service\install-homeschool-service.ps1 -ParentWindowsAccount "FAMILYPC\Parent" -StudentMode Wifi -StudentWifiHost "192.168.1.25" -Start
```

Do not enable Wi-Fi sharing for the parent/admin portal unless the parent intentionally wants admin access from another household device.

## Checking Status

The Setup page shows:

- Whether the app is running from the desktop launcher or the background service.
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

## Removing The Service

Run from an Administrator PowerShell window:

```powershell
.\tools\service\uninstall-homeschool-service.ps1
```

By default, this removes the Windows service and leaves family records in place. Use `-RemoveFamilyData` only when the parent intentionally wants to remove the service-mode records folder.
