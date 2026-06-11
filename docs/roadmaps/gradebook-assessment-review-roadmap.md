# Gradebook and Assessment Review Roadmap

Status: implemented  
Last reviewed: 2026-06-11  
Canonical for: phased implementation of parent/admin assessment records and gradebook review built from reviewed submissions and evidence.

## Related Documentation

- `../README.md`
- `../context/packs/index.pack.md`
- `../context/packs/assessment-credits-graduation.pack.md`
- `../context/packs/blazor-ui.pack.md`
- `../context/prompt-routing.md`
- `../standards/change-impact-matrix.md`
- `../standards/testing-and-verification-standards.md`
- `../standards/accessibility-and-nontechnical-ux-standards.md`
- `../domain/assessment-and-grading-rules.md`
- `../domain/credits-and-graduation-rules.md`
- `../architecture/identity-and-access-architecture.md`
- `assignment-submissions-and-evidence-roadmap.md`

## Documentation Governance Alignment

This roadmap follows the repository guidance in `docs/README.md`:

- Accepted ADRs and canonical docs remain the source of truth.
- Context packs summarize canonical docs and do not override them.
- Any implementation must update docs and tests in the same change set when behavior or terminology changes.
- Deferred decisions must remain explicit instead of being decided during implementation.
- This slice must not imply legal compliance, accreditation, MDE approval, state approval, or legal certification.

## Slice Goal

Add a parent/admin gradebook and assessment review slice that turns reviewed student work into explicit parent-owned assessment records.

The system should let the parent/admin review accepted submissions and evidence, record an assessment result, and make feedback visible to the student when appropriate. This slice must not infer grades from assignment status, submission status, planned points, planned weight, module completion, or course existence.

## Implementation Notes

Implemented in:

- `src/HomeschoolManager.Domain/Assessments/`
- `src/HomeschoolManager.Application/Assessments/`
- `src/HomeschoolManager.Web/Components/Pages/Gradebook.razor`
- `src/HomeschoolManager.StudentPortal.Web/Components/Pages/StudentModule.razor`

The parent/admin dashboard now links to the gradebook and shows assessment queue counts. Student-facing module pages show only parent-approved assessment feedback. Parent/admin student preview continues to use the admin build and does not expose student submissions or assessment feedback.

## In Scope

- Parent/admin gradebook view by student and course.
- Parent/admin assessment records linked to assignment, submission, evidence, or course context.
- Assessment result types for narrative, rubric summary, pass/fail, points, percentage, letter grade, and explicit not-graded states.
- Parent/admin feedback and visibility setting for student-facing feedback.
- Dashboard counts for work needing assessment where source data exists.
- Student view of parent-approved feedback/status only.
- Contract-backed authorization so only parent/admin can create or edit assessment records.
- Tests for missing-grade states, role boundaries, persistence, and no accidental credit/GPA effects.

## Out Of Scope

- Final course grade calculation.
- GPA calculation.
- Credit awards.
- Course completion.
- Report cards.
- Transcripts.
- Diplomas or graduation-plan satisfaction.
- Automatic assessment creation from submitted or accepted work.
- Automatic portfolio artifact creation.
- Legal compliance or accreditation wording.

## Design Direction

### Object Model

- `AssessmentRecord`: parent-owned assessment of a specific piece of work or course context.
- `AssessmentSource`: optional link to submission, evidence record, assignment, test, portfolio artifact, or parent evaluation.
- `AssessmentResult`: explicit result type and value.
- `AssessmentFeedback`: parent notes plus student-visible feedback flag.
- `GradebookCourseSummary`: read model for course-level assessment status, not a final grade.

### Assessment States

Use explicit states instead of null-as-meaning:

- `NotAssessed`
- `NeedsReview`
- `Assessed`
- `ReturnedForRevision`
- `Excused`
- `Incomplete`
- `NotApplicable`

These states must not become zero, pass, fail, credit, course completion, or GPA input without a later explicit parent/admin action.

### Result Types

Support mixed assessment styles:

- Narrative evaluation.
- Rubric summary.
- Pass/fail.
- Points earned out of points possible.
- Percentage.
- Letter grade.
- Test score.
- Not graded or not applicable.

Planned assignment points and planned weight may prefill suggestions only if clearly labeled as planning data. They must not create grades by themselves.

## Parent/Admin Experience

Parent/admin should get a practical gradebook workbench:

- Student selector and course selector.
- Course assessment summary with counts for needs review, assessed, returned, incomplete, excused, and not applicable.
- Assignment/evidence rows with source context, submission status, evidence status, and assessment status.
- Assessment editor panel for result type, value, feedback, internal notes, student-visible notes, and source links.
- Clear warning that assessment records are not final course grades, GPA, credits, transcripts, or diploma readiness.

The UI should use existing app layout patterns: main records on the left, support/editor panel on the right, plain validation messages, and explicit save actions for assessment records.

## Student Experience

Student-facing pages may show:

- Parent-approved feedback.
- Assessment status in plain language.
- Returned-for-revision notes.
- Reviewed/assessed indicators.

Student-facing pages must not show:

- Admin-only notes.
- Grade editing controls.
- Credit controls.
- GPA, transcript, report-card, or diploma controls.
- Parent/admin review queues.

## Phases

### Phase 1 - Contract And Documentation Pass

Build:

- Reconfirm assessment, credits, access, UI, and testing docs.
- Define exact assessment states, result types, and source-link rules.
- Identify docs and context packs requiring updates during implementation.

Exit criteria:

- No conflict with accepted grading, credit, graduation, or identity rules.
- Slice boundary remains assessment records, not final grades or credits.
- Deferred decisions are listed.

Verification:

- Documentation review against `docs/README.md`.
- Change-impact review for grading/GPA, UI workflow, and auth/student access rows.

### Phase 2 - Domain And Application Contracts

Build:

- Add assessment domain records and result value objects.
- Add parent/admin commands for create, update, delete/archive, and publish feedback.
- Add read models for gradebook course summaries and assessment details.
- Enforce parent/admin-only mutation.

Exit criteria:

- Assessment records require student and course context.
- Missing/unassessed states are explicit.
- Student role cannot create, edit, delete, or publish assessments.

Verification:

- Domain tests for required fields and valid states.
- Authorization tests for parent/admin and student boundaries.

### Phase 3 - Persistence And Migration

Build:

- Extend local persistence for assessment records.
- Preserve existing course, assignment, submission, and evidence data.
- Add schema/version handling if needed.

Exit criteria:

- Existing data opens without destructive changes.
- Assessment records survive restart.
- Source links remain stable even when optional source data is absent.

Verification:

- Persistence round-trip tests.
- Backward-compatibility tests with representative existing data.

### Phase 4 - Parent/Admin Gradebook View

Build:

- Add gradebook entry point from dashboard and course areas.
- Add student/course selector.
- Show assessment rows grouped by module or assignment sequence.
- Surface accepted submissions/evidence needing assessment.

Exit criteria:

- Parent/admin can see work needing assessment.
- Rows distinguish planned assignment data from recorded assessment data.
- No final grade, GPA, or credit is calculated.

Verification:

- UI smoke check for empty, pending, and assessed states.
- Tests for summary counts where practical.

### Phase 5 - Assessment Editor

Build:

- Add editor for assessment state, result type, result value, parent notes, student-visible feedback, and source links.
- Show validation near incomplete fields.
- Require explicit save.
- Allow revision/returned status without producing a grade.

Exit criteria:

- Parent/admin can save each supported assessment style.
- Invalid result combinations show plain validation messages.
- Planned points do not become actual grades unless parent/admin records an assessment result.

Verification:

- Application tests for all result types.
- UI smoke check for save, validation, and edit flows.

### Phase 6 - Student Feedback Visibility

Build:

- Add student-facing feedback/status read model.
- Show only parent-approved feedback.
- Hide internal notes and grade-editing controls.

Exit criteria:

- Student can see approved feedback for assessed or returned work.
- Student cannot see internal parent/admin notes.
- Student cannot mutate assessment records.

Verification:

- Authorization tests.
- Browser smoke check for student feedback display.

### Phase 7 - Dashboard And Review Queue Integration

Build:

- Add dashboard counts for needs assessment, returned work, and recently assessed work.
- Add links into gradebook filtered views.
- Use placeholders only where no underlying data exists.

Exit criteria:

- Dashboard reflects real assessment records and reviewed evidence.
- Counts do not imply progress, course completion, credit, or GPA.

Verification:

- Tests for count logic.
- UI smoke check for dashboard links.

### Phase 8 - Documentation And Context Updates

Build:

- Update canonical docs if assessment terminology or behavior changes.
- Update context packs if summaries need to route future work differently.
- Keep context packs under the 200-line limit.

Exit criteria:

- Docs match implemented behavior.
- No stale roadmap or context-pack conflict remains.

Verification:

- Documentation checklist review.
- Context-pack line-count check if edited.

### Phase 9 - Final Verification And Handoff

Build:

- Run focused domain, application, persistence, and UI checks.
- Verify parent/admin and student role boundaries.
- Prepare handoff notes.

Exit criteria:

- Parent/admin can assess reviewed work end to end.
- Student can view approved feedback but cannot edit assessment records.
- No GPA, credit, transcript, report-card, course-completion, or diploma behavior is introduced.

Verification:

- Automated test suite.
- Focused browser smoke checks for gradebook, editor, dashboard, and student feedback.

## Deferred Decisions

These decisions are intentionally not resolved by this roadmap:

- Final course-grade calculation.
- GPA calculation and grade-scale weighting.
- Credit award workflow.
- Course completion workflow.
- Report-card generation.
- Transcript generation.
- Diploma or graduation-plan readiness.
- Whether assessment records can later promote evidence into portfolio artifacts.
- Whether advanced rubric scoring should calculate numeric results.

## Recommended Next Vertical Slice

After this roadmap is complete, the next best slice is final course-grade review and course completion readiness. That later slice can consume explicit parent/admin assessment records while still keeping credit awards, GPA, transcripts, and graduation decisions separate.
