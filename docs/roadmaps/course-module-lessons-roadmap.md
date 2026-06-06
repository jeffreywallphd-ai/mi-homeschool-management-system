# Course Module Lessons Roadmap

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: phased implementation plan for lessons inside course learning modules
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Course Learning Modules Roadmap](course-learning-modules-roadmap.md), [Curriculum Planning Rules](../domain/curriculum-planning-rules.md), [Course Pack Rules](../domain/course-pack-rules.md), [Blazor UI Pack](../context/packs/blazor-ui.pack.md)
- Related tests: `HomeschoolManager.Tests`
- Supersedes: module-level concrete resource placement from the course learning modules roadmap once accepted

## Slice Goal

Add lessons beneath learning modules so modules organize objectives and instructions, while lessons provide the concrete instructional path, introductory text, and specific resources the student should use.

The default Michigan course pack should receive researched lesson plans for every built-in module, with at least one lesson for each module learning objective and specific reading/video/article resources tied to that lesson.

## In Scope

- Course-owned modules containing lesson collections.
- Lesson model with stable id, source lesson id, sequence order, title, introductory text, linked module objective, and one or more resources.
- Lesson resources with type, name, URL or file/physical marker, and optional citation/source note.
- Parent/admin lesson create/edit page separate from the module edit page.
- Lesson navigation from admin module pages.
- Default pack lesson definitions for every module in every default-pack course option.
- Backfill of lessons into already imported built-in pack modules.
- Student portal module page integration that shows lessons and lesson resources in read-only form.
- Documentation updates retiring module-level resources as the concrete student resource layer.

## Out of Scope

- Assignment submission workflow.
- Lesson completion tracking.
- Attendance or time logs.
- Gradebook scoring.
- External course-pack installation.
- Automatic resource availability monitoring.
- Claims that lesson resources satisfy legal, college, or accreditation requirements.

## Design Direction

The hierarchy should be:

1. Course: transcript-facing record and syllabus-level description.
2. Module: course-owned topic arc with instructions, objectives, term placement, status, and assignment/evidence placeholder.
3. Lesson: module-owned instructional step aligned to one module learning objective.
4. Lesson resource: concrete reading, chapter, article, video, file, or physical item used for that lesson.

Module resources should be migrated out of the active UI and pack contract as concrete resources. Existing module resources may be converted into starter lesson resources during backfill when a safe mapping is available, or preserved in data only as legacy fields until a cleanup migration is accepted.

## Lesson Contract

A lesson should include:

- Stable id.
- Stable source lesson id for built-in pack backfill.
- Course id and module id by ownership path.
- Sequence order within the module.
- Title.
- Introductory text describing the lesson topic and what the student should focus on.
- Optional linked module learning objective id or objective text.
- One or more resources.

A lesson resource should include:

- Stable id.
- Name.
- Resource type: reading, textbook chapter, article, video, website, file, or physical resource.
- URL when online.
- File path when uploaded locally.
- Physical-resource flag when not digital.
- Optional citation/source note.

## Research Standard

Each default-pack lesson should be researched before writing pack content.

Research should prioritize:

- Open, stable, student-appropriate resources.
- Specific chapters or sections rather than generic textbook home pages.
- Direct article or video links tied to the lesson topic.
- Reputable sources such as OpenStax, CK-12, Khan Academy, Crash Course, official government/history sources, Library of Congress, National Archives, university/public education resources, and reputable museums or science organizations.
- Resource diversity where useful: at least one reading plus one visual/video or applied resource when appropriate.

Research notes should be kept source-backed during implementation, but avoid turning the student UI into a citation database.

## Phase 1: Research and Contract Design

### Build

- Review current module, course-pack, backfill, and student portal contracts.
- Research lesson/resource modeling patterns for curriculum systems and local-first apps.
- Finalize lesson and lesson-resource domain contracts.
- Decide how to treat legacy module resources during migration.
- Add design notes to canonical docs if the roadmap is accepted.

### Exit Criteria

- Lesson ownership and validation rules are explicit.
- Module-level concrete resources are no longer the intended active design.
- Backfill behavior for existing imports is clearly defined.

### Verification

- Design review against docs/README.md precedence rules.
- Confirm no conflict with parent-owned records, local-first data, or legal-language boundaries.

## Phase 2: Domain and Application Contracts

### Build

- Add `Lesson` and `LessonResource` domain records.
- Add validation for module id, title, intro text, sequence order, linked objective, and at least one resource.
- Add lesson create/update/delete/reorder commands.
- Add lesson list/detail read models.
- Keep all lesson mutations parent/admin-only.
- Add student read models that expose lesson content without mutation fields.

### Exit Criteria

- Lessons cannot exist outside a module.
- Lessons are ordered deterministically.
- A module objective can be supported by one or more lessons.
- Student role cannot mutate lessons.

### Verification

- Domain tests for required fields and ownership.
- Application tests for parent mutations and student denial.
- Tests confirm at least one resource per lesson.

## Phase 3: Persistence and Backfill Engine

### Build

- Persist lessons inside modules or through a course-owned lesson collection, matching the existing local JSON store shape.
- Add built-in pack lesson backfill by stable source lesson id.
- Backfill lessons into already imported built-in modules.
- Preserve parent-created and parent-edited lessons.
- Convert legacy module resources into starter lesson resources only where safe and non-destructive.

### Exit Criteria

- Lessons persist and reload.
- Backfill is idempotent.
- Existing imported default-pack modules receive lessons.
- Parent lesson edits are not overwritten by pack updates.

### Verification

- Persistence tests.
- Backfill tests for old imports, current imports, and parent-edited lessons.
- Regression tests for module backfill and course detail backfill.

## Phase 4: Admin Lesson UI

### Build

- Add a dedicated lesson detail page, separate from the module edit page.
- Add lesson links under the module context in admin navigation or within the module page.
- Keep module edit page focused on module identity, instructions, objectives, status, and evidence placeholder.
- Add lesson identity, intro text, linked objective dropdown, and itemized resources.
- Add Add/Hide resource form for lesson resources.
- Support URL, local file, and physical resource options.
- Use validation messages and required-field asterisks.

### Exit Criteria

- Parent/admin can create, edit, reorder, and delete lessons without crowding the module page.
- Lesson resource editing is clear and itemized.
- Lesson page follows existing two-column UI spacing and responsive behavior.

### Verification

- Manual UI review.
- Build and tests pass.
- Browser smoke check when available.

## Phase 5: Default Pack Lesson Research and Content

### Build

- For every default-pack course option, research each module objective.
- Add at least one lesson per module learning objective.
- Add specific lesson resources such as textbook chapter links, article links, videos, primary sources, simulations, or official reference pages.
- Ensure lesson introductions are written for student use, not admin-only planning notes.
- Keep course-level resources as syllabus-level materials.
- Remove active module-level resources from pack definitions once lesson resources replace them.

### Exit Criteria

- Every built-in module objective is supported by at least one lesson.
- Every lesson has one or more specific resources.
- Default pack remains 8 credits and keeps existing course/module coverage.
- Lesson content is useful enough for the student to begin work without parent rewriting.

### Verification

- Pack contract tests confirm every module objective has lesson coverage.
- Pack contract tests confirm every lesson has resources.
- Source/link sampling review for default-pack resources.
- Legal-language scan for no compliance or accreditation claims.

## Phase 6: Student Portal Integration

### Build

- Update student module read model to include lessons.
- On the student module page, show module overview and then lessons in sequence.
- For each lesson, show title, intro text, linked objective, and resources.
- Replace module-level resource display with lesson-level resources.
- Keep assignment/evidence placeholder at module level until assignments become their own feature.
- Keep student pages read-only.

### Exit Criteria

- Student can open a module and see a lesson-by-lesson path.
- Lesson resources are actionable and clearly tied to lesson topics.
- No admin controls appear in the student portal.

### Verification

- Student read-model tests.
- UI review on mobile, tablet, and laptop/PC.
- Browser smoke check when available.

## Phase 7: Documentation and Context Update

### Build

- Update curriculum planning rules.
- Update course pack rules.
- Update Blazor UI context pack.
- Update curriculum/instruction context pack.
- Keep every context pack under 200 lines.
- Record deferred decisions.

### Exit Criteria

- Canonical docs consistently place concrete resources at the lesson level.
- Context packs route future agents away from reintroducing module-level concrete resources.
- Deferred lesson completion, assignment, and grading decisions are named.

### Verification

- Context pack line-count check.
- Docs conflict review.

## Phase 8: Final Verification and Handoff

### Build

- Run build and full app tests.
- Attempt browser smoke check.
- Review changed contracts for null-safety and role boundaries.
- Summarize known limitations and next likely slice.

### Exit Criteria

- Build passes.
- Tests pass.
- Student portal and admin lesson pages have an end-to-end path.
- Existing imported data is backfilled without destructive overwrite.

### Verification

- Build command succeeds.
- Test command succeeds.
- Browser smoke check attempted.
- Final report includes changed files, tests, and any blocked verification.

## Deferred Decisions

- Whether students may mark lessons complete.
- Whether lessons become attendance/activity log units.
- Whether lesson resources need citation styles for official packets.
- Whether lesson resources should support downloaded/offline snapshots.
- Whether assignments attach to lessons, modules, or both.
- Whether parent notes need separate lesson-level visibility controls.
