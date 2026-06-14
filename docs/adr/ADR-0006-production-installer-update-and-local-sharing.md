# ADR-0006: Production Installer Update and Local Sharing

- Status: accepted
- Date: 2026-06-13
- Supersedes: none
- Related docs: [Local Data and File Storage](../architecture/local-data-and-file-storage.md), [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Local Installation and Data Location](../operations/local-installation-and-data-location.md), [Upgrades Migrations and Recovery](../operations/upgrades-migrations-and-recovery.md)

## Context

The application needs a production-friendly Windows installation path with update capability while preserving parent-owned local records. The parent/admin and student portals also need independent runtime sharing settings so either portal can be limited to the parent computer or shared on the household Wi-Fi network.

## Decision

The first production distribution path uses a small desktop host plus Velopack release packaging.

The desktop host:

- Starts and stops the parent/admin portal and student portal as separate local web processes.
- Reads a local production settings file from `%LOCALAPPDATA%/HomeschoolManager/config/production-settings.json`.
- Lets each portal independently choose `Localhost` or `Wifi` sharing mode.
- Keeps family data under `%LOCALAPPDATA%/HomeschoolManager`, separate from installed application binaries.
- Passes the configured data root and portal URLs into the web processes.
- Opens the parent/admin portal in the default browser when configured.

Velopack is the first installer/update packaging target because it supports a traditional Windows installer, release feed files, and update packages around an ordinary compiled application layout. MSIX/App Installer remains a possible future distribution target, but it is not the first implementation.

## Consequences

- Production lifecycle behavior is owned by the desktop host, not by the Blazor web projects.
- The web projects remain focused on homeschool workflows and authorization contracts.
- Update packages replace application binaries only. Family data remains outside the install folder.
- Release packaging requires a release machine with the Velopack CLI and code-signing setup before public distribution.
- Wi-Fi sharing is explicit per portal and must be paired with clear parent/admin access controls.

## Guardrails

- Do not move student or family records to an external update service.
- Do not enable Wi-Fi sharing automatically.
- Do not expose parent/admin routes through the student portal.
- Create or require a local backup before production migrations or update-time data changes.
- Preserve uninstall behavior that keeps family data unless the parent explicitly chooses deletion.
