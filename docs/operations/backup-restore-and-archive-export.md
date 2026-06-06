# Backup Restore and Archive Export

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: operational expectations for backups, restore, and student archive exports
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md), [Data Retention Backup and Recovery Standards](../standards/data-retention-backup-and-recovery-standards.md)
- Related tests: not yet implemented
- Supersedes: none

## Manual Backup

Manual backup should be available in V1. A parent should be able to create a full backup intentionally before major milestones, document generation, upgrades, or school-year closeout.

## Automatic Backup

Automatic backup is recommended after manual backup is reliable. Scheduling details are deferred until implementation planning.

## Full Backup Contents

A full backup should include:

- Database snapshot.
- Stored files.
- Generated documents.
- Templates.
- Manifest.
- Checksums where practical.
- Data/schema version.

## Restore

Restore should:

- Validate the manifest.
- Check required files.
- Check checksums where available.
- Report missing or damaged content clearly.
- Avoid silently discarding records.

## Student Archive Export

A student archive export is not the same as a full app backup. It should package records for long-term family use, transfer, graduation, or external review. It may include generated records, course descriptions, portfolio index, selected artifacts, and supporting evidence.

## User Experience

Backup and restore wording must be nontechnical where possible. The system should explain what will be included and whether the result is a full backup or an archive export.
