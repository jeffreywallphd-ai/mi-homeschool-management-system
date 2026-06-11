# Assessment and Grading Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: grading, rubrics, tests, evaluations, and grade evidence
- Related ADRs: [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Credits and Graduation Rules](credits-and-graduation-rules.md), [Testing and Verification Standards](../standards/testing-and-verification-standards.md)
- Related tests: `src/HomeschoolManager.Tests/Program.cs`
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

## Assessment Records

Assessment records are parent-owned records of reviewed work or parent evaluation. They may be linked to:

- Assignment plans.
- Student submissions.
- Evidence records.
- Course context.
- Parent evaluations or later test/portfolio records.

Assessment records may store narrative, rubric summary, pass/fail, points, percentage, letter grade, test score, or explicit not-graded results. These records are not final course grades by themselves. They must not award credit, mark a course complete, calculate GPA, generate report cards, generate transcripts, or determine diploma readiness without a later explicit parent/admin workflow.

Student-visible feedback is controlled separately from parent/admin notes. Student-facing pages may show only feedback that the parent/admin has marked visible to the student.

## Assignment Status

Assignment status is a planning and workflow state, not a grade. Planned points and planned weight may prepare an assignment for future grading, but they must not affect a course grade until the parent/admin explicitly records graded work under a defined grading basis.

Assignments may support later assessment records, rubric reviews, portfolio artifacts, or gradebook entries. Those later records must keep their own explicit parent/admin approval boundary.

## Progress Evaluations

Progress evaluations summarize learning over a period and may support report cards, portfolio exports, and graduation packets. They are not substitutes for source evidence when source evidence exists.
