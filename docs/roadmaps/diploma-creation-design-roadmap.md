# Diploma Creation and Design Roadmap

- Status: accepted
- Last reviewed: 2026-06-13
- Canonical for: diploma creation and design implementation sequencing
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Official Records Rules](../domain/official-records-rules.md), [Credits and Graduation Rules](../domain/credits-and-graduation-rules.md), [Document Generation Architecture](../architecture/document-generation-architecture.md), [Legal Language Boundaries](../legal-requirements/legal-language-boundaries.md)
- Related tests: `src/HomeschoolManager.Tests/Program.cs`
- Supersedes: none

## Scope

Build an end-to-end parent/admin diploma workflow that records graduation readiness, previews a professional family-issued diploma, lets the administrator adjust wording and typography, and exports a printable PDF.

## Non-Scope

- State filing or MDE submission.
- Accreditation, state approval, or legal-compliance claims.
- Student editing of diplomas, graduation plans, grades, credits, or official records.
- Graduation packet bundling beyond producing the diploma PDF.

## Design Direction

The diploma should resemble a traditional high school diploma while preserving the family-issued record posture:

- Landscape letter-size layout suitable for diploma cardstock.
- Transparent page background so cardstock color is not overwritten.
- Nested black and gold borders.
- Decorative corner flourishes and centered rule lines.
- Top line: homeschool name.
- Certification line: "This certifies that".
- Prominent student name.
- Completion statement that references parent-defined coursework and selected Michigan subject-area records without implying state approval or legal certification.
- Prominent "High School Diploma" title.
- Rights and privileges line.
- Awarded date line.
- Parent/admin signature and date lines.
- Center seal with family-issued wording.

Typography controls should support each diploma text element separately:

- Font family.
- Font size.
- Uppercase styling.
- Letter spacing.

The browser may offer locally installed font families when available. If local font access is unavailable, the UI should fall back to built-in common serif, sans-serif, and monospace choices.

## Phases

### Phase 1: Research and Boundaries

- Review legal-language and graduation-plan guardrails.
- Refresh official Michigan source posture before diploma wording changes.
- Confirm no diploma generation happens without accepted parent-defined standards.

Exit criteria:

- Source review log is updated.
- Diploma wording avoids state approval, accreditation, compliance, and MDE issuance claims.

### Phase 2: Domain and Application Contracts

- Add parent-owned graduation readiness record.
- Add diploma design record with line-level typography.
- Add repository contracts and persistence for one current graduation plan and diploma design per student.
- Add application service commands for readiness, design save, and PDF export.

Exit criteria:

- Parent/admin can save readiness and design through application contracts.
- Student role is blocked from diploma export or mutation.
- PDF export is blocked until readiness is complete.

### Phase 3: Rendering

- Add a structured PDF renderer that receives the validated diploma design model.
- Preserve transparent page background.
- Render borders, rules, corner flourishes, text lines, seal, signature line, and date line.
- Map requested font families to PDF-safe base fonts for portable output.

Exit criteria:

- PDF exports as one printable file.
- Saved font size and spacing are visible in the PDF output.

### Phase 4: Parent/Admin UI

- Add a Diploma admin page and navigation entry.
- Show a live preview using the same visual hierarchy as the PDF.
- Include readiness, diploma text, and typography editors in restrained dashboard styling.
- Disable export until readiness is complete.
- Load local font families when the browser permits.

Exit criteria:

- Parent/admin can review, edit, save, and export from one screen.
- Background remains transparent in the preview.

### Phase 5: Documentation and Tests

- Update canonical docs and compact context packs.
- Add tests for readiness blocking, typography persistence, transparent PDF output, legal wording rejection, and student access restrictions.

Exit criteria:

- Tests cover domain/application behavior and generated PDF basics.
- Docs reflect the implemented workflow and boundaries.
