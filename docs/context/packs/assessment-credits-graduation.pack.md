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
- Assignment attempt policy controls student submission workflow only; it does not create grades, evidence, credits, or completion records.
- Multi-draft assignment structure controls draft submission slots only; it does not create grades, evidence, credits, or completion records.
- Accepted work for one draft must not block later draft slots in the same multi-draft assignment.
- Clearing a submission retains history and must not delete accepted evidence, assessment records, files, or official-record inputs.
- Student-facing feedback must be explicitly marked visible by the parent/admin.
- Credit awards are parent decisions.
- Course/module/lesson completion status is progress tracking only unless a later parent/admin credit workflow explicitly uses reviewed evidence.
- Diploma generation requires accepted parent-defined graduation standards.

## Common Failure Modes

- Awarding credit from course existence alone.
- Inferring grades from assignment status, planned points, or planned weight.
- Calculating GPA without a known grade scale.
