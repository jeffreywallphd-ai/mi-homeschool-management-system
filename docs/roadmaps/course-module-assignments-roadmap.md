# Course Module Assignments Roadmap

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: phased implementation plan for admin and student assignment features
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Curriculum Planning Rules](../domain/curriculum-planning-rules.md), [Course Pack Rules](../domain/course-pack-rules.md), [Assessment and Grading Rules](../domain/assessment-and-grading-rules.md), [Portfolio Evidence Rules](../domain/portfolio-evidence-rules.md), [Course Module Lessons Roadmap](course-module-lessons-roadmap.md), [Student Course Client Roadmap](student-course-client-roadmap.md)
- Related tests: `HomeschoolManager.Tests`
- Supersedes: module assignment/evidence placeholder as the active assignment model once implemented

## Slice Goal

Add structured assignments beneath course modules so the parent can turn module objectives and lesson materials into concrete student work, while the student portal shows clear read-only assignment expectations.

The default Michigan course pack should receive source-backed assignment sets for every built-in module. Assignment sets must align with module objectives, use lesson resources as the student-facing material base, and include variants that match common instructional methods.

## In Scope

- Module-owned assignments with stable ids and source assignment ids.
- Assignment links to one or more module objectives.
- Optional assignment links to one or more lessons and lesson resources.
- Assignment variant templates tied to instructional method profiles.
- Parent/admin assignment create, edit, reorder, delete, and variant selection.
- Student portal assignment display on module pages.
- Assignment status/planning fields that prepare for later evidence and grading.
- Default pack assignments for every module in every default-pack course option.
- Backfill into already imported built-in pack modules.
- Documentation and tests for assignment contracts, role boundaries, and pack coverage.

## Out of Scope

- Student submission upload workflow.
- Parent grading workflow and gradebook calculations.
- Transcript grade calculation.
- Attendance or time logging.
- Rubric scoring UI.
- External course-pack installation.
- Claims that assignment completion satisfies legal, graduation, college, or accreditation requirements.

## Design Direction

The hierarchy should be:

1. Course: transcript-facing record and syllabus context.
2. Module: topic arc with objectives, instructions, lessons, and assignments.
3. Lesson: instructional step with concrete resources.
4. Assignment: work the student should complete using lessons and resources.
5. Evidence record: later parent-owned proof that an assignment or activity was completed.

Assignments should be module-owned because the parent thinks in module-sized work expectations, but each assignment should be able to reference the lesson materials it uses. This keeps assignments visible at the module level without crowding lesson editing.

## Assignment Contract

An assignment should include:

- Stable id.
- Stable source assignment id for built-in pack backfill.
- Course id and module id by ownership path.
- Sequence order within the module.
- Title.
- Purpose or instructions written for the student.
- Assignment type.
- Estimated effort.
- Due timing label or optional due date.
- One or more linked module objective texts or ids.
- Zero or more linked lesson ids.
- Required output or evidence expectation.
- Optional parent notes not shown to student.
- Optional portfolio-worthy flag.
- Optional planned points or weight placeholder.
- Status: planned, assigned, submitted, reviewed, complete, or skipped.

Assignment types should include reading response, problem set, lab or simulation, discussion, project, essay, quiz/test prep, presentation, portfolio artifact, reflection, and practical demonstration.

## Instructional Method Variants

Course packs should include assignment variants by instructional method profile. The first implementation should support:

- Hybrid / combined methods: balanced reading, practice, discussion/reflection, and evidence artifact.
- Explicit instruction and guided practice: modeled examples, guided questions, practice set, and parent review.
- Project-based or applied learning: product, demonstration, design, investigation, or real-world application.
- Inquiry or discussion-based learning: question-driven reading, source analysis, discussion notes, and written claim.
- Independent study: reading/viewing plan, notes, self-check, and synthesis artifact.
- Mastery practice: repeated practice, correction cycle, and short demonstration of proficiency.

Each module in the default pack should have at least one hybrid assignment as the default. Other variants should be available for parent selection or replacement. Variants must preserve the same objective coverage whenever possible.

## Student UX Direction

Student module pages should show assignments in the left column after Lessons, because lessons explain what to learn and assignments explain what to do with that learning.

For each assignment, the student should see:

- Title.
- Instructions.
- Linked objective summary.
- Related lessons.
- Required output/evidence.
- Estimated effort or timing.
- Status.

The student portal must remain read-only in this slice. If a future submission workflow is added, it should be a separate accepted slice.

## Admin UX Direction

The module edit page should keep Assignments as a main module-management section, near Lessons.

Admin controls should include:

- New assignment.
- Edit assignment on a dedicated page or focused edit panel if the page remains uncluttered.
- Reorder with Up/Down controls.
- Delete with exact `Delete` confirmation.
- Variant selector for built-in assignment options when source variants exist.
- Clear labels for student-visible instructions versus parent-only notes.

Assignment editing should use itemized objective and lesson links rather than raw text blobs.

## Default Pack Research Standard

Default-pack assignments should be researched and written module by module.

Research should prioritize:

- Assignment patterns common to the course discipline.
- The lesson resources already selected for each module.
- Objective-aligned tasks that produce useful parent-owned records.
- A mix of reading, writing, practice, analysis, applied work, and reflection.
- Age-appropriate senior-year expectations.

Assignments should be specific enough to be useful immediately, but editable so the parent can adjust rigor, resources, or output.

## Phase 1: Research and Contract Design

### Build

- Review current course, module, lesson, pack, student portal, assessment, and portfolio docs.
- Research assignment patterns for high-school humanities, math, science, civics, finance, arts/electives, capstone, and world language courses.
- Finalize assignment ownership and validation rules.
- Define assignment types, statuses, and method profiles.
- Decide how module assignment/evidence placeholders migrate into structured assignments.

### Exit Criteria

- Assignment is clearly module-owned.
- Lesson links are optional but supported.
- Assignment variants have stable method-profile identifiers.
- Parent-only fields and student-visible fields are separated.

### Verification

- Design review against docs/README.md authority rules.
- No conflict with parent-owned records, student read-only boundaries, or legal-language boundaries.

## Phase 2: Domain and Application Contracts

### Build

- Add assignment domain record and assignment variant value objects as needed.
- Add validation for module ownership, title, instructions, sequence order, objective links, status, and required output.
- Add create/update/delete/reorder commands.
- Add assignment list/detail read models for admin.
- Add student assignment read models without mutation fields.
- Keep all assignment mutations parent/admin-only.

### Exit Criteria

- Assignments cannot exist outside a module.
- Assignments are ordered deterministically.
- Assignments can link to module objectives and lessons.
- Student role cannot mutate assignments.

### Verification

- Domain tests for required fields, ownership, ordering, and status.
- Application tests for parent mutations and student denial.
- Null-safety tests for optional due dates, lesson links, and point placeholders.

## Phase 3: Persistence and Backfill

### Build

- Persist assignments inside modules, matching the local JSON course/module shape unless a stronger local pattern emerges.
- Add built-in pack assignment backfill by stable source assignment id.
- Backfill assignments into already imported built-in modules.
- Preserve parent-created and parent-edited assignments.
- Convert module assignment/evidence placeholder text into a parent note or fallback assignment only when non-destructive.

### Exit Criteria

- Assignments persist and reload.
- Backfill is idempotent.
- Existing imported default-pack modules receive assignments.
- Parent assignment edits are not overwritten by pack updates.

### Verification

- Persistence tests.
- Backfill tests for old imports, current imports, and parent-edited assignments.
- Regression test that nested assignment changes are included in module comparison.

## Phase 4: Admin Assignment UI

### Build

- Add assignment section on the module edit page below Lessons.
- Add New assignment button.
- Show assignments in sequence with Up, Down, Open/Edit, and Delete controls.
- Add assignment detail page or focused editor with student-visible instructions, linked objectives, linked lessons, evidence expectation, status, timing, and parent notes.
- Add method variant selector when built-in variants are available.
- Use visible validation, asterisks for required fields, and exact `Delete` confirmation.

### Exit Criteria

- Parent/admin can create, edit, reorder, delete, and inspect assignments.
- Assignment editing does not crowd lesson/resource editing.
- Variant selection is understandable and non-destructive.

### Verification

- Build and tests pass.
- Browser smoke check when available.
- UI review for desktop, tablet, and mobile.

## Phase 5: Student Portal Integration

### Build

- Add assignments to the student module read model.
- Show Assignments in the left column after Lessons.
- Display assignment title, instructions, linked lessons/objectives, evidence expectation, timing, and status.
- Keep Assignments and Evidence support content in the right column only if it is summary or next-step context.
- Ensure no edit controls appear for student sessions.

### Exit Criteria

- Student can open a module and see what work to complete.
- Assignments are readable without parent/admin context.
- Student portal remains read-only.

### Verification

- Student read-model tests.
- Role-boundary tests.
- Responsive UI review.

## Phase 6: Default Pack Assignment Research and Content

### Build

- For every default-pack module, design at least one hybrid assignment.
- Add method-specific assignment variants for each module where appropriate.
- Align every assignment to one or more module objectives.
- Link assignments to relevant lessons and lesson resources.
- Match assignment format to the course instructional methods.
- Keep assignment language student-facing and editable.

### Exit Criteria

- Every built-in module has at least one assignment.
- Every module objective is covered by at least one assignment across the module.
- Every built-in assignment links to relevant lessons or explicitly explains why it is module-level.
- Every course option has usable assignment variants for the supported method profiles.

### Verification

- Pack contract tests for assignment coverage.
- Method-profile tests that each default module has a hybrid assignment.
- Sampling review for disciplinary fit and resource alignment.
- Legal-language scan for no compliance or accreditation claims.

## Phase 7: Evidence and Gradebook Bridge

### Build

- Add fields needed for later evidence records without building submission workflow.
- Add optional portfolio-worthy marker.
- Add optional planned points or weight placeholder without grade calculation.
- Add status values that can support future submitted/reviewed/complete workflows.
- Document deferred submission, evidence file, rubric, and gradebook decisions.

### Exit Criteria

- Assignment records can later connect to evidence and grades.
- No placeholder is treated as a real grade or completed work record.
- Future implementation paths are explicit.

### Verification

- Tests confirm no grade is inferred from assignment status or planned points.
- Docs conflict review with assessment and portfolio rules.

## Phase 8: Documentation and Context Update

### Build

- Update curriculum planning rules.
- Update course pack rules.
- Update assessment and grading rules if assignment status touches assessment language.
- Update portfolio evidence rules if portfolio-worthy markers are introduced.
- Update Blazor UI and curriculum context packs.
- Keep context packs under 200 lines.

### Exit Criteria

- Canonical docs consistently place structured assignments at the module level.
- Context packs route future agents away from reusing the old assignment/evidence placeholder as the active model.
- Deferred decisions are named.

### Verification

- Context pack line-count check.
- Docs conflict review.

## Phase 9: Final Verification and Handoff

### Build

- Run build and full tests.
- Attempt browser smoke check.
- Review parent/admin and student role boundaries.
- Review default-pack backfill against existing local data.
- Summarize limitations and next likely slice.

### Exit Criteria

- Build passes.
- Tests pass.
- Admin and student assignment flows work end to end.
- Existing imported data is backfilled without destructive overwrite.

### Verification

- Build command succeeds.
- Test command succeeds.
- Browser smoke check attempted.
- Final report includes changed files, tests, and any blocked verification.

## Deferred Decisions

- Whether students can submit files or mark assignments complete.
- Whether parent review creates evidence records automatically.
- Whether assignment status should connect to activity logs.
- Whether assignment variants can be switched after parent edits.
- Whether rubrics belong on assignments or separate assessments.
- Whether assignment completion should drive module completion.
