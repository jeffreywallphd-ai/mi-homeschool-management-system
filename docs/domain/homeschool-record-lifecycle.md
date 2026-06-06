# Homeschool Record Lifecycle

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: lifecycle of homeschool records from planning to archive
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [Official Records Rules](official-records-rules.md), [Backup Restore and Export Architecture](../architecture/backup-restore-and-export-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Lifecycle

1. Plan: the parent creates courses, curriculum plans, resources, objectives, and intended requirement mappings.
2. Do: the student completes lessons, assignments, reading, projects, tests, fieldwork, external coursework, or practical work.
3. Capture: the system stores submissions, activity logs, files, photos, test records, notes, and portfolio artifacts.
4. Evaluate: the parent reviews evidence, grades work, writes feedback, records tests, or creates progress evaluations.
5. Award: the parent completes courses, awards credits, and updates graduation progress.
6. Issue: the parent generates report cards, transcripts, diplomas, course-description packets, portfolio exports, and graduation packets.
7. Archive: the system backs up source data and generated records for long-term family ownership.

## Source of Truth

Generated documents are outputs, not the only source of truth. Source records remain courses, grades, credit awards, completion records, portfolio artifacts, files, and graduation-plan decisions.

## Immutability Expectations

Generated official records should preserve issue dates, source references, and generated-file identity. If source data changes later, the system should generate a new version rather than silently changing an already issued document.

## Review Points

High-stakes lifecycle transitions require explicit parent action:

- Final grade recorded.
- Course completed.
- Credit awarded.
- Graduation requirement satisfied.
- Report card issued.
- Transcript issued.
- Diploma issued.
- Graduation packet generated.
