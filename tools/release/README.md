# Release Tools

Use `build-windows-release.ps1` from the repository root to create the production publish layout, the family-facing setup and maintenance tool, and, when the Velopack CLI is available, a Windows installer/update feed.

## Create A Production Package

```powershell
.\tools\release\build-windows-release.ps1 -Version 1.0.0 -SkipVelopack
```

The command above creates only the self-contained application layout for inspection.

If a development machine cannot reach NuGet vulnerability-audit services while smoke-testing the layout, add `-DisableNuGetAudit`. Use the normal audited release command on the release machine.

Remove `-SkipVelopack` on a release machine to create the installer and update feed:

```powershell
.\tools\release\build-windows-release.ps1 -Version 1.0.0
```

From Git Bash, use the Bash wrapper:

```bash
bash tools/release/build-windows-release.sh -Version 1.0.0
```

The script uses `vpk` from PATH when present, otherwise it restores the repository-pinned local tool from `.config/dotnet-tools.json`.

The release machine must be able to restore the Windows Desktop publish/runtime support packages used by the self-contained family setup app. If NuGet cannot be reached, normal builds may still pass but the self-contained family setup publish can fail.

The layout places the desktop host at the app root and the two portals in `admin/` and `student/` subfolders. The host expects that layout when installed.

The layout also includes Always Available helpers under `tools\service`:

- `enable-always-available.ps1`
- `disable-always-available.ps1`
- `move-to-service-data-root.ps1`
- `install-homeschool-service.ps1`
- `uninstall-homeschool-service.ps1`

The generated files are written under `artifacts/release` by default:

- `layout/app`: the unpackaged production app layout.
- `packages/HomeschoolManager-Family-Setup/HomeschoolManager-Family-Setup.exe`: the installer/maintenance tool to give to the family.
- `packages/HomeschoolManager-stable-Setup.exe`: the raw Velopack package installer used by the family setup tool and advanced troubleshooting.
- `packages/HomeschoolManager-<version>-stable-full.nupkg`: the full update package.
- `packages/RELEASES-stable`, `packages/releases.stable.json`, and `packages/assets.stable.json`: update-feed metadata.

Do not commit generated release output. The release output stays ignored by git.

## Install A Production Package

1. Build a package with a real version number, for example `1.0.0`.
2. Code-sign the release on the release machine before public distribution.
3. Give the family the `artifacts\release\packages` folder.
4. Run `artifacts\release\packages\HomeschoolManager-Family-Setup\HomeschoolManager-Family-Setup.exe` on the target Windows computer.
5. Choose the default **Always Available** option unless the parent intentionally wants **Open Only**.
6. The setup tool runs the raw Velopack package installer, turns on Always Available when selected, and registers the Homeschool Manager maintenance uninstall prompt when possible.

The first launch creates `%LOCALAPPDATA%\HomeschoolManagerData\config\production-settings.json`. That file controls the two portals independently:

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

Do not give nontechnical families `HomeschoolManager-stable-Setup.exe` as the primary installer. That file is intentionally one-click and cannot ask Homeschool Manager setup or uninstall data-retention questions.

## Always Available Student Access

Always Available is the recommended production setup. It lets Homeschool Manager start with Windows and keep the student portal available while the computer is on and awake, even when the parent is not signed in.

Always Available stores family records at:

```text
%PROGRAMDATA%\HomeschoolManager
```

From the installed app folder, run the friendly helper from an Administrator PowerShell window:

```powershell
.\tools\service\enable-always-available.ps1
```

To share only the student portal over home Wi-Fi:

```powershell
.\tools\service\enable-always-available.ps1 -StudentMode Wifi -StudentWifiHost "192.168.1.25"
```

Recommended default:

- Keep the parent/admin portal on `Localhost`.
- Keep the student portal on `Localhost` until the parent intentionally enables Wi-Fi sharing.

To turn off Always Available without deleting family records:

```powershell
.\tools\service\disable-always-available.ps1
```

The lower-level helpers remain available for advanced troubleshooting:

```powershell
.\tools\service\move-to-service-data-root.ps1
.\tools\service\install-homeschool-service.ps1 -ParentWindowsAccount "FAMILYPC\Parent" -Start
.\tools\service\uninstall-homeschool-service.ps1
```

## Uninstall And Family Records

When installed through `HomeschoolManager-Family-Setup.exe`, Windows Add/Remove should open the Homeschool Manager maintenance prompt. The default uninstall choice keeps family records on the computer.

The parent can choose to remove family records from the computer. That choice requires typing `Remove Family Records` and can create a safety archive under the parent's Documents folder before deleting the active Homeschool Manager data folders.

If the parent installed by running raw `HomeschoolManager-stable-Setup.exe` directly, Windows Add/Remove uses Velopack's default uninstall behavior and does not show Homeschool Manager's data-retention prompt. Family records still live outside the app binaries and remain on disk unless the parent removes them through the maintenance tool or manually deletes the data folders.

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
6. On the installed computer, make sure `%LOCALAPPDATA%\HomeschoolManagerData\config\production-settings.json` has `updateFeedUrl` pointed at that feed.
7. Restart Homeschool Manager. The desktop host checks the feed on launch, downloads the update when one is available, applies it, and restarts.

For troubleshooting, launch the desktop host with `--skip-update-check` to start the app without checking the feed. To inspect portal binding without starting the web portals, run `HomeschoolManager.exe --dry-run`.

Updates replace installed application binaries only. Family records, uploaded evidence, exports, backups, logs, and production settings remain under `%LOCALAPPDATA%\HomeschoolManagerData`.

If an older prerelease copy stored records under `%LOCALAPPDATA%\HomeschoolManager`, the current desktop host copies known family-data folders into `%LOCALAPPDATA%\HomeschoolManagerData` on first launch when the new folder is empty. It leaves the old folder in place and does not copy app binary folders such as `current`.

For background service installations:

1. Back up family records first.
2. Stop the `HomeschoolManager` service.
3. Install the newer Homeschool Manager package.
4. Start the `HomeschoolManager` service.
5. Open Setup and confirm the mode, family records folder, and portal sharing.

Service-mode records remain under `%PROGRAMDATA%\HomeschoolManager`.
