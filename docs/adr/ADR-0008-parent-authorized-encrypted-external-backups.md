# ADR-0008: Parent-Authorized Encrypted External Backups

- Status: accepted
- Last reviewed: 2026-06-13
- Deciders: parent/project owner
- Technical story: Parents may want an off-computer backup copy in Google Drive or email while preserving local-first ownership.
- Supersedes: none

## Context

Family homeschool records, files, portfolio artifacts, transcripts, diplomas, and backup packages are sensitive. ADR-0004 makes the application local-first and defers external storage. Local backup and restore now exist, and parents may reasonably want a second copy in a provider they control in case the family PC fails.

Google Drive supports uploading files through the Drive API, including larger files through resumable upload. Google desktop OAuth uses a browser consent flow with a local callback and stores refresh tokens for later use. Gmail API email content is created as MIME and sent or drafted as base64url content. Gmail personal accounts limit attachments to 25 MB, while Workspace limits can vary by administrator.

## Decision

The application may support optional parent-authorized external backup destinations under these rules:

- Local backup remains the source of truth and the default workflow.
- Only a parent/admin may configure, connect, upload, email, download, decrypt, or restore external backups.
- A full backup must be encrypted with a parent-entered passphrase before it leaves the local computer.
- The passphrase is never stored by the application.
- Google OAuth client credentials are parent/app-owner configuration, not repository secrets.
- Google refresh/access tokens are stored locally using ASP.NET Core Data Protection and are not embedded in source control.
- Google Drive backup files are visible files in the parent's Drive folder so the parent can independently see and manage them.
- Gmail backup creates a parent-reviewable draft with the encrypted backup attached rather than silently sending sensitive records.
- Restore from Google Drive must download the encrypted package, require the passphrase, decrypt it locally, and then use the existing backup validation and restore safety-backup rules.

## Consequences

- External backups remain optional. Families can continue using local-only backups.
- A parent who loses the encryption passphrase cannot restore an external encrypted backup from that file alone.
- A restored installation may need to reconnect to Google if local OAuth tokens are unavailable or revoked.
- Email backup is best for smaller encrypted backups because provider limits may block larger attachments.
- Documentation and UI must clearly distinguish local backups, encrypted external backups, and student archive exports.

## Follow-Up

- Consider automatic external backup scheduling only after manual external backup is reliable.
- Consider a future dedicated key-management design if the project later adds broader encryption at rest.
