# Local Installation and Data Location

- Status: accepted
- Last reviewed: 2026-07-02
- Canonical for: local running mode and family data location
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0007](../adr/ADR-0007-background-service-mode-and-machine-data-root.md), [ADR-0009](../adr/ADR-0009-family-setup-maintenance-wrapper.md)
- Related docs: [ASP.NET Blazor SQLite Stack](../architecture/aspnet-blazor-sqlite-stack.md), [Local Data and File Storage](../architecture/local-data-and-file-storage.md)
- Related tests: not yet implemented
- Supersedes: none

## Running Modes

During development, the app may run from source using the local development server.

For family use, the app should be able to run locally on the parent PC without requiring a cloud account or hosted service.

## Data Location

The default desktop-mode family-data root is:

```text
%LOCALAPPDATA%/HomeschoolManagerData
```

The recommended Always Available family-data root is:

```text
%PROGRAMDATA%/HomeschoolManager
```

Family data must remain separate from application binaries.

Older prerelease desktop builds may have stored records under `%LOCALAPPDATA%/HomeschoolManager`, which can also be used by the installer for application files. Current desktop builds copy known family-data folders from that legacy location into `%LOCALAPPDATA%/HomeschoolManagerData` on first launch when the new folder is empty. The old folder is left in place as a safety copy.

## Data Root Contents

Expected contents:

- `data/` for SQLite database files.
- `files/` for student, curriculum, portfolio, submission, official-record, and generated-document files.
- `backups/` for manual backups, automatic backups, and exports.
- `templates/` for document templates.
- `logs/` for privacy-safe diagnostics.
- `config/` for production runtime settings.

## User Visibility

The app should make the active data location visible to the parent/admin.

Changing the data location is deferred until implementation planning, but the architecture should not make it impossible.

## Dev vs Production

Development mode may use a separate local dev data root or disposable database. Production/family use should use the default family-data root unless the parent/admin chooses another supported location in a future version.

## Installation Boundary

Installation should not overwrite, delete, or migrate family data without explicit migration/recovery handling.

## Installed Production Run

The recommended family-facing installer is `HomeschoolManager-Family-Setup.exe`. It runs the packaged app installer, asks the parent whether to use Always Available or Open Only, and registers the Homeschool Manager maintenance uninstall prompt when possible.

Raw Velopack `HomeschoolManager-stable-Setup.exe` remains an advanced package artifact. It should not be the primary installer handed to nontechnical families because it cannot show Homeschool Manager setup or uninstall data-retention prompts.

The installed production shape uses a desktop host named `HomeschoolManager`. The host starts the parent/admin portal and student portal as separate local web processes and opens the parent/admin portal in the default browser.

Desktop-mode production runtime settings are stored at:

```text
%LOCALAPPDATA%/HomeschoolManagerData/config/production-settings.json
```

Service-mode production runtime settings are stored at:

```text
%PROGRAMDATA%/HomeschoolManager/config/production-settings.json
```

Each portal can be configured independently:

- `Localhost`: same-computer access only, binding to `127.0.0.1`.
- `Wifi`: household network access, binding to a selected Wi-Fi IP address when configured.

The default is same-computer access for both portals. Wi-Fi sharing should be enabled only by the parent/admin.

## Always Available Production Run

The installed production app can be configured for Always Available access. In that mode, Windows starts Homeschool Manager in the background and can keep the student portal available while the computer is on and awake, even when no parent is signed in.

Always Available mode uses `%PROGRAMDATA%/HomeschoolManager` as the one authoritative family-data root. Existing Open Only records must be backed up and copied into that folder before switching the family to Always Available mode. The helper must protect the folder for Windows, administrators, the background runner account, and the parent setup account when provided.

Open Only remains available and uses `%LOCALAPPDATA%/HomeschoolManagerData` by default. In Open Only mode, students can use the student portal only while the parent has Homeschool Manager open.

## Uninstall and Data Retention

When Homeschool Manager was installed through `HomeschoolManager-Family-Setup.exe`, Windows Add/Remove should route uninstall to the Homeschool Manager maintenance prompt for that Windows account. The prompt defaults to keeping family records on the computer.

The parent may choose to remove family records. That destructive choice must require exact confirmation and should create a safety archive first. Family records include setup details, requirements, course plans, gradebook/report-card data, transcript and diploma source records, portfolio files, stored submissions, generated documents, and local backups/exports under the selected data roots.

If a parent runs raw Velopack `Setup.exe` directly, Windows Add/Remove uses Velopack's uninstall behavior and does not show Homeschool Manager's data-retention prompt. In that case, family records still remain outside the app binaries unless the parent removes them through the maintenance tool or manually deletes the data folders.
