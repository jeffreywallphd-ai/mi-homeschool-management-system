# Blazor UI Pack

Purpose: UI workflow and form-boundary guidance.

## Canonical Sources

- `docs/architecture/modular-monolith-boundaries.md`
- `docs/architecture/identity-and-access-architecture.md`
- `docs/standards/accessibility-and-nontechnical-ux-standards.md`
- `docs/standards/coding-standards.md`
- Task-specific domain docs

## Must Preserve

- UI uses explicit view models and commands.
- High-stakes actions require explicit parent confirmation.
- Validation errors are visible and plain.
- Required fields use an asterisk.
- Planning screens put main forms/records on the left and support tools on the right.
- Optional helper tables in support panels may be collapsed by default when they are not the main task.
- Course coverage support summaries belong with related support tools when the main workflow is course list/editing.
- Course rows are clickable navigation targets with hover affordance; avoid separate Open columns.
- Admin course pages include a header-level student selector when multiple students may exist.
- Course list, create, import, and coverage views must reflect the selected student instead of all household courses.
- Course list archive/delete actions appear beside the Course List heading only when active courses exist.
- Course list archive/delete actions support selected courses or the full active list and require confirmation.
- Archive hides courses from active admin and student lists while retaining course details and future student work.
- Delete removes eligible courses; courses with student work must fail with a plain message directing the parent to archive.
- Course forms do not expose subject-area text fields; coverage is managed through requirement mappings.
- Course credit displays should include at least one decimal place and preserve more precise values when present.
- Course detail uses two columns: identity/description/resources/assessment on the left, plan/mapping/current mappings on the right.
- Course detail uses autosave for field changes; requirement mapping remains an explicit add/update action.
- Course detail autosave feedback belongs in the page header bar above the main content.
- Course detail stays course/syllabus-focused; module details live on separate course module pages with course-scoped module subnavigation in the left nav.
- Course detail includes a Modules section below mappings for modulepack import, modulepack download/template download, module creation, module list navigation, reorder, and delete.
- Course module pages show the modules list first in the left column.
- Course module subnavigation includes a "Course Modules" heading and a parent/admin "+ New Module" link.
- Module delete actions require a modal confirmation and exact `Delete` text.
- Module objectives use itemized Add/Hide controls; module objective links to course objectives are optional.
- Lesson details live on a separate page from module editing and manage lesson resources with itemized Add/Hide controls.
- Lesson edit pages expose lesson metadata, objectives, workflow steps, resources, practice/problem sets, portfolio connections, rubric criteria, reflection prompts, parent notes, and linked assignment controls.
- Course module lesson sections expose `.lessonpack` import, current-module lesson download, and template download controls.
- Lesson pack import controls show progress and plain-language success or failure messages.
- Assignments are managed from the module page list and edited on a dedicated assignment page.
- Course module assignment sections expose `.assignmentpack` import, current-module assignment download, and template download controls.
- Assignment pack import controls show progress and plain-language success or failure messages.
- Assignment edit pages use itemized objective and lesson selections, not raw mapping text.
- Assignment edit pages expose summary, goal, deliverables, submission formats, resources, steps, portfolio, rubric, revision, completion, evidence, scoring, and parent-note controls.
- Student module pages show assignments in the main learning flow after lessons, with evidence notes only as support content.
- Student module pages show student-facing assignment details: goal, deliverables, steps, resources, checklist, completion criteria, portfolio notes, rubric summaries, reflection prompts, and related lessons.
- Student module pages show student-facing lesson details: objectives, success criteria, workflow, resources, practice prompts, portfolio connections, rubric summaries, reflection prompts, and related assignments.
- Student module pages do not show lesson answer keys, worked solutions, or parent/instructor notes.
- The Courses page support card is the Course plan card; it imports and downloads course plan `.zip` files.
- The Courses page provides single-course `.coursepack` import and coursepack template download above the course list.
- Course plan bundle and coursepack import actions show progress and plain-language success or failure messages.
- Page columns should fill available width and have generous horizontal spacing.
- Course detail text areas allow at least four visible lines before scrolling.
- Preset dropdown `Other` choices clear the associated text area so parent text starts clean.
- Preset dropdown plus textarea pairs use one visible label and hidden secondary labels as needed for accessibility.
- Texts/resources and learning objectives are edited as itemized lists with Add/Hide controls.
- UI does not mutate domain objects directly.
- Student PIN sessions cannot reach admin actions.
- Logged-out navigation exposes only Login and startup routes to Login.
- Student sessions route to the student course client and should not see parent/admin navigation.
- Student client pages are read-only: dashboard, course landing, syllabus, and module detail.
- Student module pages show lesson-by-lesson resources rather than module-level resource editing data.
- Student client pages use student read models instead of admin form models or mutation commands.
- Parent/admin preview uses child-specific student paths selected from configured children and must be visibly labeled as preview.
- Student dashboard and module lists group items under configured semester headings when terms exist.
- Student pages must support mobile, tablet, and laptop/PC screens; use one column on narrow screens and two columns only when content has room.

## Common Failure Modes

- Nullable form state leaks into domain operations.
- Support panels become hidden decision points instead of visible helpers.
- UI screens hide legal or credential boundaries.
