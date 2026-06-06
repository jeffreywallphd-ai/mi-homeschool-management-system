# Data Retention Backup and Recovery Standards

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: backup, restore, archive, and generated-document retention standards
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Backup Restore and Archive Export](../operations/backup-restore-and-archive-export.md)
- Related tests: not yet implemented
- Supersedes: none

## Retention Principle

Source records and generated official records should remain available for long-term family ownership.

## Backup Standards

- Full backups include database, files, templates, generated documents, and manifest.
- Backups include checksums where practical.
- Manual backup should exist in V1.
- Automatic backup may follow after manual backup is reliable.

## Restore Standards

- Validate manifest.
- Validate required files.
- Validate checksums where available.
- Report missing or damaged content plainly.
- Do not silently drop records.

## Migration Backup Standards

- Production/family migrations default to backup before migration.
- Parent/admin users may opt out only after a clear warning that data may be permanently lost if migration fails.
- Development migrations default to backup opt-out.
- Migration behavior must preserve source records, files, generated-document references, and restore options where practical.

## Archive Standards

Student archive exports should include both official records and enough source/evidence context for future interpretation.
