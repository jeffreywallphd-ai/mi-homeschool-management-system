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
- Do not hide legal-boundary wording when generating official records.
- Preserve keyboard accessibility and semantic UI structure.

## Contract-Backed UI

UI components must submit explicit view models or commands. A screen should not depend on partially initialized domain objects or raw persistence models.

## Student Workflows

Student-facing screens should focus on courses, assignments, submissions, feedback, and portfolio review. Parent-owned finalization remains separate.
