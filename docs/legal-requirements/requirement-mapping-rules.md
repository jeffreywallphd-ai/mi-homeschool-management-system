# Requirement Mapping Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: mapping courses and evidence to requirement areas
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Curriculum Planning Rules](../domain/curriculum-planning-rules.md), [Michigan Requirement Areas](michigan-requirement-areas.md)
- Related tests: not yet implemented
- Supersedes: none

## Mapping Purpose

Requirement mappings show how parent-owned records cover selected subject areas. They are evidence summaries, not legal determinations.

## Coverage Levels

Use these coverage levels:

- Primary: the course or artifact is mainly designed to cover the area.
- Secondary: the area is meaningfully addressed but not the main focus.
- Supporting: the area is incidentally or practically supported.

## Course Mapping Examples

Homestead Biology and Soil Science:

- Science: primary.
- Writing: secondary.
- Mathematics: supporting.

Farm Business Accounting:

- Mathematics: primary.
- Writing: secondary.
- Civics/Economics/Social Studies: supporting when the course includes those topics.

## Mapping Rules

- A course may map to multiple areas.
- A course may also carry multiple subject labels.
- A requirement area may be covered by multiple courses.
- Coverage is parent-selected and should be editable.
- Generated coverage summaries should say "records show coverage" rather than "compliant."
- Missing mappings must be visible, not silently inferred.
- Parent-facing coverage summaries may group matching area names across source views and combine source labels while preserving source traceability.
