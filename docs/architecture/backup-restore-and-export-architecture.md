# Backup Restore and Export Architecture

- Status: accepted
- Last reviewed: 2026-07-01
- Canonical for: backup, restore, archive, and export architecture
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0008](../adr/ADR-0008-parent-authorized-encrypted-external-backups.md)
- Related docs: [Local Data and File Storage](local-data-and-file-storage.md), [Backup Restore and Archive Export](../operations/backup-restore-and-archive-export.md)
- Related tests: `Full local backup creates manifest checksums and restorable source files`, `Full local backup validation rejects incomplete packages`, `Full local restore validates backup and creates safety backup first`, `Encrypted backup packages round-trip through the local backup validator`, `Synced folder backup prepares native-picker files and records saved copies`, `Remote backup service requires parent access and uses encrypted Google artifacts`, `Google provider builds Gmail drafts with RFC-style MIME headers`, `Setup page offers restore from backup only before setup is complete`
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

External-provider token files stored under `secrets/` are also outside the full backup ZIP contents. They are local connection credentials, not family source records. A restored installation may need the parent to reconnect Google backup. Google connection status must therefore be token-aware: a saved Google connected timestamp without a readable protected token is a reconnect-needed state, not a working connection.

## Synced Folder and Encrypted External Backup Packages

Optional off-computer backup destinations use the local full backup ZIP as the source artifact.

The default synced-folder workflow lets the parent/admin choose an existing local folder through the browser/operating-system folder picker. Browsers do not expose the full local folder path to the app; the browser grants a folder handle and writes the prepared backup file there. Encryption is recommended and selected by default. If encryption is selected, the app encrypts the backup before writing it to the selected folder. If encryption is turned off, the app writes a normal full-backup ZIP and must warn that anyone with access to that folder can open the backup.

Direct provider actions, including Google Drive API upload and Gmail draft creation, must encrypt before any upload or email action. The encrypted package uses this shape:

```text
encrypted-backup.json
payload.bin
```

The encrypted package is downloaded, written to a synced folder, or uploaded as an `.hsmbak` file. The parent-entered passphrase is required to decrypt the package. The passphrase is not stored.

The default off-computer workflow writes the selected backup file to a parent-chosen synced folder, such as a Google Drive for desktop folder, OneDrive folder, Dropbox folder, USB drive, or network folder. The sync provider is outside the app boundary and is responsible for moving that file off the family PC.

Advanced Google API backup can also store encrypted files in a visible `Homeschool Manager Backups` folder in the parent's Drive. Google OAuth uses an installed-app loopback callback on `127.0.0.1`, so connection must be initiated from the computer running the app. Gmail backup creates a draft with the encrypted backup attached so the parent can review it before sending. Draft messages should include normal RFC-style MIME headers and use the connected Gmail account as the sender. Gmail is intended for smaller backup files because attachment limits vary and personal Gmail accounts limit attachments to 25 MB.

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
- Backup restore should be backward compatible across application updates. Newer versions of Homeschool Manager should continue to restore older supported full-backup formats, using explicit format/schema migration when needed.
- If a backup format must be retired, the app must fail with a clear message and provide a documented recovery path rather than silently dropping records.
- Restore from Google Drive must download the encrypted package, decrypt locally with the parent passphrase, and then use the normal full-backup validation and restore rules.
- Restore from a local encrypted `.hsmbak` file must decrypt locally with the parent passphrase, then use the normal full-backup validation and restore rules.
- Restore from a plain synced-folder ZIP uses the same normal full-backup validation and restore rules as a manual backup ZIP.
- Before required setup is complete, the Setup page may expose a startup restore path for full backup ZIP and encrypted `.hsmbak` files. After setup is complete, restore should be available only from Backup & Restore.

## Contract Rule

Backup and restore services should operate through explicit manifests and storage contracts. They must not depend on undocumented folder scanning as the only source of truth.

Backup format changes must keep versioned restore fixtures or equivalent compatibility tests for older supported backup formats. A newer app should prove it can restore prior supported backup versions before the backup format is changed or released.
