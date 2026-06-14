# Upgrades Migrations and Recovery

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: upgrade, migration, backup-before-migration, and recovery expectations
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0007](../adr/ADR-0007-background-service-mode-and-machine-data-root.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Backup Restore and Archive Export](backup-restore-and-archive-export.md), [Background Service Mode](background-service-mode.md), [Data Retention Backup and Recovery Standards](../standards/data-retention-backup-and-recovery-standards.md)
- Related tests: not yet implemented
- Supersedes: none

## Migration Principle

Once real student records exist, migrations are high-risk operations. They must preserve source records, files, generated documents, and archive integrity.

## Versioning

The system should track:

- Application version.
- Database schema version.
- Data format version when needed.
- Backup manifest version.

## Backup Before Migration

Production/family use should default to creating or requiring a backup before migration.

The parent/admin may opt out of backup before migration only after a clear warning that data may be permanently lost if migration fails.

Development mode should default to opt out of backup before migration because dev data may be disposable and repeated backups would slow iteration.

Production update packages replace application binaries only. They must not include family data. Before an update performs data-format or schema changes, the app should create or require a local backup under the production backup folder.

The local Backup & Restore page can create a full backup ZIP under `backups/manual`. Restore creates a pre-restore safety backup under `backups/automatic` before replacing current records.

## Background Service Updates

Background service installations should be updated intentionally:

1. Create or confirm a recent family-record backup.
2. Stop the `HomeschoolManager` Windows service.
3. Install the newer app package.
4. Start the `HomeschoolManager` service.
5. Open Setup and confirm the active data folder and portal sharing.

Service-mode updates must not delete or overwrite `%PROGRAMDATA%/HomeschoolManager`.

## Desktop To Service Migration

Switching from desktop mode to service mode is a data-location migration. The migration helper must copy records from `%LOCALAPPDATA%/HomeschoolManager` to `%PROGRAMDATA%/HomeschoolManager`, create a backup first, and leave the original folder in place.

## Migration Rules

- Validate current schema/version before migrating.
- Create a backup first in production unless the parent/admin opts out.
- Do not silently delete records or files.
- Preserve generated document records and source links.
- Report migration failures clearly.
- Record migration completion status.

## Recovery

After migration failure, the system should guide the parent/admin toward restore or recovery options.

Recovery guidance should be plain-language and should distinguish:

- Restoring a full backup.
- Reopening an unmigrated data set.
- Exporting or preserving files manually when automated recovery is not possible.

## Test Expectations

Migration tests should cover:

- Version detection.
- Successful migration.
- Failed migration.
- Backup opt-in production path.
- Backup opt-out warning path.
- Dev opt-out default.
- Preservation of records, files, and generated-document references.
