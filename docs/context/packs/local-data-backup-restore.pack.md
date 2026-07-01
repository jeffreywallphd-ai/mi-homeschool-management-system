# Local Data Backup Restore Pack

Purpose: Local storage, backups, restore, archive export, and data ownership.

## Canonical Sources

- `docs/architecture/local-data-and-file-storage.md`
- `docs/architecture/backup-restore-and-export-architecture.md`
- `docs/operations/backup-restore-and-archive-export.md`
- `docs/operations/upgrades-migrations-and-recovery.md`
- `docs/standards/data-retention-backup-and-recovery-standards.md`
- `docs/adr/ADR-0004-local-first-parent-pc-data-ownership.md`
- `docs/adr/ADR-0008-parent-authorized-encrypted-external-backups.md`

## Must Preserve

- Family data is local-first.
- Production binaries and update packages stay separate from `%LOCALAPPDATA%/HomeschoolManager` family data.
- Full backups include database, files, templates, generated docs, manifest, and checksums.
- Implemented V1 backup ZIP includes `manifest.json`, `manifest.md`, `checksums.json`, `data/`, `files/`, `templates/`, and `config/`; it excludes `backups/` and `logs/`.
- Restore must not silently drop records.
- Restore creates a pre-restore safety backup before replacing active source folders.
- Newer app versions should restore older supported full-backup formats; backup format changes need migration/recovery behavior and compatibility tests.
- Production migrations default to backup first; dev migrations default to backup opt-out.
- Optional off-computer backups follow ADR-0008; encryption is recommended and passphrases are never stored.
- Default off-computer backup writes to a parent-chosen synced folder selected through the browser/operating-system folder picker. It can save encrypted `.hsmbak` files or, by explicit parent choice after a warning, normal full-backup ZIP files.
- Google external-backup connection uses local protected tokens; if a restored config has no readable token, status must require reconnect instead of showing connected.
- Google Drive API and Gmail draft backup are advanced encrypted-only options, not the default parent workflow.
- Restore from Google Drive decrypts locally, then uses the normal full-backup validation and safety-backup restore rules.
- Restore from an encrypted `.hsmbak` file decrypts locally, then uses the normal full-backup validation and safety-backup restore rules.

## Common Failure Modes

- Relying only on folder scanning.
- Treating student archive export as equivalent to full restore.
- Uploading or emailing a plain full backup ZIP through direct provider actions instead of an encrypted external backup package.
