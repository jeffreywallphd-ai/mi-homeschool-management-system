# Backup Restore and Archive Export

- Status: accepted
- Last reviewed: 2026-07-01
- Canonical for: operational expectations for backups, restore, and student archive exports
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0008](../adr/ADR-0008-parent-authorized-encrypted-external-backups.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Data Retention Backup and Recovery Standards](../standards/data-retention-backup-and-recovery-standards.md)
- Related tests: `Full local backup creates manifest checksums and restorable source files`, `Full local backup validation rejects incomplete packages`, `Full local restore validates backup and creates safety backup first`, `Encrypted backup packages round-trip through the local backup validator`, `Synced folder backup prepares native-picker files and records saved copies`, `Remote backup service requires parent access and uses encrypted Google artifacts`, `Google provider builds Gmail drafts with RFC-style MIME headers`, `Setup page offers restore from backup only before setup is complete`
- Supersedes: none

## Manual Backup

Manual backup should be available in V1. A parent should be able to create a full backup intentionally before major milestones, document generation, upgrades, or school-year closeout.

Implemented parent/admin workflow:

1. Open Backup & Restore.
2. Select Create full backup.
3. The app saves a copy under `backups/manual`.
4. The app offers the ZIP for download.

## Automatic Backup

Automatic backup is recommended after manual backup is reliable. Scheduling details are deferred until implementation planning.

Implemented automatic foundation:

- Restore creates a pre-restore safety backup under `backups/automatic` before replacing current records.

Scheduled automatic backups are still deferred.

## Full Backup Contents

A full backup should include:

- Database snapshot.
- Stored files.
- Generated documents.
- Templates.
- Manifest.
- Checksums where practical.
- Data/schema version.

Implemented V1 ZIP shape:

```text
manifest.json
manifest.md
checksums.json
data/
files/
templates/
config/
```

The backup does not include previous backups or logs.

## Restore

Restore should:

- Validate the manifest.
- Check required files.
- Check checksums where available.
- Report missing or damaged content clearly.
- Avoid silently discarding records.
- Require parent/admin confirmation.
- Create a pre-restore safety backup before replacing current records.

Restore replaces active source folders from the selected backup:

- `data/`
- `files/`
- `templates/`
- `config/`

## First Launch Restore From Backup

When Homeschool Manager has not completed required setup yet, the Setup page should offer a parent/admin a way to set up from an existing full backup. This startup restore path supports the same plain full-backup ZIP files and encrypted `.hsmbak` files as Backup & Restore. It must validate the backup, preview the restore, require explicit confirmation, and create a pre-restore safety backup before replacing records.

After required setup is complete, the startup restore option should no longer appear on Setup. Ongoing backup and restore actions belong in Backup & Restore only.

## Synced Folder Backup

Synced folder backup is the default off-computer workflow for parents who want a second copy away from the family PC without creating Google developer credentials. It works with Google Drive for desktop, OneDrive, Dropbox, USB drives, network folders, and other folders the parent controls.

Parent workflow:

1. Install and sign in to Google Drive for desktop or another sync app, if using cloud storage.
2. Choose or create a folder such as `Homeschool Manager Backups`.
3. In Backup & Restore, select Choose folder and use the browser/operating-system folder picker to choose that folder.
4. Leave encryption selected and enter a backup passphrase, or intentionally turn encryption off after reading the warning.
5. Select Save backup to synced folder.
6. If encryption was used, keep the passphrase somewhere safe and separate from the backup file.

The app creates a normal full local backup first. With encryption selected, it encrypts the backup locally and writes an `.hsmbak` file to the selected folder through the browser-granted folder handle. With encryption turned off, it writes a normal full-backup ZIP to the selected folder. The sync app is responsible for copying that file to the parent's cloud account. Homeschool Manager does not store encryption passphrases and cannot recover them. Browsers do not expose the full selected folder path to Homeschool Manager.

## Restore From Encrypted Backup

Parent workflow:

1. Open Backup & Restore.
2. Choose a full backup ZIP or encrypted `.hsmbak` file.
3. For `.hsmbak` files, enter the backup passphrase.
4. Preview the backup.
5. Confirm restore.

The app decrypts encrypted backup files locally, validates the full backup, and creates a pre-restore safety backup before replacing current records. Plain ZIP backups are validated directly before restore.

## Advanced Google API Backup

Google API backup remains available as an advanced option for families or project maintainers who intentionally configure a Google OAuth client ID. It is not the default parent workflow.

Advanced parent workflow:

1. Create a Google OAuth desktop/client ID in Google Cloud and paste it into Backup & Restore.
2. On the computer running Homeschool Manager, select Connect Google and approve the requested Drive/Gmail permissions in the browser.
3. Enter a backup passphrase.
4. Select Save to Google Drive API.
5. Keep the passphrase somewhere safe and separate from the backup file.

The app creates a normal full local backup first, encrypts it locally, and uploads only the encrypted `.hsmbak` file to a visible `Homeschool Manager Backups` folder in Google Drive.

Google uses an installed-app loopback callback on `127.0.0.1`. This means Google connection must be started from the same Windows session and computer that is hosting Homeschool Manager, not from another device on Wi-Fi sharing.

Google connection tokens are stored locally and are not treated as family source records. After restoring on a new computer, changing the Google client ID, or moving data to a different Windows account, the parent may need to reconnect Google backup. The Backup & Restore page should show this as a reconnect-needed state instead of claiming Google is connected.

## Advanced Gmail Draft Backup

Gmail draft backup uses the same advanced Google API connection. The app creates a normal full local backup, encrypts it, and creates a Gmail draft with the encrypted `.hsmbak` file attached. The draft is created from the connected Gmail account, includes normal MIME email headers, and is left for the parent to review and send manually.

Gmail is best for smaller backup files. Personal Gmail accounts limit attachments to 25 MB, and work or school accounts may have administrator-defined limits. Larger backups should use Google Drive.

## Advanced Restore From Google Drive API

Parent workflow:

1. Open Backup & Restore.
2. Refresh Google Drive backups.
3. Enter the backup passphrase.
4. Preview the selected backup.
5. Confirm restore.

The app downloads the encrypted file, decrypts it locally, validates the full backup, and creates a pre-restore safety backup before replacing current records.

## Student Archive Export

A student archive export is not the same as a full app backup. It should package records for long-term family use, transfer, graduation, or external review. It may include generated records, course descriptions, portfolio index, selected artifacts, and supporting evidence.

An approved portfolio archive packet is one student archive export type. It is generated by the parent/admin from a parent/admin-approved portfolio snapshot and may include:

- Printable portfolio HTML.
- JSON and Markdown manifests.
- Approved section and item context.
- Accepted evidence-file copies.
- Missing-file warnings.

The packet is a family-owned record export, not a full restore package. Creating it must not change source grades, credits, assessments, evidence facts, or official-record history.

## User Experience

Backup and restore wording must be nontechnical where possible. The system should explain what will be included and whether the result is a full backup or an archive export.

External-backup wording must explain when a backup is encrypted, when it is a plain ZIP, and that the parent must remember any passphrase because the app cannot recover it.
