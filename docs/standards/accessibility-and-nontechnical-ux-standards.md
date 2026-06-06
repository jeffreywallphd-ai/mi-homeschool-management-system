# Accessibility and Nontechnical UX Standards

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: parent-first, student-capable, nontechnical UI expectations
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [User and Household Model](../product/user-and-household-model.md), [Modular Monolith Boundaries](../architecture/modular-monolith-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## UX Posture

Design parent-first and student-capable. The parent should be able to operate the system without technical knowledge.

## Rules

- Use plain language.
- Keep workflows clear and low clutter.
- Make record status visible.
- Make high-stakes actions explicit.
- Show validation errors near the affected field.
- Mark required fields with an asterisk.
- Do not hide legal-boundary wording when generating official records.
- Preserve keyboard accessibility and semantic UI structure.

## Contract-Backed UI

UI components must submit explicit view models or commands. A screen should not depend on partially initialized domain objects or raw persistence models.

Required-field validation should run in the UI before command submission when the missing field can be identified locally. Domain validation remains the final boundary.

## Workbench Layout

For form-heavy planning screens, place the main form and primary records on the left side of the content body. Place related support tools, import panels, previews, summaries, and selection helpers on the right side. On smaller screens, the support area should collapse below the main workflow.

## Student Workflows

Student-facing screens should focus on courses, assignments, submissions, feedback, and portfolio review. Parent-owned finalization remains separate.
