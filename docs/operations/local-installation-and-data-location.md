# Local Installation and Data Location

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: local running mode and family data location
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [ASP.NET Blazor SQLite Stack](../architecture/aspnet-blazor-sqlite-stack.md), [Local Data and File Storage](../architecture/local-data-and-file-storage.md)
- Related tests: not yet implemented
- Supersedes: none

## Running Modes

During development, the app may run from source using the local development server.

For family use, the app should be able to run locally on the parent PC without requiring a cloud account or hosted service.

## Data Location

The default family-data root is:

```text
%LOCALAPPDATA%/HomeschoolManager
```

Family data must remain separate from application binaries.

## Data Root Contents

Expected contents:

- `data/` for SQLite database files.
- `files/` for student, curriculum, portfolio, submission, official-record, and generated-document files.
- `backups/` for manual backups, automatic backups, and exports.
- `templates/` for document templates.
- `logs/` for privacy-safe diagnostics.

## User Visibility

The app should make the active data location visible to the parent/admin.

Changing the data location is deferred until implementation planning, but the architecture should not make it impossible.

## Dev vs Production

Development mode may use a separate local dev data root or disposable database. Production/family use should use the default family-data root unless the parent/admin chooses another supported location in a future version.

## Installation Boundary

Installation should not overwrite, delete, or migrate family data without explicit migration/recovery handling.
