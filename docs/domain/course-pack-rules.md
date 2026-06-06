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
- One or more subject labels used for course records and requirement coverage mapping.
- Duration of one semester or two semesters.
- Planned credit value.
- Course description.
- Instructional methods.
- Major topics.
- Recommended texts and resources.
- Assessment methods.
- Grading basis.
- Curriculum plan fields: goals, learning objectives, major resources, planned sequence, and parent notes.
- Optional requirement mappings by view, area name, coverage level, and notes.
- Optional course choices for a template slot, with stable option ids and one default option.

Every selectable option must carry its own title, subject labels, duration, planned credit value, description details, curriculum plan fields, and requirement mappings. The default option is the option used for full-pack import unless the parent chooses another option in the UI.

Subject labels are part of the pack contract even when the pack UI does not display them. The import path must preserve those labels so imported courses can appear correctly in coverage summaries and course records.

Requirement mappings must use the exact requirement-area names and views from the pack's target jurisdiction seed. The default built-in pack targets the Michigan seed. Future state packs may be added, but the current UI should remain Michigan-focused.

## Import Rules

- Parent/admin authorization is required.
- Student sessions must not import packs.
- Imported courses become ordinary editable course records.
- The UI should support full-pack import and selected-course import.
- For templates with choices, the UI should use dropdowns and import the currently selected option.
- Re-importing the same pack skips courses already imported from the same template id.
- Missing requirement areas fail with visible errors instead of silently dropping mappings.
- Importing a built-in pack may refresh its target requirement seed before mapping so local data with older seed contents can be repaired safely.
- Pack data must not bypass domain course validation.
- Imported courses become editable course records regardless of whether they came from a fixed template or a selected option.
- Built-in pack updates may backfill blank imported-course detail fields, but must not overwrite parent-entered text.

## Installed Pack Terminology

Importing a pack means creating editable courses from a pack that is already available in the app.

Installing a pack is reserved for a later feature where the parent adds a pack created outside the built-in library.

## Default Pack Rule

The default Michigan pack should represent a transcript-recognizable planning starter aligned with common Michigan high-school credit categories and Michigan homeschool subject areas.

The pack must not claim that imported courses satisfy graduation requirements, legal requirements, college admission requirements, or transcript acceptance standards.

## Course Description Rule

Standard course titles are allowed, but each course must support a distinct course description so the parent can record the actual texts, methods, topics, assessments, and differentiating details.
