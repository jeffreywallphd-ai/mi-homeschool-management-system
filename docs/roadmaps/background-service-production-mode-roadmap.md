# Background Service Production Mode Roadmap

- Status: accepted
- Last reviewed: 2026-07-02
- Canonical for: Always Available production mode, machine-level data storage, and household student access
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0006](../adr/ADR-0006-production-installer-update-and-local-sharing.md), [ADR-0007](../adr/ADR-0007-background-service-mode-and-machine-data-root.md)
- Related docs: [Local Data and File Storage](../architecture/local-data-and-file-storage.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Local Installation and Data Location](../operations/local-installation-and-data-location.md), [Background Service Mode](../operations/background-service-mode.md), [Production Installer Update Roadmap](production-installer-update-roadmap.md)
- Related tests: `Production service mode uses ProgramData and persists protected-root intent`, `Service data protection plan excludes broad student-facing Windows access`, `Windows release script preserves data outside the app layout and invokes Velopack packaging`
- Supersedes: none

## Parent-Friendly Goal

Add a recommended "Always Available" production mode. In this mode, Homeschool Manager can keep the student portal available while the family PC is turned on and awake, even when no one is signed in to Windows.

The parent/admin still owns the records. The student can use the student portal from another household device only when the parent has turned on student Wi-Fi sharing.

## Plain-Language Behavior

- If the PC is on and awake, the service can keep Homeschool Manager running.
- The student portal can be available on the household Wi-Fi network.
- The parent/admin area should stay protected and should default to this computer only.
- Family records should live in one protected machine-level folder, not in several copies.
- The parent can still open Homeschool Manager from a desktop icon or Start menu shortcut.
- Updates should stop the service, update the app, and restart the service without deleting family records.

## Data Location

For Always Available mode, use a machine-level family data folder:

```text
%PROGRAMDATA%/HomeschoolManager
```

Expected layout:

```text
data/
files/
backups/
templates/
logs/
config/
```

The folder should be protected with Windows file permissions. Normal student Windows accounts should not be able to browse or edit the database and evidence files directly.

Allowed access:

- The Homeschool Manager Windows background runner.
- The parent/admin Windows account selected during setup.
- Local Administrators.
- Windows SYSTEM.

## Scope

- Add a recommended Windows background install mode with an Open Only fallback.
- Add service-safe data-root handling under `%PROGRAMDATA%/HomeschoolManager`.
- Add installer support for creating the service and protected folder.
- Add parent-facing controls for service status and portal sharing.
- Keep the existing desktop-launched mode available.
- Keep student Wi-Fi sharing explicit.

## Non-Scope

- Cloud sync.
- Hosted accounts.
- Roaming user-profile storage.
- Multiple active databases for the same household.
- Student direct file access to the database.
- Replacing parent/admin authorization checks with folder permissions.

## Design Details

### Install Modes

Offer two parent-facing choices:

- "Always Available": recommended production mode that keeps student access available while this PC is on.
- "Open Only": current desktop-host mode.

The installer should explain the practical difference:

- Open Only is simpler and runs only while the parent starts the app.
- Always Available can keep the student portal available from household devices while the PC is on and awake.

### Service Shape

Create a Windows Service host that:

- Starts automatically with Windows.
- Starts the parent/admin portal and student portal using production settings.
- Uses `%PROGRAMDATA%/HomeschoolManager` as the data root.
- Writes privacy-safe logs under the service data folder.
- Can be stopped and restarted by the installer and parent/admin service controls.

### Portal Defaults

Use safe defaults:

- Parent/admin portal: `Localhost`, available only on the family PC.
- Student portal: `Localhost` until the parent turns on Wi-Fi sharing.

When the parent turns on student Wi-Fi sharing, the app should show:

- The address the student should use.
- A reminder that the PC must stay on and awake.
- A reminder that the student device must be on the same household network.

### Parent/Admin Controls

Add a simple admin screen for:

- Service status: running, stopped, or unavailable.
- Start service.
- Stop service.
- Restart service.
- Open parent/admin portal.
- Show student portal address.
- Configure parent/admin sharing mode.
- Configure student sharing mode.
- Configure ports.
- Show active data location.
- Run backup before update or major changes.

Use parent-friendly wording. Avoid service jargon unless it is needed.

### Folder Protection

The installer should create `%PROGRAMDATA%/HomeschoolManager` and set file permissions before the service starts.

The parent-facing explanation should say:

"Your records are stored in a protected folder on this computer. Student Windows accounts should not be able to open the database directly. Students use the student portal instead."

### Update Flow

For Always Available mode:

- Check for updates from the configured feed.
- Stop the service before replacing application files.
- Run a backup before any update that changes data structure.
- Apply the update.
- Restart the service.
- Leave `%PROGRAMDATA%/HomeschoolManager` in place on update and uninstall unless the parent explicitly chooses to remove family data.

### Migration From Desktop Mode

If the family already uses Open Only mode under `%LOCALAPPDATA%/HomeschoolManagerData`, provide a guided move. If that folder is not present, fall back to the legacy prerelease desktop folder under `%LOCALAPPDATA%/HomeschoolManager`:

- Show the current data location.
- Show the new protected service data location.
- Create a backup first.
- Copy records, files, templates, backups, and config to `%PROGRAMDATA%/HomeschoolManager`.
- Verify the copied data with manifest/checksum checks where available.
- Keep the old data folder as a backup unless the parent explicitly chooses cleanup later.

## Phase 1: Decision and Safety Review

- Create or update an ADR for Always Available mode and machine-level storage.
- Confirm `%PROGRAMDATA%/HomeschoolManager` as the service-mode data root.
- Confirm folder permission strategy.
- Confirm that Always Available is the recommended production default and Open Only remains available.

Exit criteria:

- ADR records the chosen service/data-root design.
- Docs clearly state that Roaming storage is not used.

## Phase 2: Service Data Root and Permissions

- Add production settings for data-root mode: parent profile data or service machine data.
- Add path provider support for `%PROGRAMDATA%/HomeschoolManager`.
- Add installer/setup helper to create protected folders.
- Add tests for path selection and protected-folder intent.

Exit criteria:

- Service mode computes the correct data root.
- Folder layout matches the local data architecture.
- Student direct file access is not part of the design.

## Phase 3: Windows Service Host

- Add a service-host entry point or Always Available mode to the production host.
- Start admin and student portals from the service.
- Log service startup, shutdown, and portal status without logging private student content.
- Keep desktop shortcuts for opening the parent/admin portal.

Exit criteria:

- Service can start portals without an interactive Windows session.
- Dry-run/status command reports effective service settings.

## Phase 4: Installer Integration

- Add installer support for Always Available mode.
- Register the Windows Service.
- Create Start menu and desktop shortcuts.
- Create protected data folders.
- Add firewall rule handling for student portal Wi-Fi sharing only when enabled by the parent.

Exit criteria:

- Fresh install can complete Always Available setup.
- Installed service starts after reboot.
- Parent can open the admin portal from the shortcut.

## Phase 5: Parent/Admin Service Controls

- Add UI for service status and portal sharing.
- Show the active data location.
- Show the student portal address when sharing is enabled.
- Provide plain messages for firewall, sleep, and network limitations.

Exit criteria:

- Parent can understand whether student access is currently available.
- Parent can turn student Wi-Fi sharing on or off intentionally.

## Phase 6: Migration From Existing Production Data

- Add a guided move from `%LOCALAPPDATA%/HomeschoolManagerData` to `%PROGRAMDATA%/HomeschoolManager`, with fallback support for the legacy `%LOCALAPPDATA%/HomeschoolManager` folder.
- Require or create a backup first.
- Verify copied files.
- Keep the original folder as a fallback until the parent chooses cleanup.

Exit criteria:

- Existing Open Only families can move to Always Available mode without losing records.
- The app clearly shows which data location is active.

## Phase 7: Service-Aware Updates and Recovery

- Stop service before updating.
- Apply update.
- Restart service.
- Back up before data migrations.
- Document recovery if the service fails to start.

Exit criteria:

- Update flow works while Always Available mode is installed.
- Family data remains separate from application binaries.

## Phase 8: Verification

- Test Always Available path selection.
- Test protected folder creation intent.
- Test portal URL settings.
- Test installer package creation.
- Test service start/stop/status commands.
- Test migration from Open Only data to Always Available data.
- Test update package creation and service restart behavior.

Exit criteria:

- Tests pass.
- Installer package can be built.
- Manual Always Available smoke test passes on Windows.

## Implementation Status

Implemented:

- Optional desktop or service host mode.
- Service-mode data root under `%PROGRAMDATA%/HomeschoolManager`.
- Protected-folder intent contract.
- Service install, uninstall, and desktop-to-service data copy helpers.
- Setup status panel that shows host mode, active records folder, and portal sharing.
- Parent-friendly Always Available operations docs.

Verification note:

- Infrastructure, web build, script parsing, and the custom test suite pass locally.
- Desktop host/package verification currently depends on restoring `Microsoft.Extensions.Hosting.WindowsServices` from NuGet. Local NuGet HTTPS access was blocked by a Windows TLS credential error during implementation.

## Resolved Direction

- Always Available is the recommended production setup; Open Only remains available.
- The parent supplies the Windows account to grant direct support-folder access during install.
- Administrator elevation is required for service install, uninstall, protected-folder setup, and desktop-to-service copy.
- Student Wi-Fi sharing remains off by default.
- The docs and Setup status explain that the computer must be on and awake.
