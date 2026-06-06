# Course Learning Modules Roadmap

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: phased implementation plan for course learning modules
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Course Pack Rules](../domain/course-pack-rules.md), [Curriculum Planning Rules](../domain/curriculum-planning-rules.md), [Requirement Mapping Rules](../legal-requirements/requirement-mapping-rules.md), [Domain Module Map](../architecture/domain-module-map.md), [Course Curriculum Planning Roadmap](course-curriculum-planning-roadmap.md)
- Related tests: `HomeschoolManager.Tests`
- Supersedes: none

Note: [Course Module Lessons Roadmap](course-module-lessons-roadmap.md) supersedes module-level concrete resources as the active student-facing resource layer. Modules still organize objectives and instructions; lessons now carry concrete readings, videos, files, and physical resources.

## Slice Goal

Add learning modules as the instructional structure inside each course. A course remains the transcript-facing record, while modules organize the course into teachable topic units with instructions, learning objectives, resources, assignment/evidence placeholders, and optional requirement-support context.

## In Scope

- Course-owned learning module model.
- Module title, description, sequence order, optional term placement, estimated length, instructions, itemized learning objectives, concrete itemized resources, status, and assignment/evidence placeholder.
- No module-level goals field.
- Parent/admin module create, edit, reorder, and view flow.
- Student read-only module view.
- Course-scoped subnavigation that lists modules under the course being edited.
- Learning modules in course-pack contracts.
- Default Michigan pack modules for every default-pack course option.
- Backfill of modules into already imported built-in pack courses without overwriting parent-created modules.

## Out of Scope

- Full assignment workflow.
- Submission collection.
- Gradebook scoring.
- File upload and evidence storage.
- Lesson attendance or time logs.
- Transcript changes.
- Module-level legal compliance claims.

## Phase 1: Domain Model and Contracts

### Build

- Add LearningModule as a course-owned record.
- Add ModuleStatus with planned, active, and complete.
- Add validation for course id, title, sequence order, and complete contracts.
- Store learning objectives and resources as itemized lists.
- Add an assignment/evidence placeholder field that can later become structured evidence records.
- Exclude module goals from the contract.

### Exit Criteria

- Modules cannot exist without a course.
- Modules have stable ids and deterministic ordering.
- Module learning objectives are sufficient for module purpose.
- Invalid module contracts fail before persistence.

### Verification

- Domain tests cover required fields and ordering.
- Domain tests confirm goals are not part of the module contract.
- Domain tests cover status values and itemized objective/resource behavior.

## Phase 2: Application Services

### Build

- Add create module command.
- Add update module command.
- Add reorder modules command.
- Add module list and module detail queries.
- Add parent/admin authorization to all mutations.
- Keep student access read-only.

### Exit Criteria

- UI can use commands without touching domain objects directly.
- Student role cannot mutate modules.
- Module queries include enough data for course-scoped subnavigation and module editing pages.

### Verification

- Application tests cover parent/admin mutations.
- Application tests cover student mutation denial.
- Application tests cover list/detail queries and reorder behavior.

## Phase 3: Persistence and Backfill

### Build

- Persist modules in the local data document.
- Preserve existing course, mapping, setup, and requirement data.
- Add built-in pack module backfill for imported courses.
- Backfill modules only when the course has no parent-created modules or when missing built-in module ids can be safely added.
- Never overwrite parent-edited module text.

### Exit Criteria

- Modules persist and reload.
- Existing imported courses receive default pack modules.
- Parent-created or parent-edited modules are preserved.
- Backfill remains idempotent.

### Verification

- Infrastructure tests cover module persistence.
- Backfill tests cover already imported default-pack courses.
- Regression tests confirm existing course-pack detail backfill still works.

## Phase 4: Course Module UI

### Build

- Keep the course detail page focused on course/syllabus-level information.
- Add a dedicated course module page for module detail.
- Add module create/edit form using existing two-column page styling on the module page.
- Left column: module identity, description, instructions, resources, assignment/evidence placeholder.
- Right column: itemized learning objectives, objective alignment, concrete resources, status, and related requirement-support context.
- Use autosave or explicit save consistently with current course-detail behavior.
- Show validation errors near fields.

### Exit Criteria

- Parent/admin can create and edit modules from the dedicated module page.
- Student can view module information without mutation controls.
- Textareas allow at least four visible lines.
- UI uses complete commands and clear boundaries.

### Verification

- Manual UI check for module create/edit/view.
- Build and test suite pass.
- Accessibility check for labels, buttons, status controls, and validation messages.

## Phase 5: Course-Scoped Subnavigation

### Build

- When viewing/editing a course, show a subnavigation group in the left navigation under the course title.
- Include course overview and module links in sequence order.
- Keep subnavigation course-scoped; do not show module links globally outside course context.
- Ensure long course/module names do not break the navigation layout.

### Exit Criteria

- Course workflow exposes module navigation without cluttering the global nav.
- Clicking a module link opens that module's detail page.
- The subnav updates after module create, rename, reorder, or delete if delete is included.

### Verification

- Manual UI check for course-scoped navigation.
- Browser check for long titles and mobile navigation when available.
- Navigation tests if the component boundary supports them.

## Phase 6: Default Pack Modules

### Build

- Add module definitions to the course-pack contract.
- Add modules for every course option in the default Michigan pack.
- Ensure modules align with each course's requirement mappings, resources, and learning objectives.
- Include instructions and assignment/evidence placeholders for each module.
- Keep module objectives specific and itemized.

### Exit Criteria

- Full-pack import creates courses with starter modules.
- Selected-option import creates modules for the selected option only.
- Existing imported default-pack courses are backfilled with starter modules.
- Default pack remains 8 planned credits.

### Verification

- Tests confirm every default-pack option has modules.
- Tests confirm imported courses receive modules.
- Tests confirm already imported courses backfill modules.
- Tests confirm parent-created modules are not overwritten.

## Phase 7: Closeout

### Build

- Update canonical docs if implementation changes module, course-pack, or UI rules.
- Update context packs while keeping every pack under 200 lines.
- Update README only if run/install/test behavior changes.
- Record deferred decisions.

### Exit Criteria

- Build passes.
- Tests pass.
- Context packs stay under 200 lines.
- No docs conflict with accepted ADRs or legal-language boundaries.
- Next slice can attach assignments, evidence, activity records, or grading to modules.

### Verification

- Build and app tests run.
- Context pack line-count check runs.
- Manual UI review when browser tooling is available.
- Final report names changed files, tests run, and known limitations.
