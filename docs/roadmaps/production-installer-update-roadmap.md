# Production Installer Update Roadmap

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: production installer, update, desktop host, and local sharing implementation
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0006](../adr/ADR-0006-production-installer-update-and-local-sharing.md)
- Related docs: [Local Data and File Storage](../architecture/local-data-and-file-storage.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Upgrades Migrations and Recovery](../operations/upgrades-migrations-and-recovery.md)
- Related tests: `src/HomeschoolManager.Tests/Program.cs`
- Supersedes: none

## Scope

Create the production foundation for installing, launching, configuring, and updating the local homeschool manager on Windows.

## Non-Scope

- Cloud sync or hosted family records.
- External identity providers.
- Encryption-at-rest decisions.
- Public code-signing certificate procurement.
- Release hosting account setup.

## Phase 1: Decision and Research

- Review local data, backup/recovery, identity, and installer/update docs.
- Choose the production host plus Velopack packaging path.
- Keep MSIX/App Installer as a future option.

Exit criteria:

- ADR records the production distribution decision.

## Phase 2: Runtime Configuration

- Define production settings for the parent/admin and student portals.
- Support independent sharing modes: `Localhost` and `Wifi`.
- Store production settings under the local app data config folder.
- Create production data, files, templates, backups, logs, and config directories.

Exit criteria:

- Each portal can compute its own bind URL and display URL.
- Localhost defaults preserve same-computer access.
- Wi-Fi mode is explicit and produces warnings when no specific Wi-Fi address is selected.

## Phase 3: Desktop Host

- Add a host executable that reads production settings.
- Start admin and student portals with their configured URLs.
- Pass the production data root to both portals.
- Pass the configured student portal base URL to the admin portal.
- Provide dry-run and print-config modes for installer smoke tests.

Exit criteria:

- The host can report effective production settings without starting portals.
- The host can launch both portals from a packaged layout.

## Phase 4: Release Packaging

- Publish the desktop host, admin portal, and student portal into one application layout.
- Package the application layout with Velopack when `vpk` is available.
- Generate release metadata and package outputs.

Exit criteria:

- A release layout can be produced locally.
- A release machine with Velopack can produce installer/update feed artifacts.

## Phase 5: Update and Recovery Safety

- Keep application binaries separate from family data.
- Default update-time and migration-time behavior toward backup first.
- Preserve local data on uninstall unless explicitly removed.
- Add release notes and update feed configuration points.

Exit criteria:

- Update packaging does not include family data.
- Production data location and backup expectations are documented.

## Phase 6: Verification

- Test endpoint construction, production paths, settings persistence, and host dry-run output.
- Build all projects.
- Run the contract test suite.
- Run packaging layout creation with `-SkipVelopack`.

Exit criteria:

- Tests and builds pass.
- Release layout contains host, admin portal, student portal, and manifest.
