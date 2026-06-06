# ASP.NET Blazor SQLite Stack

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: initial application stack and hosting direction
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [System Overview](system-overview.md), [Modular Monolith Boundaries](modular-monolith-boundaries.md), [Local Installation and Data Location](../operations/local-installation-and-data-location.md)
- Related tests: not yet implemented
- Supersedes: none

## Stack Decision

The initial application stack is:

- ASP.NET Core.
- Blazor Web App.
- SQLite for local persistence.
- Local file storage for attachments, generated documents, backups, and archives.
- Modular monolith structure with Web, Application, Domain, Infrastructure, and Tests projects.

## Hosting Direction

The first implementation should run as a local web app on the parent PC. It may run from source during development and later be packaged for nontechnical local use.

The system should avoid premature API/server separation. A separate public API, cloud service, or multi-device server is not part of the initial stack decision.

## Blazor Direction

Blazor UI should be contract-backed:

- Components use explicit view models.
- Form submission maps to application commands.
- UI validation mirrors command validation where practical.
- Domain objects are not directly edited by UI forms.

## SQLite Direction

SQLite is the initial database because the system is local-first, parent-owned, and does not need a server database for V1.

SQLite usage must preserve:

- Explicit schema versioning.
- Migration awareness.
- Backup compatibility.
- Clear separation between database records and stored files.

## Dependency Rule

New framework, persistence, rendering, or hosting dependencies must be reviewed against local-first ownership, privacy, portability, backup/restore, licensing, and boundary discipline.
