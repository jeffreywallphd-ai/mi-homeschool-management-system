# Local Data Backup Restore Pack

Purpose: Local storage, backups, restore, archive export, and data ownership.

## Canonical Sources

- `docs/architecture/local-data-and-file-storage.md`
- `docs/architecture/backup-restore-and-export-architecture.md`
- `docs/operations/backup-restore-and-archive-export.md`
- `docs/operations/upgrades-migrations-and-recovery.md`
- `docs/standards/data-retention-backup-and-recovery-standards.md`
- `docs/adr/ADR-0004-local-first-parent-pc-data-ownership.md`

## Must Preserve

- Family data is local-first.
- Production binaries and update packages stay separate from `%LOCALAPPDATA%/HomeschoolManager` family data.
- Full backups include database, files, templates, generated docs, manifest, and checksums.
- Implemented V1 backup ZIP includes `manifest.json`, `manifest.md`, `checksums.json`, `data/`, `files/`, `templates/`, and `config/`; it excludes `backups/` and `logs/`.
- Restore must not silently drop records.
- Restore creates a pre-restore safety backup before replacing active source folders.
- Production migrations default to backup first; dev migrations default to backup opt-out.
- Optional Google Drive and Gmail backup use encrypted `.hsmbak` packages under ADR-0008; the parent passphrase is never stored.
- Restore from Google Drive decrypts locally, then uses the normal full-backup validation and safety-backup restore rules.

## Common Failure Modes

- Relying only on folder scanning.
- Treating student archive export as equivalent to full restore.
- Uploading or emailing a plain full backup ZIP instead of an encrypted external backup package.
