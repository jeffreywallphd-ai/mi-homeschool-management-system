# Records and Credentials Use Cases

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: user-facing official-record and credential workflows
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Official Records Rules](../domain/official-records-rules.md), [Document Generation Architecture](../architecture/document-generation-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Use Cases

The system must support these parent-facing record workflows:

- Generate a progress report for a date range.
- Generate a report card for a term, semester, or school year.
- Generate a high-school transcript.
- Generate a family-issued diploma.
- Generate a course-description packet.
- Generate a portfolio export.
- Generate a graduation packet.
- Export a student archive for long-term family records.

## Required Record Packet Types

- Transcript only.
- Transcript plus course descriptions.
- Full graduation packet.
- Portfolio packet.

## Transcript Preview and Export

The transcript workflow should let the parent/admin review a high-school-style transcript preview, record final transcript line values for each course, and export a transcript packet. Student access may show the transcript and course descriptions read-only.

Transcript packets should be able to include:

- A conventional transcript table grouped by school year and grade level.
- In-progress courses clearly labeled as in progress or planned.
- Final grades and earned credits only when explicitly parent-recorded.
- Course descriptions as an appendix or companion section.
- A source manifest that identifies course and transcript course record identifiers.
- A single PDF version of the packet for printing or sharing.
- A grade-span label and coverage note that describe only the grades represented by system course records.

## Required Graduation Packet Contents

A graduation packet should be able to include:

- Transcript.
- Diploma copy.
- Course descriptions.
- Graduation plan.
- Report cards.
- Test records.
- Portfolio index.
- Selected artifacts.
- Parent evaluation notes.

## Diploma Preview and Export

The diploma workflow should let the parent/admin record graduation readiness, preview a professional family-issued diploma, adjust diploma wording, adjust font family and size for each text element, and export a printable PDF. The workflow should remain blocked until parent-defined graduation standards are accepted and requirements are satisfied or explicitly waived by the parent.

The diploma preview and PDF should use a transparent page background for cardstock printing and should preserve a traditional diploma layout with a homeschool name, certification line, student name, completion statement, diploma title, rights and privileges line, awarded date, seal, signature line, and date line.

## Credential Language

Generated records must identify themselves as family-issued or parent-issued when appropriate. They must not imply state submission, MDE approval, accreditation, or legal certification.

## Evidence Chain

Official records should link back to source data:

- Transcript course lines should trace to course completions or credit awards.
- Diploma issuance should trace to an accepted graduation plan.
- Report card grades should trace to gradebook and evaluation evidence.
- Portfolio packets should trace to selected artifacts and evidence tags.
