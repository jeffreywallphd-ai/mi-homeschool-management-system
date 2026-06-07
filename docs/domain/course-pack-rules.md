# Course Pack Rules

- Status: accepted
- Last reviewed: 2026-06-07
- Canonical for: importable course pack contracts and default pack behavior
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Curriculum Planning Rules](curriculum-planning-rules.md), [Requirement Mapping Rules](../legal-requirements/requirement-mapping-rules.md), [Michigan Requirement Areas](../legal-requirements/michigan-requirement-areas.md)
- Related tests: `HomeschoolManager.Tests`
- Supersedes: none

## Purpose

Course planning packs are importable planning templates. They help a parent start from recognizable high-school course structures without turning the imported records into state-issued, school-issued, or final academic determinations.

## Course Pack Contract

A `.coursepack` is a single-course JSON artifact. It must define:

- Format identifier `homeschool-manager.coursepack`.
- Format version.
- Download timestamp.
- Package mode and archive note.
- Source identity metadata with publisher id, pack id, pack version, and source namespace.
- Stable source course id.
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
- Module references with stable source module id, title, sequence order, and term name.
- Optional requirement mappings by view, area name, coverage level, and notes.

A `.coursepack` must not embed module, lesson, or assignment bodies. Module bodies belong in `.modulepack`; lesson bodies belong in `.lessonpack`; assignment bodies belong in `.assignmentpack`.

The built-in default course plan may keep internal template choices and defaults so the UI can offer dropdown selections before import. Every selectable option must carry its own title, internal subject labels, duration, planned credit value, description details, curriculum plan fields, learning modules, and requirement mappings.

Subject labels are internal support data and should not be the parent-facing requirement coverage mechanism. Coverage summaries and mapping workflows should use explicit requirement mappings.

Requirement mappings must use the exact requirement-area names and views from the pack's target jurisdiction seed. The default built-in pack targets the Michigan seed. Future state packs may be added, but the current UI should remain Michigan-focused.

Learning objectives must be stored as one objective per line. Each objective should complete the sentence "Upon completion of this course students will be able to..." without repeating that lead-in.

Learning modules must not include module goals or module major-topic fields. Module learning objectives are sufficient for module purpose.

Module learning objectives may optionally link to course learning objectives. Default-pack modules should include at least one linked objective per module and should support each course objective through at least two module objectives.

Lesson resources should be concrete readings, videos, files, or physical resources for the lesson. Course-level texts/resources remain syllabus-level materials.

Assignments should be concrete student work connected to module objectives. Default-pack assignments should link to relevant lesson source ids so student-facing assignments point back to the readings, videos, or resources the student should use.

Instructional methods, assessment methods, and grading basis may include a hybrid option that broadly combines common methods, evidence types, or grading bases.

## Course Plan Pack Contract

A `.courseplanpack` is a JSON manifest for a set of course offerings. It should include:

- Format identifier `homeschool-manager.courseplanpack`.
- Format version.
- Download timestamp.
- Package mode and archive note.
- Source identity metadata with publisher id, pack id, pack version, and source namespace.
- Stable plan id.
- Plan name and description.
- Pacing label such as Year, Semester, or Term.
- Course offerings with source course id, course title, term name, and sequence order.

The plan pack says which courses are offered in which time period. It does not contain course detail bodies; those live in the course folders of a course plan bundle.

## Course Plan Bundle Contract

A course plan bundle is the preferred full-plan import/export artifact. It is a `.zip` archive with this structure:

- `courseplan.courseplanpack`.
- `courses/{course-folder}/course.coursepack`.
- `courses/{course-folder}/modules/{module-folder}/module.modulepack`.
- `courses/{course-folder}/modules/{module-folder}/lessons.lessonpack`.
- `courses/{course-folder}/modules/{module-folder}/assignments.assignmentpack`.

Bundle JSON files should be written as UTF-8 without a byte order marker.

Importing a course plan bundle creates student-owned courses, then appends modules, lessons, and assignments from the folder structure. Requirement mappings that do not match the local seed must not block import.

Importing a newer zip version of the same course plan should update by stable source ids instead of duplicating courses. Existing course, module, lesson, and assignment records with matching source ids should be preserved; missing new pack-owned items may be added.

## External and Internal Identity Rules

Pack ids are external references only. The app must always create internal GUIDs for stored courses, modules, lessons, assignments, resources, mappings, and future student work.

External source ids are allowed because packs need to link elements together. They should be matched with the pack's source identity, item type, and source id, not source id alone.

Pack source identity should include:

- Publisher id.
- Pack id.
- Pack version.
- Source namespace.

The source namespace is the preferred durable key for update matching. The app stores this namespace as the course source pack key for imported plan bundles.

Blank or example source ids from externally created packs should not become internal ids. Import may repair blank or sample ids into stable title/sequence-based source ids so the parent can still use the pack, but downloaded non-template packs should provide deliberate source ids.

Template packs may include sample ids when `isTemplate` is true.

## Lesson Pack Contract

A lesson pack is a smaller module-level import/export artifact, not a course pack. A `.lessonpack` file should use a JSON envelope with:

- Format identifier `homeschool-manager.lessonpack`.
- Format version.
- Download timestamp.
- Package mode.
- Archive note.
- Pack name and description.
- One or more lessons.

Each lesson should include stable source lesson id, sequence order, title, introductory text, optional linked module objective, lesson type, estimated minutes, suggested days, difficulty level, subject areas, tags, prerequisites, lesson-specific objectives, optional standards alignment, success criteria, ordered workflow steps, one or more resources, optional problem sets, portfolio connections, rubric criteria, reflection prompts, instructor notes, and optional assignment links.

Each resource should include name, resource type, URL, file path, physical-resource marker, source note, required marker, estimated minutes, student instructions, notes prompt, optional citation metadata, offline marker, and license note.

Problem sets may store expected answers and worked solutions for parent review. Student-facing views must hide answer keys and instructor notes by default.

Supported lesson workflow step types include reading, video, notes, discussion, practice, problem set, lab or simulation, portfolio artifact, reflection, parent conference, planning, and research. Supported lesson resource types include reading, textbook chapter, article, video, website, file, physical resource, and data source.

Supported lesson problem response types include short answer, worked solution, essay, diagram, spreadsheet, oral explanation, written explanation, and graph with written analysis.

Assignment links inside lesson packs should use linked assignment source ids and linked assignment titles so links can reconnect when matching assignments exist in the target module. Missing assignment links should not block lesson import.

Importing a lesson pack appends lessons to the selected module. It must not replace existing module lessons or create courses. Parent/admin authorization is required.

Current lesson pack download may write a single JSON `.lessonpack` file. Future lesson packs with uploaded files should use a zip archive containing the `.lessonpack` JSON plus referenced files.

## Module Pack Contract

A module pack is a smaller course-level import/export artifact, not a course pack. A `.modulepack` file should use a JSON envelope with:

- Format identifier `homeschool-manager.modulepack`.
- Format version.
- Download timestamp.
- Package mode.
- Archive note.
- Pack name and description.
- Exactly one module shell.

The module shell should include stable source module id, sequence order, title, description, term name, estimated length, instructions, module learning objectives with optional linked course objectives, module resources, assignment/evidence placeholder, status, lesson sequencing references, and assignment sequencing references.

Lesson and assignment sequencing references should include source id, title, and sequence order only. Module packs must not embed lesson bodies or assignment bodies; those belong in `.lessonpack` and `.assignmentpack` artifacts.

Importing a module pack appends a module to the selected course. It must not replace existing modules, create courses, or create lesson or assignment bodies. Parent/admin authorization is required.

Current module pack download may write a single JSON `.modulepack` file. Future module packs with uploaded files should use a zip archive containing the `.modulepack` JSON plus referenced files.

## Assignment Pack Contract

An assignment pack is a smaller module-level import/export artifact, not a course pack. A `.assignmentpack` file should use a JSON envelope with:

- Format identifier `homeschool-manager.assignmentpack`.
- Format version.
- Download timestamp.
- Package mode.
- Archive note.
- Pack name and description.
- One or more assignments.

Each assignment should include stable source assignment id, sequence order, title, assignment type, instructional method profile, summary, student-facing goal, instructions, estimated effort label, optional minimum and maximum minutes, timing label, optional due date, linked module objectives, linked lesson source ids, linked lesson titles, required output, required deliverables, submission formats, optional assignment resources, ordered assignment steps, assessment skills, student checklist, portfolio connection, embedded rubric or linked rubric id, revision policy, completion criteria, reflection prompts, evidence requirements, structured scoring plan, parent notes, portfolio-candidate marker, planned points or weight, and status.

Assignment resources are assignment-specific supports. They may supplement lesson resources but must not replace lesson links when the assignment depends on particular lessons.

Assignment rubrics and scoring plans are planning support. They do not create grades until the parent explicitly records a grade or evaluation in a later workflow.

Supported assignment submission formats include written response, worked solutions, spreadsheet, graph, data table, field notes, photo evidence, portfolio entry, decision memo, budget, reflection, presentation, practical demonstration, written memo, written analysis, optional spreadsheet, and optional graph.

Importing an assignment pack appends assignments to the selected module. It must not replace existing module assignments or create courses. Parent/admin authorization is required.

Lesson links should reconnect by lesson source id or lesson title when the target module contains matching lessons. Missing lesson links should not block assignment import because the parent can correct links after import.

Current assignment pack download may write a single JSON `.assignmentpack` file. Future assignment packs with uploaded files should use a zip archive containing the `.assignmentpack` JSON plus referenced files.

## Import Rules

- Parent/admin authorization is required.
- Student sessions must not import packs.
- Importing creates ordinary editable course records in the student's course list from an available course plan, course plan bundle, or single-course coursepack.
- Importing is always scoped to the selected student; the same pack template may be imported independently for different students.
- The UI should support full-plan import and selected-course import for built-in plans.
- For templates with choices, the UI should use dropdowns and import the currently selected option.
- Re-importing the same pack skips courses already imported from the same template id.
- Requirement mappings that match the current jurisdiction seed should import.
- Requirement mappings that do not match the current jurisdiction seed should not block import; leave those mappings off so the parent can review and correct coverage after import.
- Importing a built-in pack may refresh its target requirement seed before mapping so local data with older seed contents can be repaired safely.
- Built-in pack reads/imports should remove stale imported-course mappings to requirement rows no longer present in the current seed and add missing current pack mappings.
- Pack data must not bypass domain course validation.
- Imported courses become editable course records regardless of whether they came from a fixed template or a selected option.
- Built-in pack updates may backfill blank imported-course detail fields, but must not overwrite parent-entered text.
- Built-in pack updates may replace recognizable legacy built-in default text with the newer built-in default format.
- Built-in pack updates may add missing source-backed learning modules, lessons, or assignments to imported courses, but must not overwrite parent-created or parent-edited modules, lessons, or assignments.
- Course plan zip re-imports should use stable plan, course, module, lesson, and assignment source ids to avoid duplicate active records when a newer version of a plan is imported.
- External ids must never be trusted as internal storage ids. They are source references used to build an import/update mapping into app-owned GUIDs.

## Installed Pack Terminology

Importing a plan means creating editable courses from a plan that is already available in the app.

Installing a legacy multi-course `.coursepack` means adding it to the system's available course plan library. Installing must not create student courses.

Installed legacy packs may appear alongside built-in plans in the Course plan selector. The parent/admin must still choose Import full plan or Import selected to copy courses into the student's course list.

Installed plans are household/system-level templates. Imported courses are student-owned records.

Installing a pack with the same id as a built-in pack should be rejected so built-in packs are not shadowed by uploaded files.

## Download Rules

Downloaded single-course course packs should use `.coursepack` and contain JSON using the versioned single-course contract.

Downloaded full plans should use `.zip` and contain the structured course plan bundle folder contract so standard zip tools recognize them.

The JSON envelope should include:

- Format identifier.
- Format version.
- Download timestamp.
- Package mode.
- Archive note.
- Course payload or plan payload.

Future bundles that include attached lesson files, assignment files, or other artifacts should add those files beside the relevant JSON pack file and keep the JSON as the manifest.

## Default Pack Rule

The default Michigan pack should represent a transcript-recognizable planning starter aligned with common Michigan high-school credit categories and Michigan homeschool subject areas.

The default Michigan pack should total 8 planned credits and keep history separate from government/civics/economics.

Default government/civics and U.S. history options should map U.S. Constitution and Michigan Constitution coverage from the Michigan seed.

Default-pack modules should include at least one assignment. Built-in assignments should include a hybrid variant and additional variants for common instructional method profiles so parent/admin users can adapt the work to the course method. Supported assignment method profiles include hybrid, explicit/guided practice, project-based/applied, inquiry/discussion, independent study, mastery practice, and digital.

The pack must not claim that imported courses satisfy graduation requirements, legal requirements, college admission requirements, or transcript acceptance standards.

## Course Description Rule

Standard course titles are allowed, but each course must support a distinct course description so the parent can record the actual texts, methods, topics, assessments, and differentiating details.

Texts/resources and learning objectives should be edited as itemized lists in the UI even when their compact storage format remains newline-based.

Major resources should not be duplicated under curriculum plan when texts/resources already capture course materials.
