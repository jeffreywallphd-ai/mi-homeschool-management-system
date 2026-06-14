# Google Drive and Email Backup Roadmap

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: implementation sequencing for optional encrypted off-computer backups
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0008](../adr/ADR-0008-parent-authorized-encrypted-external-backups.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Backup Restore and Archive Export](../operations/backup-restore-and-archive-export.md)
- Related tests: `Encrypted backup packages round-trip through the local backup validator`, `Remote backup service requires parent access and uses encrypted Google artifacts`
- Supersedes: none

## Scope

Add optional parent-authorized backup destinations that place encrypted full backups outside the family PC:

- Google Drive upload.
- Gmail draft with encrypted backup attachment.
- Google Drive restore by download, local decrypt, validation, and normal restore.

## Non-Scope

- Automatic backup scheduling.
- Cloud sync.
- External account login for app authentication.
- Silent email sending.
- General database/file encryption at rest.

## Design Details

- The Backup & Restore page remains the parent/admin workbench.
- Local backup controls stay first because local backup is the default and source of truth.
- Off-computer backup controls use a separate panel titled "Encrypted off-computer backup."
- Google setup lives in a support card with clear connection status and last-use dates.
- Google Drive restore has its own support card so the parent can refresh Drive files, enter the passphrase, preview, and confirm restore.
- The encrypted package uses `.hsmbak` so parents can distinguish it from a plain local backup ZIP.
- The passphrase field always reminds the parent that the app cannot recover the passphrase.
- Gmail creates a draft for parent review instead of silently sending sensitive student records.

## Phases

1. Record the external-backup privacy decision in ADR-0008.
2. Add encrypted backup package creation and decrypt-to-validate support.
3. Add Google OAuth client configuration and parent/admin connection flow.
4. Add Google Drive encrypted backup upload and listing.
5. Add Gmail draft creation with encrypted backup attachment and size guard.
6. Add Google Drive restore preview and restore through the existing local restore service.
7. Update docs, tests, and UI wording.

## Exit Criteria

- A parent/admin can create and download an encrypted backup.
- A student cannot use external backup actions.
- Google Drive and Gmail actions receive encrypted backup bytes only.
- Google Drive restore decrypts locally and uses the existing backup validation and safety-backup rules.
- Docs explain provider setup, passphrase care, size limits, and restore behavior.
