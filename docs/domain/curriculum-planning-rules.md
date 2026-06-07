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

Courses are student-owned records. Admin course lists, course creation, course-pack import, and coverage summaries must be scoped to the selected student when multiple homeschooled children are configured.

Courses may be archived. Archived courses remain retained records with modules, lessons, assignments, mappings, and future student work, but they are removed from active course lists and active coverage summaries.

Course deletion is for active planning records that do not have student work attached. If student work exists, deletion should fail for that course and direct the parent to archive it instead. Bulk delete actions may continue deleting other eligible courses.

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
- Module-owned assignments tied to objectives and, where useful, lessons.
- Status of planned, active, or complete.
- Assignment/evidence notes for later evidence workflows.

Learning modules must not include a separate goals field. Module learning objectives are the purpose statement for the module.

Module learning objectives may be module-specific or linked to course learning objectives. Course-pack defaults should ensure course objectives receive repeated support across modules.

Lessons contain the concrete readings, videos, files, or physical resources the student should use. Course-level resources describe the syllabus-level resource pool; lesson resources describe the specific student work.

Module learning objectives and lesson resources should remain itemized enough for assignments, evidence, and portfolio workflows.

## Lessons

A lesson may include:

- Title.
- Sequence order within the module.
- Introductory text for the student.
- Optional link to a module learning objective.
- One or more itemized resources.

Lesson resources may be readings, textbook chapters, articles, videos, websites, files, or physical resources.

## Assignments

An assignment is module-owned work the student is expected to complete. It may link to one or more module objectives and one or more lessons.

An assignment may include:

- Title.
- Sequence order within the module.
- Assignment type.
- Instructional method profile.
- Student-facing instructions.
- Estimated effort.
- Due date or timing label.
- Linked module objectives.
- Linked lesson ids.
- Required output or evidence expectation.
- Parent notes.
- Portfolio-candidate marker.
- Planned points or planned weight.
- Status.

Assignment status and planned points are planning fields. They must not create a grade, credit award, or evidence record without a later explicit parent/admin action.

## Requirement Mapping

Requirement mappings are separate from course identity. A course can support several requirement areas with coverage levels of primary, secondary, or supporting.

## Course Packs

Course packs are importable templates. Imported courses become editable parent-owned records and must keep stable source pack/template identifiers so repeated imports do not duplicate the same template course.

Built-in course pack defaults may populate blank course description and curriculum plan fields for already imported courses. This migration-style backfill must preserve parent-entered text.

Built-in course pack defaults may add missing source-backed learning modules, lessons, or assignments to already imported courses, but must not overwrite parent-created or parent-edited modules, lessons, or assignments.

## Contract Rule

UI planning forms must submit complete command/view-model contracts. Domain code must not infer missing required planning values from nullable UI state.
