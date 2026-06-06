# Assessment Credits Graduation Pack

Purpose: Grades, GPA, tests, credit awards, course completion, and graduation plans.

## Canonical Sources

- `docs/domain/assessment-and-grading-rules.md`
- `docs/domain/credits-and-graduation-rules.md`
- `docs/adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md`
- `docs/standards/testing-and-verification-standards.md`

## Must Preserve

- Missing grades are explicit states, not zero.
- Credit awards are parent decisions.
- Diploma generation requires accepted parent-defined graduation standards.

## Common Failure Modes

- Awarding credit from course existence alone.
- Calculating GPA without a known grade scale.
