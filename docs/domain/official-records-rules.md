# Official Records Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: report cards, transcripts, diplomas, and official family-issued packets
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Records and Credentials Use Cases](../product/records-and-credentials-use-cases.md), [Legal Language Boundaries](../legal-requirements/legal-language-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## Official Family-Issued Records

Official records are family-issued artifacts generated from parent-owned source records. They include:

- Report cards.
- Transcripts.
- Diplomas.
- Course-description packets.
- Portfolio packets.
- Graduation packets.

## Report Card

A report card summarizes courses, grades, progress, credits where relevant, attendance/activity summaries when selected, and parent notes for a reporting period.

## Transcript

A transcript summarizes high-school course history, grade levels, school years, terms, credits attempted, credits earned, final grades, GPA, graduation date if applicable, and parent/school signature information.

## Diploma

A diploma is family-issued based on parent-defined graduation standards. Diploma generation must not imply state approval, accreditation, or MDE issuance.

## Course Description Packet

A course-description packet explains courses deeply enough to support colleges, employers, trade schools, transfer evaluators, or personal archives.

## Generated Record Versioning

Issued records should be versioned by issue date and generated file identity. Re-generating after source changes creates a new generated document record.
