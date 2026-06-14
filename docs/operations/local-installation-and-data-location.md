# Local Installation and Data Location

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: local running mode and family data location
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0007](../adr/ADR-0007-background-service-mode-and-machine-data-root.md)
- Related docs: [ASP.NET Blazor SQLite Stack](../architecture/aspnet-blazor-sqlite-stack.md), [Local Data and File Storage](../architecture/local-data-and-file-storage.md)
- Related tests: not yet implemented
- Supersedes: none

## Running Modes

During development, the app may run from source using the local development server.

For family use, the app should be able to run locally on the parent PC without requiring a cloud account or hosted service.

## Data Location

The default desktop-mode family-data root is:

```text
%LOCALAPPDATA%/HomeschoolManager
```

The optional background-service family-data root is:

```text
%PROGRAMDATA%/HomeschoolManager
```

Family data must remain separate from application binaries.

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

The installed production shape uses a desktop host named `HomeschoolManager`. The host starts the parent/admin portal and
student portal as separate local web processes and opens the parent/admin portal in the default browser.

Desktop-mode production runtime settings are stored at:

```text
%LOCALAPPDATA%/HomeschoolManager/config/production-settings.json
```

Service-mode production runtime settings are stored at:

```text
%PROGRAMDATA%/HomeschoolManager/config/production-settings.json
```

Each portal can be configured independently:

- `Localhost`: same-computer access only, binding to `127.0.0.1`.
- `Wifi`: household network access, binding to a selected Wi-Fi IP address when configured.

The default is same-computer access for both portals. Wi-Fi sharing should be enabled only by the parent/admin.

## Optional Background Service Run

The installed production app can also be configured as a Windows background service. In that mode, the app starts with Windows and can keep the student portal available while the computer is on, even when no parent is signed in.

Service mode uses `%PROGRAMDATA%/HomeschoolManager` as the one authoritative family-data root. Existing desktop-mode records must be backed up and copied into that folder before switching the family to service mode. The service installer must protect the folder for Windows, administrators, the service account, and the parent setup account when provided.
