# ADR-0004: Local-First Parent PC Data Ownership

- Status: accepted
- Last reviewed: 2026-06-06
- Deciders: parent/project owner
- Technical story: Family homeschool data should remain local and parent-owned by default.
- Supersedes: none

## Context

The system stores sensitive student records, grades, files, portfolio artifacts, and official family-issued documents. The project vision favors parent ownership and local control.

## Decision

The initial system will be local-first and store family data separately from application binaries under the parent user's local application data folder.

## Consequences

- SQLite and local file storage are appropriate initial persistence choices.
- Backup, restore, and archive export are core product capabilities.
- Cloud sync, multi-device hosting, and external storage are deferred decisions.
- File paths and database records must be portable enough for backups and restores.

## Follow-Up

Implementation must define safe local paths, backup manifests, checksums, and restore validation.
