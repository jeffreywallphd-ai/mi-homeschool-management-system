# Assessment and Grading Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: grading, rubrics, tests, evaluations, and grade evidence
- Related ADRs: [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Credits and Graduation Rules](credits-and-graduation-rules.md), [Testing and Verification Standards](../standards/testing-and-verification-standards.md)
- Related tests: not yet implemented
- Supersedes: none

## Grading Model

The system must support mixed grading styles:

- Letter grades.
- Percentages.
- Rubrics.
- Pass/fail.
- Narrative evaluation.
- Test scores.

A standard letter/percentage scale may be seeded, but grading basis should be configurable per course.

## Evidence Requirement

Grades should be connected to evidence whenever practical:

- Assignment submissions.
- Rubric criteria.
- Test records.
- Parent evaluations.
- Portfolio artifacts.
- External course records.

## Gradebook Rules

- A grade belongs to a student and course context.
- A final grade should be explicit and parent-approved.
- GPA calculations must use a known grade scale.
- Missing grades must not silently convert to zero or pass.
- Null grade values must be represented as explicit states such as not graded, excused, incomplete, or not applicable.

## Progress Evaluations

Progress evaluations summarize learning over a period and may support report cards, portfolio exports, and graduation packets. They are not substitutes for source evidence when source evidence exists.
