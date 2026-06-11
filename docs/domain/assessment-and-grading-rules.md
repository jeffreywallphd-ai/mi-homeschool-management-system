# Assessment and Grading Rules

- Status: accepted
- Last reviewed: 2026-06-11
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

Parent/admin gradebook review may display files attached to student submissions so the parent can assess actual work. File viewing must use stored-file metadata and local file-storage contracts; gradebook viewing does not convert a file into evidence, a grade, a credit award, or an official-record input without explicit parent/admin action.

## Assignment Status

Assignment status is a planning and workflow state, not a grade. Planned points and planned weight may prepare an assignment for future grading, but they must not affect a course grade until the parent/admin explicitly records graded work under a defined grading basis.

Assignments may support later assessment records, rubric reviews, portfolio artifacts, or gradebook entries. Those later records must keep their own explicit parent/admin approval boundary.

Assignment attempt policy controls student submission workflow only. A single-attempt assignment may accept one reviewed attempt unless the parent/admin returns or clears active work. A multiple-attempt assignment may accept later reviewed attempts, but pending submitted work should not be duplicated while it is awaiting parent review.

Multi-draft assignment structure controls draft slots inside one larger assignment. A single-attempt multi-draft assignment may accept one reviewed submission per configured draft. Accepted draft work must not block later draft slots or the final draft. Multi-draft structure does not create grades, evidence, credits, completion status, report cards, transcripts, diplomas, or portfolio records without later explicit parent/admin action.

Clearing a submission removes it from the active review workflow and may reopen submission for the student. Clearing must retain the submission history and must not delete accepted evidence, assessment records, files, grades, credits, transcripts, report cards, diplomas, or portfolio records.

## Progress Evaluations

Progress evaluations summarize learning over a period and may support report cards, portfolio exports, and graduation packets. They are not substitutes for source evidence when source evidence exists.
