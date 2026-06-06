# Curriculum Planning Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: course-first curriculum planning behavior
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Course Pack Rules](course-pack-rules.md), [Requirement Mapping Rules](../legal-requirements/requirement-mapping-rules.md), [Domain Module Map](../architecture/domain-module-map.md)
- Related tests: not yet implemented
- Supersedes: none

## Planning Model

Planning is course-first. A parent creates courses, then optionally adds curriculum plans, lessons, objectives, resources, assignments, and requirement mappings.

Subject-first and requirement-first views may exist as filters or reports, but they must not replace the course as the primary high-school planning unit.

Learning modules are course-owned instructional units inside a course. They organize the course into teachable topic arcs while the course remains the transcript-facing record. Lessons inside modules provide the concrete instructional steps and resources.

## Course Requirements

A high-school course should have:

- Title.
- One or more subject labels.
- One-semester or two-semester duration.
- School year or term placement.
- Credit value or credit policy.
- Description status.
- Requirement mappings where applicable.

Subject labels are transcript-friendly descriptors. Requirement mappings remain the explicit source-backed coverage model.

## Curriculum Plan

A curriculum plan may include:

- Goals or learning objectives.
- Major topics.
- Sequence of lessons.
- Assignments and assessments.
- Parent notes.

Texts and resources belong in the course description/resources section and should not be duplicated as major resources in the curriculum plan.

## Learning Modules

A learning module may include:

- Title.
- Description.
- Sequence order.
- Optional term or semester placement.
- Estimated length.
- Instructions.
- Itemized learning objectives.
- Optional alignment from a module objective to a course learning objective.
- Lessons tied to module learning objectives.
- Status of planned, active, or complete.
- Assignment/evidence placeholder.

Learning modules must not include a separate goals field. Module learning objectives are the purpose statement for the module.

Module learning objectives may be module-specific or linked to course learning objectives. Course-pack defaults should ensure course objectives receive repeated support across modules.

Lessons contain the concrete readings, videos, files, or physical resources the student should use. Course-level resources describe the syllabus-level resource pool; lesson resources describe the specific student work.

Module learning objectives and lesson resources should remain itemized enough for later assignment, evidence, and portfolio workflows.

## Lessons

A lesson may include:

- Title.
- Sequence order within the module.
- Introductory text for the student.
- Optional link to a module learning objective.
- One or more itemized resources.

Lesson resources may be readings, textbook chapters, articles, videos, websites, files, or physical resources.

## Requirement Mapping

Requirement mappings are separate from course identity. A course can support several requirement areas with coverage levels of primary, secondary, or supporting.

## Course Packs

Course packs are importable templates. Imported courses become editable parent-owned records and must keep stable source pack/template identifiers so repeated imports do not duplicate the same template course.

Built-in course pack defaults may populate blank course description and curriculum plan fields for already imported courses. This migration-style backfill must preserve parent-entered text.

Built-in course pack defaults may add missing source-backed learning modules or lessons to already imported courses, but must not overwrite parent-created or parent-edited modules or lessons.

## Contract Rule

UI planning forms must submit complete command/view-model contracts. Domain code must not infer missing required planning values from nullable UI state.
