# Backup Restore and Export Architecture

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: backup, restore, archive, and export architecture
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0008](../adr/ADR-0008-parent-authorized-encrypted-external-backups.md)
- Related docs: [Local Data and File Storage](local-data-and-file-storage.md), [Backup Restore and Archive Export](../operations/backup-restore-and-archive-export.md)
- Related tests: `Full local backup creates manifest checksums and restorable source files`, `Full local backup validation rejects incomplete packages`, `Full local restore validates backup and creates safety backup first`, `Encrypted backup packages round-trip through the local backup validator`, `Remote backup service requires parent access and uses encrypted Google artifacts`
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

The implemented V1 local backup ZIP uses this shape:

```text
manifest.json
manifest.md
checksums.json
data/
files/
templates/
config/
```

The `backups/` and `logs/` folders are intentionally excluded from full backup ZIP contents. This keeps backups from recursively including prior backups and avoids treating diagnostic logs as source records.

External-provider token files stored under `secrets/` are also outside the full backup ZIP contents. They are local connection credentials, not family source records. A restored installation may need the parent to reconnect Google backup.

## Encrypted External Backup Package

Optional off-computer backup destinations use the local full backup ZIP as the source artifact, then encrypt it before any upload or email action. The encrypted package uses this shape:

```text
encrypted-backup.json
payload.bin
```

The encrypted package is downloaded or uploaded as an `.hsmbak` file. The parent-entered passphrase is required to decrypt the package. The passphrase is not stored.

Google Drive backup stores encrypted files in a visible `Homeschool Manager Backups` folder in the parent's Drive. Gmail backup creates a draft with the encrypted backup attached so the parent can review it before sending. Gmail is intended for smaller backup files because attachment limits vary and personal Gmail accounts limit attachments to 25 MB.

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
- Restore must create a pre-restore safety backup before replacing active source folders.
- Restore from Google Drive must download the encrypted package, decrypt locally with the parent passphrase, and then use the normal full-backup validation and restore rules.

## Contract Rule

Backup and restore services should operate through explicit manifests and storage contracts. They must not depend on undocumented folder scanning as the only source of truth.
