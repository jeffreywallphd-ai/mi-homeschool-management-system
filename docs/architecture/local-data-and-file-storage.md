# Local Data and File Storage

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: local data ownership, database, and file-storage shape
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [File and Artifact Taxonomy](../domain/file-and-artifact-taxonomy.md), [Backup Restore and Export Architecture](backup-restore-and-export-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Storage Root

Family data should be stored separately from application binaries. Desktop mode stores family data under the parent-user local application data folder:

```text
%LOCALAPPDATA%/HomeschoolManager
```

Optional background service mode stores family data under the computer-level application data folder:

```text
%PROGRAMDATA%/HomeschoolManager
```

Production installer and update flows must keep these folders outside the installed application binaries. Updating the application replaces the installed program files, not the family data folder.

## Data Layout

```text
data/
  homeschool.db
  homeschool.db-shm
  homeschool.db-wal

files/
  students/
  curriculum/
  generated-documents/

backups/
  automatic/
  manual/
  exports/

templates/
logs/
config/
secrets/
```

## Student File Categories

Student file storage should support:

- Profile.
- Submissions.
- Portfolio.
- Tests.
- Projects.
- Reading logs.
- Fieldwork.
- External courses.
- Dual enrollment.
- Official records.

## Storage Rules

- Database records store metadata and stable identifiers.
- Files are stored on disk using safe generated paths.
- Checksums should be recorded for stored files and backup manifests.
- Generated documents are stored separately from source submissions and portfolio artifacts.
- File paths should not be used as domain identity.
- Service mode must use one authoritative data root. It must not create separate active copies in parent and student Windows profiles.
- External-provider tokens or similar local secrets belong under `secrets/`, not in source-record backup folders. A restored installation may need the parent to reconnect the provider.

## Multi-Household Rule

V1 may optimize for one household, but storage and domain identity should not require a global singleton household.
