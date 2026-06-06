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
- Full backups include database, files, templates, generated docs, manifest, and checksums.
- Restore must not silently drop records.
- Production migrations default to backup first; dev migrations default to backup opt-out.

## Common Failure Modes

- Relying only on folder scanning.
- Treating student archive export as equivalent to full restore.
