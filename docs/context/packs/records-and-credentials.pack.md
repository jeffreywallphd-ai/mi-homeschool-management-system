# Records and Credentials Pack

Purpose: Report cards, transcripts, diplomas, official packets, and credential wording.

## Canonical Sources

- `docs/domain/official-records-rules.md`
- `docs/product/records-and-credentials-use-cases.md`
- `docs/legal-requirements/legal-language-boundaries.md`
- `docs/architecture/document-generation-architecture.md`

## Must Preserve

- Records are family-issued.
- Generated documents trace to source records.
- Transcript rows use explicit parent-recorded final grade and earned credit values.
- Student transcript access is read-only.
- Diploma generation depends on graduation-plan rules.

## Common Failure Modes

- Fabricating transcript lines from incomplete course data.
- Inferring earned credit, final grades, or GPA from planned courses, assignments, or assessments.
- Letting generated files become the only source of truth.
