# Local Data and File Storage

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: local data ownership, database, and file-storage shape
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [File and Artifact Taxonomy](../domain/file-and-artifact-taxonomy.md), [Backup Restore and Export Architecture](backup-restore-and-export-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Storage Root

Family data should be stored separately from application binaries under a parent-user local application data folder, initially:

```text
%LOCALAPPDATA%/HomeschoolManager
```

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

## Multi-Household Rule

V1 may optimize for one household, but storage and domain identity should not require a global singleton household.
