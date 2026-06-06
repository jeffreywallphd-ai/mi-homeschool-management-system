# Document Generation Pack

Purpose: Rendering official records and packets from validated source models.

## Canonical Sources

- `docs/architecture/document-generation-architecture.md`
- `docs/domain/official-records-rules.md`
- `docs/legal-requirements/legal-language-boundaries.md`
- `docs/domain/credits-and-graduation-rules.md`

## Must Preserve

- Renderers receive complete document models.
- Renderers do not decide academic truth.
- Generated document records preserve issue date, template, source references, and file identity.

## Common Failure Modes

- Renderer queries arbitrary domain state and patches missing data.
- Generated wording implies state approval.
