# Security and Privacy Standards

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: student-record privacy and local-data safety
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Local Data and File Storage](../architecture/local-data-and-file-storage.md), [Data Retention Backup and Recovery Standards](data-retention-backup-and-recovery-standards.md)
- Related tests: not yet implemented
- Supersedes: none

## Privacy Posture

All student records, grades, files, portfolio artifacts, and generated documents are sensitive.

## Rules

- Start local-only unless a future accepted ADR authorizes external storage or sync.
- Do not log sensitive student content, grades, file contents, or private notes.
- Do not embed credentials or secrets.
- Treat generated transcripts, diplomas, and archives as sensitive files.
- Keep backup/export contents explicit and reviewable.

## Access

The parent is the primary authority. Student access, if implemented, must be limited and contract-backed.

## Future Security Decisions

Encryption at rest, authentication details, and multi-user access require explicit architecture review before implementation.
