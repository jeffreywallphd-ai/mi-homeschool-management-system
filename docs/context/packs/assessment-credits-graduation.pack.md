# Assessment Credits Graduation Pack

Purpose: Grades, GPA, tests, credit awards, course completion, and graduation plans.

## Canonical Sources

- `docs/domain/assessment-and-grading-rules.md`
- `docs/domain/credits-and-graduation-rules.md`
- `docs/adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md`
- `docs/standards/testing-and-verification-standards.md`

## Must Preserve

- Missing grades are explicit states, not zero.
- Assessment records are parent-owned review records, not final course grades by themselves.
- Assignment status and planned points do not create grades.
- Assignment status may prepare later evidence or grading workflows only after explicit parent/admin action.
- Student-facing feedback must be explicitly marked visible by the parent/admin.
- Credit awards are parent decisions.
- Diploma generation requires accepted parent-defined graduation standards.

## Common Failure Modes

- Awarding credit from course existence alone.
- Inferring grades from assignment status, planned points, or planned weight.
- Calculating GPA without a known grade scale.
