# Requirement Set Model

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: jurisdiction and requirement-area modeling
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Michigan Requirement Areas](michigan-requirement-areas.md), [Requirement Mapping Rules](requirement-mapping-rules.md)
- Related tests: not yet implemented
- Supersedes: none

## Requirement Set

A requirement set represents a jurisdictional or family-defined requirement profile.

Expected fields:

- Identifier.
- Jurisdiction.
- Legal or guidance basis.
- Effective date.
- Status.
- Notes.

## Requirement Area

A requirement area represents a subject, category, or coverage area.

Expected fields:

- Identifier.
- Requirement set identifier.
- Name.
- Description.
- Grade band.
- Required or recommended status.
- Source or note.

## Multiple Views

The model must support multiple related views:

- Legal/statutory coverage view.
- MDE guidance or course-of-study summary view.
- MMC transcript-planning reference view.
- Parent-added extensions to any of those views.

Seeded views may overlap, but duplicate aliases should not be shown as separate rows when statutory rows already represent the coverage. Parent-added rows are preserved during seed refresh and should appear in requirement mapping and coverage summary workflows.

## Jurisdiction Rule

Michigan should be seeded as the first jurisdiction. Future jurisdictions should be addable without rewriting course, transcript, portfolio, or document-generation modules.
