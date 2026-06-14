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

## Transcript Packet Rendering

Transcript packet rendering should receive a complete transcript document model from the application layer. That model should already identify the student, school profile, span, school-year sections, course rows, transcript course records, warnings, course-description appendix data, and source identifiers.

Transcript renderers must preserve missing states. They may display "not recorded" for final grades or earned credits, but they must not infer grades, credits, GPA, or completion from planned course data, assignment status, or assessment records.

Transcript renderers must preserve grade-span truthfulness. A requested high-school or middle-school filter is not proof that the system contains the full conventional span. The rendered title, grade-span label, manifest, and PDF/HTML note should identify the actual grades represented by local course records and should note when other transcripts or records may exist for omitted grades.

Transcript packets may be generated as a ZIP archive containing `transcript.html`, `manifest.json`, and `manifest.md`, and may also offer a single PDF packet version for easier sharing or printing. The manifest should include source course identifiers and transcript course record identifiers when present. The PDF packet should preserve the same transcript, course-description appendix, record-note, and source-summary content in one styled file with readable identity, summary, and course-table sections.

## Diploma Rendering

Diploma rendering should receive a complete, validated diploma design model from the application layer. The application layer must verify parent/admin access, accepted graduation readiness, awarded date, signature labeling, and legal wording boundaries before passing the model to a renderer.

The diploma renderer should produce one landscape PDF suitable for printing on diploma cardstock. It must not paint a page background color. Borders, rule lines, corner ornamentation, seal text, signature line, date line, and diploma text are drawn over the transparent page surface.

Line-level typography from the diploma design should be preserved where portable PDF output allows it. When a chosen local font cannot be embedded, the renderer may map the requested font family to a PDF-safe base font while preserving font size, uppercase styling, and letter spacing.

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
