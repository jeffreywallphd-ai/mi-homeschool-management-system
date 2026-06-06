# Document Generation Architecture

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: report card, transcript, diploma, course-description, portfolio, and packet rendering
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Official Records Rules](../domain/official-records-rules.md), [Legal Language Boundaries](../legal-requirements/legal-language-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## Rendering Model

Document generation should be template-driven. The first implementation should support an HTML or structured preview plus PDF output.

Generated documents should be created from validated source records, not from loosely assembled UI state.

## Document Types

- Progress report.
- Report card.
- Transcript.
- Diploma.
- Course-description packet.
- Portfolio export.
- Graduation packet.

## Source Contracts

Each renderer should receive a complete document model:

- Student identity and school profile fields.
- Date/term/year context.
- Source course, grade, credit, and artifact summaries.
- Required signature or issue fields.
- Legal-boundary wording.

Renderers must not query arbitrary domain state or patch missing data during rendering.

## Generated Document Record

Each generated document should record:

- Document type.
- Student.
- Issue/generated date.
- Source record identifiers or snapshot reference.
- File identifier.
- Template identifier/version.

## Wording Boundary

Generated documents must preserve family-issued wording and must not imply state approval, accreditation, or MDE issuance.
