# Curriculum Planning Rules

- Status: accepted
- Last reviewed: 2026-06-11
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

Courses, modules, and lessons may carry completion status for parent/admin progress tracking. Completion status is distinct from grading, evidence acceptance, credit awards, report cards, transcripts, and diploma readiness.

Module packs are course-level artifacts for moving a module shell between courses or systems. They may include module details and lightweight lesson or assignment sequencing references, but they must not embed lesson or assignment bodies.

Single-course course packs are course-level artifacts for moving the course detail-page shape plus module references. They must not embed module, lesson, or assignment bodies.

Course plan bundles are structured `.zip` artifacts for moving a complete course plan. They contain a course plan manifest, one single-course `.coursepack` per course, and module folders containing `.modulepack`, `.lessonpack`, and `.assignmentpack` files.

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
- Completion status for progress tracking.
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
- Lesson type, difficulty, estimated minutes, suggested days, tags, prerequisites, and subject-area planning metadata.
- Lesson-specific learning objectives that may break module objectives into smaller measurable targets.
- Optional parent-defined standards alignment.
- Student-facing success criteria.
- Ordered student workflow steps.
- One or more itemized resources with student instructions, note prompts, estimated time, source notes, and citation metadata.
- Problem sets or structured practice.
- Portfolio connections.
- Rubric or evaluation criteria.
- Reflection prompts.
- Parent/instructor notes.
- Optional links to module assignments.
- Completion status for progress tracking.

Lesson resources may be readings, textbook chapters, articles, videos, websites, files, or physical resources.

Expected answers, worked solutions, and parent/instructor notes are planning and evaluation support. They must not be shown in the student lesson view by default.

Lesson-to-assignment links are planning links. They help the student and parent see which assignment uses the lesson material, but they do not create completion evidence or grades by themselves.

Lesson packs are module-level artifacts. A `.lessonpack` may contain one or more lesson definitions and imports by appending lessons to the selected module, not by replacing existing module lessons.

Current `.lessonpack` files are JSON envelopes with a lesson array. Future lesson packs that include attached files should use a zip archive containing the `.lessonpack` JSON plus referenced files.

## Assignments

An assignment is module-owned work the student is expected to complete. It may link to one or more module objectives and one or more lessons.

An assignment may include:

- Title.
- Sequence order within the module.
- Assignment type.
- Instructional method profile.
- Short summary.
- Student-facing goal.
- Student-facing instructions.
- Estimated effort label and optional structured minute range.
- Due date or timing label.
- Linked module objectives.
- Linked lesson ids.
- Required output or evidence expectation.
- Required deliverables.
- Submission formats.
- Assignment-specific resources.
- Ordered assignment steps.
- Assessment skills.
- Student checklist.
- Portfolio connection.
- Rubric or linked rubric id.
- Revision policy.
- Completion criteria.
- Reflection prompts.
- Evidence retention requirements.
- Structured scoring plan.
- Parent notes.
- Portfolio-candidate marker.
- Planned points or planned weight.
- Status.
- Attempt policy for student submission workflow.
- Submission structure for single-submission or multi-draft workflows.
- Draft count when the assignment is intentionally built across lesson drafts.

Assignment status and planned points are planning fields. They must not create a grade, credit award, or evidence record without a later explicit parent/admin action.

Assignment rubrics, scoring plans, completion criteria, and evidence requirements are planning and review support. They must not create grades, completion evidence, or retained records without a later explicit parent/admin action.

Multi-draft assignments are still one module-owned assignment. Linked lessons may represent draft slots for that larger assignment. The final linked draft is treated as the final assignment submission for workflow labeling, but grades, evidence, credits, and completion still require later explicit parent/admin action.

Assignment packs are module-level artifacts. A `.assignmentpack` may contain one or more assignment definitions and imports by appending assignments to the selected module, not by replacing existing module assignments.

Current `.assignmentpack` files are JSON envelopes with an assignment array. Lesson links inside assignment packs should use lesson source ids and lesson titles so links can reconnect when matching lessons exist in the target module. Missing lesson links should not block import.

Future assignment packs that include attached files should use a zip archive containing the `.assignmentpack` JSON plus referenced files.

## Module Packs

A `.modulepack` may contain one module shell. It may include module title, description, term name, estimated length, instructions, module objectives, module resources, assignment/evidence placeholder, status, and lightweight lesson or assignment sequencing references.

Module packs must not include full lesson or assignment details. Lesson and assignment bodies belong in `.lessonpack` and `.assignmentpack` files.

Importing a module pack appends a new module to the selected course. It must not replace existing modules or create lesson or assignment bodies.

## Requirement Mapping

Requirement mappings are separate from course identity. A course can support several requirement areas with coverage levels of primary, secondary, or supporting.

## Course Packs

Single-course `.coursepack` files are importable course shells. Imported courses become editable parent-owned records.

Built-in course plans are importable templates. Imported courses should keep stable source plan/template identifiers so repeated built-in plan imports do not duplicate the same template course.

Course plan bundles should import courses, modules, lessons, and assignments from their folder structure. Requirement mappings that do not match the local jurisdiction seed should be skipped rather than blocking import.

Built-in course pack defaults may populate blank course description and curriculum plan fields for already imported courses. This migration-style backfill must preserve parent-entered text.

Built-in course pack defaults may add missing source-backed learning modules, lessons, or assignments to already imported courses, but must not overwrite parent-created or parent-edited modules, lessons, or assignments.

## Contract Rule

UI planning forms must submit complete command/view-model contracts. Domain code must not infer missing required planning values from nullable UI state.
