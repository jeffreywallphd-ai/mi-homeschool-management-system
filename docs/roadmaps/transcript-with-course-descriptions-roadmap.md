# Transcript with Course Descriptions Roadmap

- Status: accepted
- Last reviewed: 2026-06-12
- Canonical for: transcript preview/export implementation slice
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Official Records Rules](../domain/official-records-rules.md), [Assessment and Grading Rules](../domain/assessment-and-grading-rules.md), [Credits and Graduation Rules](../domain/credits-and-graduation-rules.md), [Document Generation Architecture](../architecture/document-generation-architecture.md)
- Related tests: `src/HomeschoolManager.Tests/Program.cs`
- Supersedes: none

## Scope

Build an end-to-end transcript workflow that supports parent/admin review, explicit final course-line recording, transcript packet export, course-description appendix content, and student read-only access.

## Design Posture

The transcript should resemble a typical high school transcript: compact identity header, school/student information, yearly course tables, credit and grade columns, summary values, record notes, and signature space. Course descriptions are included after the transcript as supporting material because they are useful for homeschool records but are not usually part of the main transcript table.

The UI should follow the existing app design: regal purple navigation, card-based work surfaces, clear metrics, plain-language warnings, and responsive grids. Transcript tables may horizontally scroll on narrow screens to preserve record readability.

## Non-Scope

- Diploma generation.
- GPA calculation without a known grade scale.
- State submission, accreditation, or legal-compliance claims.
- Student editing of grades, credits, transcript records, or exports.

## Phases

1. Research transcript conventions and repository rules.
2. Add transcript source model and parent-recorded course-line contract.
3. Build transcript preview and export service.
4. Add parent/admin transcript page with course-line editor and packet download.
5. Add student read-only transcript page.
6. Include course-description appendix content.
7. Add tests for permissions, missing states, span filtering, and export contents.
8. Update canonical docs and verify builds.

## Exit Criteria

- Parent/admin can preview high school, middle school, and all-course transcript spans.
- Parent/admin can record final grade, earned credit, completion date, basis, notes, and inclusion per course.
- Student can view the transcript but cannot edit or export it.
- In-progress and missing final records are plainly labeled.
- Transcript packet includes `transcript.html`, `manifest.json`, and `manifest.md`, with a single PDF packet download available for printing or sharing.
- Tests cover no inferred grades/credits, student permission boundaries, span separation, and prohibited wording.
