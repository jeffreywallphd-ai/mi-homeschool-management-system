# Domain Module Map

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: domain module inventory and ownership boundaries
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [Modular Monolith Boundaries](modular-monolith-boundaries.md), [Glossary](../domain/glossary.md)
- Related tests: not yet implemented
- Supersedes: none

## Core Modules

| Module | Responsibility |
| --- | --- |
| Household | Household, parent/legal guardian, school profile |
| Students | Student identity, enrollment, school years, grade levels |
| LegalRequirements | Jurisdictions, requirement sets, requirement areas, mappings, legal notes |
| Curriculum | Subjects, course packs, courses, descriptions, plans, resources, lessons, objectives |
| InstructionRecords | Instruction sessions, activity logs, attendance/activity summaries, reading, projects, fieldwork |
| Assignments | Assignments, attachments, submissions, submission files, resubmissions |
| Assessment | Assessments, rubrics, grades, grade scales, tests, progress evaluations |
| Credits | Credit policies, credit awards, course completions, graduation plans, graduation progress |
| Records | Report cards, transcripts, transcript lines, diplomas, generated records, release/packet concepts |
| Portfolio | Artifacts, collections, evidence tags |
| Files | Stored files, file categories, checksums |

## Supporting Application/Infrastructure Modules

| Module | Responsibility |
| --- | --- |
| Documents | Render report cards, transcripts, diplomas, course-description packets, portfolio exports |
| Backups | Backup, restore, manifest, archive export |
| Auth | Local parent access and future student access boundaries |

## Module Rules

- Records depends on completed source records; it must not fabricate course, grade, credit, or graduation data.
- LegalRequirements provides requirement areas and mappings; it must not certify compliance.
- Credits owns graduation-plan satisfaction; Records consumes that state for diploma generation.
- Portfolio owns evidence context; Files owns stored binary/file metadata.
- Documents renders records; it does not decide academic truth.
