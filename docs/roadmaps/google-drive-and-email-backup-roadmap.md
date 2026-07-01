# Google Drive and Email Backup Roadmap

- Status: accepted
- Last reviewed: 2026-07-01
- Canonical for: implementation sequencing for optional off-computer backups
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0008](../adr/ADR-0008-parent-authorized-encrypted-external-backups.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Backup Restore and Archive Export](../operations/backup-restore-and-archive-export.md)
- Related tests: `Encrypted backup packages round-trip through the local backup validator`, `Synced folder backup prepares native-picker files and records saved copies`, `Remote backup service requires parent access and uses encrypted Google artifacts`, `Google provider builds Gmail drafts with RFC-style MIME headers`
- Supersedes: none

## Scope

Add optional parent-authorized backup destinations that place full backups outside the family PC:

- Default synced-folder backup for Google Drive for desktop, OneDrive, Dropbox, USB, network folders, or similar parent-controlled locations, with parent-selected encryption.
- Encrypted `.hsmbak` restore from local or synced folders.
- Advanced Google Drive API upload.
- Advanced Gmail draft with encrypted backup attachment.
- Advanced Google Drive API restore by download, local decrypt, validation, and normal restore.

## Non-Scope

- Automatic backup scheduling.
- Automatic cloud sync controlled by the app.
- External account login for app authentication.
- Silent email sending.
- General database/file encryption at rest.

## Design Details

- The Backup & Restore page remains the parent/admin workbench.
- Local backup controls stay first because local backup is the default and source of truth.
- Off-computer backup controls use a main panel titled "Synced folder backup."
- The synced folder is selected through the browser/operating-system folder picker. The app does not store or require the full folder path, and backup passphrases are never stored.
- The default save action writes encrypted `.hsmbak` files to the parent-chosen folder.
- A parent/admin may intentionally turn off synced-folder encryption and write a normal full-backup ZIP after seeing a warning that anyone with access to the folder can open it.
- Google API setup lives in a collapsed advanced support card with clear connection status and last-use dates.
- Google OAuth uses a `127.0.0.1` installed-app callback and should be started from the computer running Homeschool Manager, even when the portals are otherwise available through Wi-Fi sharing.
- Google status treats a saved connected timestamp without a readable local token as reconnect-needed.
- Google Drive API restore stays under the advanced support card so the parent can refresh Drive files, enter the passphrase, preview, and confirm restore.
- Local encrypted restore accepts `.hsmbak`, decrypts locally, and then uses the same validation and safety-backup restore rules as plain ZIP restore.
- The encrypted package uses `.hsmbak` so parents can distinguish it from a plain local backup ZIP.
- Passphrase fields are shown only for encrypted flows and remind the parent that the app cannot recover the passphrase.
- Gmail creates a draft for parent review instead of silently sending sensitive student records; the draft uses the connected Gmail address and normal MIME headers.

## Phases

1. Record the external-backup privacy decision in ADR-0008.
2. Add encrypted backup package creation and decrypt-to-validate support.
3. Add Google OAuth client configuration and parent/admin connection flow.
4. Add Google Drive encrypted backup upload and listing.
5. Add Gmail draft creation with encrypted backup attachment and size guard.
6. Add Google Drive restore preview and restore through the existing local restore service.
7. Update docs, tests, and UI wording.
8. Harden connection status and Gmail draft formatting after end-to-end review.
9. Make synced-folder backup the default parent workflow and keep Google OAuth/Gmail under Advanced Google API backup.
10. Replace raw synced-folder path entry and custom folder browsing with the browser/operating-system folder picker, and add parent-selected encryption for synced-folder backups.

## Exit Criteria

- A parent/admin can create and download an encrypted backup.
- A parent/admin can choose a synced folder through the native picker and save either an encrypted `.hsmbak` backup or an explicitly unencrypted full-backup ZIP without entering Google OAuth credentials.
- A parent/admin can preview and restore an encrypted `.hsmbak` file through local decrypt and existing restore validation.
- A student cannot use external backup actions.
- Google Drive and Gmail actions receive encrypted backup bytes only.
- Google Drive restore decrypts locally and uses the existing backup validation and safety-backup rules.
- Docs explain provider setup, passphrase care, size limits, and restore behavior.
