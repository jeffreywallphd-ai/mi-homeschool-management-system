# Local Backup and Restore Roadmap

- Status: implemented
- Last reviewed: 2026-06-13
- Canonical for: full local backup and restore implementation sequencing
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Backup Restore and Archive Export](../operations/backup-restore-and-archive-export.md), [Local Data and File Storage](../architecture/local-data-and-file-storage.md)
- Related tests: `Full local backup creates manifest checksums and restorable source files`, `Full local backup validation rejects incomplete packages`, `Full local restore validates backup and creates safety backup first`
- Supersedes: none

## Goal

Give the parent/admin a full local backup and restore workflow before adding optional off-computer backup destinations.

## Implemented Scope

- Parent/admin-only backup service contracts.
- Full local backup ZIP creation.
- Backup manifest and parent-readable manifest.
- Checksum file for included source files.
- Backup validation before restore.
- Restore preview.
- Explicit restore confirmation.
- Pre-restore safety backup under automatic backups.
- Backup history with download and delete.
- Parent/admin Backup & Restore page.

## Backup Package Shape

```text
manifest.json
manifest.md
checksums.json
data/
files/
templates/
config/
```

The backup package intentionally does not include `backups/` or `logs/` so that each new backup does not recursively include old backups and diagnostic files.

## Restore Safety

Restore validates the selected backup first. If valid, restore creates a pre-restore safety backup of the current records before replacing active source folders.

Active source folders replaced during restore:

- `data/`
- `files/`
- `templates/`
- `config/`

## Non-Scope

- Google Drive backup.
- Email backup.
- Backup encryption.
- Scheduled automatic backups.
- Cloud identity or cloud sync.

## Future Slice

The next backup-related slice should be encrypted off-computer backup destinations. It can reuse the full local backup ZIP as the source artifact.
