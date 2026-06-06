# Course Pack Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: importable course pack contracts and default pack behavior
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Curriculum Planning Rules](curriculum-planning-rules.md), [Requirement Mapping Rules](../legal-requirements/requirement-mapping-rules.md), [Michigan Requirement Areas](../legal-requirements/michigan-requirement-areas.md)
- Related tests: `HomeschoolManager.Tests`
- Supersedes: none

## Purpose

Course packs are importable planning templates. They help a parent start from recognizable high-school course structures without turning the imported records into state-issued, school-issued, or final academic determinations.

## Pack Contract

A course pack must define:

- Stable pack id.
- Display name.
- Plain-language description.
- Requirement jurisdiction/profile the pack is built against.
- Stable template id per course.
- Course title.
- Internal subject labels for course categorization.
- Duration of one semester or two semesters.
- Planned credit value.
- Course description.
- Instructional methods.
- Major topics.
- Recommended texts and resources as one item per line, using `Name | URL` when a viewable link is available.
- Assessment methods.
- Grading basis.
- Curriculum plan fields: goals, learning objectives, planned sequence, and parent notes.
- Learning module definitions with title, description, sequence order, optional term number, estimated length, instructions, itemized learning objectives, source-backed lessons, source-backed assignments, status, and assignment/evidence notes.
- Lesson definitions with title, sequence order, introductory text, optional linked module objective, and one or more itemized resources.
- Assignment definitions with stable source assignment id, sequence order, and method-profile variants.
- Assignment variant definitions with assignment type, method profile, title, instructions, estimated effort, timing label, linked module objectives, linked source lesson ids, required output, parent notes, portfolio-candidate marker, planned points or weight, and status.
- Optional requirement mappings by view, area name, coverage level, and notes.
- Optional course choices for a template slot, with stable option ids and one default option.

Every selectable option must carry its own title, internal subject labels, duration, planned credit value, description details, curriculum plan fields, learning modules, and requirement mappings. The default option is the option used for full-pack import unless the parent chooses another option in the UI.

Subject labels are internal support data and should not be the parent-facing requirement coverage mechanism. Coverage summaries and mapping workflows should use explicit requirement mappings.

Requirement mappings must use the exact requirement-area names and views from the pack's target jurisdiction seed. The default built-in pack targets the Michigan seed. Future state packs may be added, but the current UI should remain Michigan-focused.

Learning objectives must be stored as one objective per line. Each objective should complete the sentence "Upon completion of this course students will be able to..." without repeating that lead-in.

Learning modules must not include module goals or module major-topic fields. Module learning objectives are sufficient for module purpose.

Module learning objectives may optionally link to course learning objectives. Default-pack modules should include at least one linked objective per module and should support each course objective through at least two module objectives.

Lesson resources should be concrete readings, videos, files, or physical resources for the lesson. Course-level texts/resources remain syllabus-level materials.

Assignments should be concrete student work connected to module objectives. Default-pack assignments should link to relevant lesson source ids so student-facing assignments point back to the readings, videos, or resources the student should use.

Instructional methods, assessment methods, and grading basis may include a hybrid option that broadly combines common methods, evidence types, or grading bases.

## Import Rules

- Parent/admin authorization is required.
- Student sessions must not import packs.
- Imported courses become ordinary editable course records.
- The UI should support full-pack import and selected-course import.
- For templates with choices, the UI should use dropdowns and import the currently selected option.
- Re-importing the same pack skips courses already imported from the same template id.
- Missing requirement areas fail with visible errors instead of silently dropping mappings.
- Importing a built-in pack may refresh its target requirement seed before mapping so local data with older seed contents can be repaired safely.
- Built-in pack reads/imports should remove stale imported-course mappings to requirement rows no longer present in the current seed and add missing current pack mappings.
- Pack data must not bypass domain course validation.
- Imported courses become editable course records regardless of whether they came from a fixed template or a selected option.
- Built-in pack updates may backfill blank imported-course detail fields, but must not overwrite parent-entered text.
- Built-in pack updates may replace recognizable legacy built-in default text with the newer built-in default format.
- Built-in pack updates may add missing source-backed learning modules, lessons, or assignments to imported courses, but must not overwrite parent-created or parent-edited modules, lessons, or assignments.

## Installed Pack Terminology

Importing a pack means creating editable courses from a pack that is already available in the app.

Installing a pack is reserved for a later feature where the parent adds a pack created outside the built-in library.

## Export Rules

An exported course pack should use a `.coursepack` extension and contain JSON using a versioned envelope around the course pack contract.

The JSON envelope should include:

- Format identifier.
- Format version.
- Export timestamp.
- Package mode.
- Archive note.
- Pack payload.

Current built-in pack export may write a single JSON `.coursepack` file because built-in lesson and assignment resources are links or physical-resource references, not attached files.

Future exports that include attached lesson files, assignment files, or other course-pack files should use a zip archive containing the `.coursepack` JSON plus the referenced files. The JSON contract should remain the manifest inside that archive.

## Default Pack Rule

The default Michigan pack should represent a transcript-recognizable planning starter aligned with common Michigan high-school credit categories and Michigan homeschool subject areas.

The default Michigan pack should total 8 planned credits and keep history separate from government/civics/economics.

Default government/civics and U.S. history options should map U.S. Constitution and Michigan Constitution coverage from the Michigan seed.

Default-pack modules should include at least one assignment. Built-in assignments should include a hybrid variant and additional variants for common instructional method profiles so parent/admin users can adapt the work to the course method.

The pack must not claim that imported courses satisfy graduation requirements, legal requirements, college admission requirements, or transcript acceptance standards.

## Course Description Rule

Standard course titles are allowed, but each course must support a distinct course description so the parent can record the actual texts, methods, topics, assessments, and differentiating details.

Texts/resources and learning objectives should be edited as itemized lists in the UI even when their compact storage format remains newline-based.

Major resources should not be duplicated under curriculum plan when texts/resources already capture course materials.
