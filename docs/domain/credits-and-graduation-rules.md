# Credits and Graduation Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: credit awards, course completion, graduation plans, and diploma readiness
- Related ADRs: [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Official Records Rules](official-records-rules.md), [Records and Credentials Use Cases](../product/records-and-credentials-use-cases.md)
- Related tests: not yet implemented
- Supersedes: none

## Graduation Plan

The graduation plan is parent-defined. The system may seed a conventional high-school-style template, but the parent must accept or edit the internal graduation standard before diploma generation.

## Recommended Seed Categories

- English / Language Arts.
- Mathematics.
- Science.
- Social Studies.
- Civics / Government.
- Practical Arts.
- Fine Arts.
- Physical Education / Health.
- Electives.
- Parent-defined.

Categories must be editable.

## Credit Award

A credit award is an explicit parent decision. It should identify:

- Student.
- Course.
- Credit amount.
- Date awarded.
- Basis for award.
- Source course completion.
- Parent notes when needed.

## Course Completion

Course completion is distinct from credit award. A course may be completed before credit is finalized, and a parent may need to resolve final grade, evidence, or credit amount before award.

## Diploma Readiness

The system must not generate a diploma unless:

- A graduation plan exists.
- Parent-defined standards are accepted.
- Required graduation categories are satisfied or explicitly waived by the parent.
- Graduation date and issue data are provided.
- Parent signature name is provided.

## GPA

GPA calculations must be deterministic, traceable to grade scales, and able to exclude pass/fail or non-GPA courses when the grade scale requires it.
