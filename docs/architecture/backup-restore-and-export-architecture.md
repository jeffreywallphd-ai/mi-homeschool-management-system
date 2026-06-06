# Backup Restore and Export Architecture

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: backup, restore, archive, and export architecture
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Local Data and File Storage](local-data-and-file-storage.md), [Backup Restore and Archive Export](../operations/backup-restore-and-archive-export.md)
- Related tests: not yet implemented
- Supersedes: none

## Backup Principle

Backups protect family-owned educational records. A backup should include enough data to restore the app's source records and generated documents without relying on application binaries or hidden external services.

## Full Backup Contents

A full backup should include:

- Database snapshot.
- Stored files.
- Generated documents.
- Document templates.
- Backup manifest.
- Checksums.
- App/data schema version.

## Export Types

- Full family backup.
- Manual backup.
- Automatic backup.
- Student archive export.
- Graduation packet export.
- Portfolio export.

## Restore Rules

- Restore must validate manifest, version, required files, and checksums where practical.
- Restore should report missing or damaged files clearly.
- Restore must not silently discard source records.
- Restore should distinguish full app restore from student archive export import.

## Contract Rule

Backup and restore services should operate through explicit manifests and storage contracts. They must not depend on undocumented folder scanning as the only source of truth.
