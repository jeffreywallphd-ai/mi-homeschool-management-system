# ADR-0007: Background Service Mode and Machine Data Root

- Status: accepted
- Date: 2026-06-13
- Supersedes: none
- Related docs: [Local Data and File Storage](../architecture/local-data-and-file-storage.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Local Installation and Data Location](../operations/local-installation-and-data-location.md), [Background Service Production Mode Roadmap](../roadmaps/background-service-production-mode-roadmap.md)

## Context

Some families need the student portal to remain available while the family PC is powered on, even when no Windows account is signed in. The desktop-host mode starts the app only inside an interactive parent session, so it cannot provide that behavior.

The family also prefers records to live in one protected location. Roaming profile storage is not appropriate because the app is local-first and should not imply cloud or domain sync.

## Decision

Homeschool Manager will support an optional Windows background service production mode.

In service mode:

- The service starts with Windows and can run when no Windows user is signed in.
- Family data is stored under `%PROGRAMDATA%/HomeschoolManager`.
- The service data folder is the one authoritative family data root for service mode.
- Installer and setup tooling should protect the folder with Windows permissions so normal student Windows accounts do not browse or edit the database directly.
- Parent/admin and student portal sharing settings remain independent.
- Parent/admin sharing defaults to `Localhost`.
- Student sharing defaults to `Localhost` until the parent enables Wi-Fi sharing.
- The existing desktop-host mode remains available and continues to use `%LOCALAPPDATA%/HomeschoolManager` by default.

## Consequences

- Production code must distinguish desktop mode from background service mode.
- Service mode cannot rely on an interactive desktop session or per-user local app data.
- Installer tooling must register the Windows Service and create protected machine-level folders.
- Updates in service mode must stop the service before replacing binaries and restart it afterward.
- Parent-facing UI and docs must explain that the PC must be on and awake for student Wi-Fi access.

## Guardrails

- Do not use Roaming profile storage for family records.
- Do not duplicate the active database across user and machine data roots.
- Do not enable student Wi-Fi sharing automatically.
- Do not expose parent/admin routes through the student portal.
- Do not rely on folder permissions as the only parent/admin authorization boundary.
- Preserve backups before moving data from desktop mode to service mode.
