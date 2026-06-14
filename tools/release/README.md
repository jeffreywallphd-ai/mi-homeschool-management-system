# Release Tools

Use `build-windows-release.ps1` from the repository root to create the production publish layout and, when the Velopack CLI is available, a Windows installer/update feed.

## Create A Production Package

```powershell
.\tools\release\build-windows-release.ps1 -Version 1.0.0 -SkipVelopack
```

The command above creates only the self-contained application layout for inspection.

Remove `-SkipVelopack` on a release machine to create the installer and update feed:

```powershell
.\tools\release\build-windows-release.ps1 -Version 1.0.0
```

From Git Bash, use the Bash wrapper:

```bash
bash tools/release/build-windows-release.sh -Version 1.0.0
```

The script uses `vpk` from PATH when present, otherwise it restores the repository-pinned local tool from `.config/dotnet-tools.json`.

The layout places the desktop host at the app root and the two portals in `admin/` and `student/` subfolders. The host expects that layout when installed.

The layout also includes optional background-service helpers under `tools\service`:

- `move-to-service-data-root.ps1`
- `install-homeschool-service.ps1`
- `uninstall-homeschool-service.ps1`

The generated files are written under `artifacts/release` by default:

- `layout/app`: the unpackaged production app layout.
- `packages/HomeschoolManager-stable-Setup.exe`: the installer to give to the family.
- `packages/HomeschoolManager-<version>-stable-full.nupkg`: the full update package.
- `packages/RELEASES-stable`, `packages/releases.stable.json`, and `packages/assets.stable.json`: update-feed metadata.

Do not commit generated release output. The release output stays ignored by git.

## Install A Production Package

1. Build a package with a real version number, for example `1.0.0`.
2. Code-sign the release on the release machine before public distribution.
3. Run `artifacts\release\packages\HomeschoolManager-stable-Setup.exe` on the target Windows computer.
4. Start Homeschool Manager from the Start menu or desktop shortcut.
5. In normal desktop mode, the app data stays outside the installed binaries at `%LOCALAPPDATA%\HomeschoolManager`.

The first launch creates `%LOCALAPPDATA%\HomeschoolManager\config\production-settings.json`. That file controls the two portals independently:

```json
{
  "adminPortal": {
    "enabled": true,
    "sharingMode": "Localhost",
    "port": 5171,
    "wifiHost": ""
  },
  "studentPortal": {
    "enabled": true,
    "sharingMode": "Wifi",
    "port": 5172,
    "wifiHost": "192.168.1.25"
  },
  "updateFeedUrl": "https://example.com/homeschool-manager/releases",
  "updateChannel": "stable",
  "backupBeforeUpdate": true
}
```

Use `Localhost` for same-computer access only. Use `Wifi` only when that portal should be reachable from another household device on the local network. Admin and student sharing can be configured differently.

## Optional Background Service Install

Background service mode is optional. It lets Homeschool Manager start with Windows and keep the student portal available while the computer is on, even when the parent is not signed in.

Service mode stores family records at:

```text
%PROGRAMDATA%\HomeschoolManager
```

If the family already used desktop mode, first copy records into the service data folder from an Administrator PowerShell window:

```powershell
.\tools\service\move-to-service-data-root.ps1
```

Then install and start the service:

```powershell
.\tools\service\install-homeschool-service.ps1 -ParentWindowsAccount "FAMILYPC\Parent" -Start
```

To share only the student portal over home Wi-Fi:

```powershell
.\tools\service\install-homeschool-service.ps1 -ParentWindowsAccount "FAMILYPC\Parent" -StudentMode Wifi -StudentWifiHost "192.168.1.25" -Start
```

Recommended default:

- Keep the parent/admin portal on `Localhost`.
- Keep the student portal on `Localhost` until the parent intentionally enables Wi-Fi sharing.

To remove the service without deleting family records:

```powershell
.\tools\service\uninstall-homeschool-service.ps1
```

## Update A Production Installation

1. Finish and test the development changes.
2. Choose the next SemVer version. Every production update must use a higher version than the installed one.
3. Run the release script without `-SkipVelopack`, for example:

```powershell
.\tools\release\build-windows-release.ps1 -Version 1.0.1
```

From Git Bash:

```bash
bash tools/release/build-windows-release.sh -Version 1.0.1
```

4. Publish the contents of `artifacts\release\packages` to the same update feed location used by installed copies.
5. Keep all generated files together in that feed location; the installer/update metadata and `.nupkg` package work as a set.
6. On the installed computer, make sure `%LOCALAPPDATA%\HomeschoolManager\config\production-settings.json` has `updateFeedUrl` pointed at that feed.
7. Restart Homeschool Manager. The desktop host checks the feed on launch, downloads the update when one is available, applies it, and restarts.

For troubleshooting, launch the desktop host with `--skip-update-check` to start the app without checking the feed. To inspect portal binding without starting the web portals, run `HomeschoolManager.exe --dry-run`.

Updates replace installed application binaries only. Family records, uploaded evidence, exports, backups, logs, and production settings remain under `%LOCALAPPDATA%\HomeschoolManager`.

For background service installations:

1. Back up family records first.
2. Stop the `HomeschoolManager` service.
3. Install the newer Homeschool Manager package.
4. Start the `HomeschoolManager` service.
5. Open Setup and confirm the mode, family records folder, and portal sharing.

Service-mode records remain under `%PROGRAMDATA%\HomeschoolManager`.
