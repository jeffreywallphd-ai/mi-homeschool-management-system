# ADR-0008: Parent-Authorized External Backups

- Status: accepted
- Last reviewed: 2026-07-01
- Deciders: parent/project owner
- Technical story: Parents may want an off-computer backup copy in Google Drive or email while preserving local-first ownership.
- Supersedes: none

## Context

Family homeschool records, files, portfolio artifacts, transcripts, diplomas, and backup packages are sensitive. ADR-0004 makes the application local-first and defers external storage. Local backup and restore now exist, and parents may reasonably want a second copy in a provider or folder they control in case the family PC fails. Some parents may prefer a normal full-backup ZIP in a folder they already protect and sync themselves; others may prefer passphrase encryption before any copy leaves the application-managed data root.

Google Drive for desktop and similar sync tools expose cloud-backed folders as normal local folders. Google Drive also supports uploading files through the Drive API, including larger files through resumable upload. Google desktop OAuth uses a browser consent flow with a local callback and stores refresh tokens for later use. Gmail API email content is created as MIME and sent or drafted as base64url content. Gmail personal accounts limit attachments to 25 MB, while Workspace limits can vary by administrator.

## Decision

The application may support optional parent-authorized external backup destinations under these rules:

- Local backup remains the source of truth and the default workflow.
- Only a parent/admin may configure, connect, upload, email, download, decrypt, or restore external backups.
- Synced-folder backup uses parent-selected encryption: encryption is recommended and on by default, but a parent/admin may intentionally save a normal full-backup ZIP after seeing a warning that anyone with access to that folder can open it.
- When synced-folder encryption is selected, the full backup is encrypted with a parent-entered passphrase and written as an `.hsmbak` file.
- The passphrase is never stored by the application.
- The default off-computer workflow writes the backup to a parent-chosen synced folder selected through the browser/operating-system folder picker, such as Google Drive for desktop, OneDrive, Dropbox, USB, or a network folder. The app must not depend on a custom in-page folder tree or on receiving the full local folder path from the browser.
- Google OAuth client credentials are parent/app-owner configuration, not repository secrets.
- Google refresh/access tokens are stored locally using ASP.NET Core Data Protection and are not embedded in source control.
- Advanced Google API backup files must remain encrypted `.hsmbak` files in the parent's Drive folder so the parent can independently see and manage them.
- Advanced Gmail backup creates a parent-reviewable draft with the encrypted backup attached rather than silently sending sensitive records.
- Restore from an encrypted `.hsmbak` file or Google Drive API backup must require the passphrase, decrypt locally, and then use the existing backup validation and restore safety-backup rules.

## Consequences

- External backups remain optional. Families can continue using local-only backups.
- A parent who loses the encryption passphrase cannot restore an external encrypted backup from that file alone.
- A parent who turns off synced-folder encryption is responsible for the privacy of that folder and any cloud account or removable media that receives the plain ZIP.
- A restored installation may need to reconnect to Google if local OAuth tokens are unavailable or revoked.
- Synced-folder backup avoids requiring nontechnical parents to create Google OAuth credentials.
- Email backup is best for smaller encrypted backups because provider limits may block larger attachments.
- Documentation and UI must clearly distinguish local backups, parent-selected synced-folder backups, encrypted provider backups, and student archive exports.

## Follow-Up

- Consider automatic external backup scheduling only after manual external backup is reliable.
- Consider a future dedicated key-management design if the project later adds broader encryption at rest.
