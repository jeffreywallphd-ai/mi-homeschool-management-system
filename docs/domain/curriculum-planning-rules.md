# Curriculum Planning Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: course-first curriculum planning behavior
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Requirement Mapping Rules](../legal-requirements/requirement-mapping-rules.md), [Domain Module Map](../architecture/domain-module-map.md)
- Related tests: not yet implemented
- Supersedes: none

## Planning Model

Planning is course-first. A parent creates courses, then optionally adds curriculum plans, lessons, objectives, resources, assignments, and requirement mappings.

Subject-first and requirement-first views may exist as filters or reports, but they must not replace the course as the primary high-school planning unit.

## Course Requirements

A high-school course should have:

- Title.
- Subject area.
- School year or term placement.
- Credit value or credit policy.
- Description status.
- Requirement mappings where applicable.

## Curriculum Plan

A curriculum plan may include:

- Goals or learning objectives.
- Major topics.
- Sequence of lessons.
- Texts and resources.
- Assignments and assessments.
- Parent notes.

## Requirement Mapping

Requirement mappings are separate from course identity. A course can support several requirement areas with coverage levels of primary, secondary, or supporting.

## Contract Rule

UI planning forms must submit complete command/view-model contracts. Domain code must not infer missing required planning values from nullable UI state.
