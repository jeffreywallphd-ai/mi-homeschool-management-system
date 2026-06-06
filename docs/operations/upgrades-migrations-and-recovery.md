# Upgrades Migrations and Recovery

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: upgrade, migration, backup-before-migration, and recovery expectations
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Backup Restore and Archive Export](backup-restore-and-archive-export.md), [Data Retention Backup and Recovery Standards](../standards/data-retention-backup-and-recovery-standards.md)
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
