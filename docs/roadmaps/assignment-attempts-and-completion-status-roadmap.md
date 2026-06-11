# Assignment Attempts and Completion Status Roadmap

Status: accepted  
Last reviewed: 2026-06-11  
Canonical for: phased implementation of assignment attempt policy, admin submission clearing, and explicit lesson/module/course completion workflow.

## Related Documentation

- `../README.md`
- `../context/packs/index.pack.md`
- `../context/packs/assessment-credits-graduation.pack.md`
- `../context/packs/blazor-ui.pack.md`
- `../context/packs/curriculum-and-instruction.pack.md`
- `../context/packs/portfolio-and-files.pack.md`
- `../context/prompt-routing.md`
- `../standards/change-impact-matrix.md`
- `../standards/testing-and-verification-standards.md`
- `../standards/accessibility-and-nontechnical-ux-standards.md`
- `../domain/assessment-and-grading-rules.md`
- `../domain/credits-and-graduation-rules.md`
- `../domain/curriculum-planning-rules.md`
- `../architecture/identity-and-access-architecture.md`
- `assignment-submissions-and-evidence-roadmap.md`
- `gradebook-assessment-review-roadmap.md`

## Documentation Governance Alignment

This roadmap follows the repository guidance in `docs/README.md`:

- Accepted ADRs and canonical docs remain the source of truth.
- Context packs summarize canonical docs and do not override them.
- Code, tests, docs, and context packs must be updated together when behavior or terminology changes.
- Deferred decisions must remain explicit instead of being decided during implementation.
- This slice must not imply legal compliance, accreditation, MDE approval, state approval, or legal certification.

## Slice Goal

Add parent/admin controls for assignment attempt policy and submission clearing, then add explicit completion status across lessons, modules, and courses.

The result should let the parent/admin decide whether an assignment allows one attempt or multiple attempts, clear an active submission state when appropriate, and track lesson/module/course workflow completion without turning completion into grades, credits, GPA, transcripts, report cards, or diploma readiness.

## In Scope

- Assignment-level attempt policy: single attempt or multiple attempts.
- Parent/admin assignment editor controls for attempt policy.
- Student portal behavior that respects attempt policy before showing submit/resubmit actions.
- Parent/admin clearing of a submission from active review/student-facing state.
- Retained audit/history for cleared submissions when files or reviewed records exist.
- Explicit lesson completion status.
- Explicit module completion status.
- Explicit course completion status/readiness flag separate from credit award.
- Admin and student portal read models that show completion status in plain language.
- Tests for role boundaries, attempt policy, clearing behavior, persistence, and completion status separation from grades/credits.

## Out Of Scope

- Automatic final grade calculation.
- GPA calculation.
- Credit awards.
- Transcript or report-card generation.
- Diploma or graduation-plan readiness.
- Automatic portfolio artifact creation.
- Student editing of lesson, module, course, grade, credit, graduation, backup, restore, or admin settings.
- Hard deletion of submitted files or reviewed evidence as part of clearing.

## Design Direction

### Attempt Policy

Each assignment should have an explicit attempt policy:

- `SingleAttempt`: the student may submit once unless the parent/admin clears or returns the submission.
- `MultipleAttempts`: the student may submit additional attempts according to parent/admin policy.

Attempt policy controls student submission availability only. It must not imply grading, completion, or evidence acceptance.

### Submission Clearing

Clearing is a parent/admin action that removes the active submission from the student's current workflow and review queue. It should preserve records needed for parent-owned history:

- Cleared submissions should move to a retained cleared/archive state.
- Attachments and metadata should remain linked unless a separate future retention policy authorizes deletion.
- Clearing must not delete assessment records, accepted evidence, portfolio candidates, grades, credits, or course completion.
- Clearing should require explicit confirmation and plain-language warning text.

### Completion Status

Completion status is workflow progress, not academic credit.

Recommended status values:

- `NotStarted`
- `InProgress`
- `Completed`
- `NeedsReview`
- `Skipped`

Lesson completion may support module completion summaries. Module completion may support course completion readiness. None of these states may award credit, create a final grade, calculate GPA, generate official records, or satisfy graduation standards without later explicit parent/admin workflows.

## Parent/Admin Experience

Parent/admin should be able to:

- Set an assignment to single-attempt or multiple-attempt mode.
- See each assignment's current attempt policy in assignment lists and edit pages.
- Clear active submitted work with confirmation.
- See cleared submission history where relevant.
- Mark lessons complete, not started, in progress, needs review, or skipped.
- Mark modules complete after reviewing lessons and assignments.
- Mark courses complete/readiness status separately from credit award.
- See clear warnings that completion is not a grade, credit, transcript line, report card, GPA, or graduation decision.

## Student Experience

Student portal should:

- Show whether an assignment accepts one attempt or multiple attempts.
- Hide or disable submission controls when a single-attempt submission is already active and not returned or cleared.
- Allow another submission when multiple attempts are allowed or when parent/admin clearing/return rules permit it.
- Show lesson and module completion status in plain language.
- Avoid admin controls, clearing controls, grade controls, credit controls, official record controls, backup controls, and settings links.

## Phases

### Phase 1 - Contract And Documentation Pass

Build:

- Reconfirm all related docs listed above.
- Define exact attempt policy, clearing, and completion status terminology.
- Identify docs and context packs that need updates during implementation.
- Confirm clearing behavior preserves parent-owned records and local-first storage.

Exit criteria:

- No conflict with assessment, credit, graduation, identity, file, or curriculum rules.
- Clearing is retention-safe and parent/admin-only.
- Completion remains workflow status, not academic credit.

Verification:

- Documentation review against `docs/README.md`.
- Change-impact review for assignment/submission/files, grading/GPA, credits/course completion, UI workflows, and auth/student access.

### Phase 2 - Domain And Application Contracts

Build:

- Add assignment attempt policy value/state to the assignment model.
- Add submission clearing command and explicit cleared/archive state.
- Add lesson, module, and course completion status commands.
- Add read models for attempt policy, active attempt availability, cleared history, and completion summaries.
- Enforce parent/admin-only mutation for clearing and completion status changes.

Exit criteria:

- Student can submit only through the true student portal submission contract.
- Student cannot clear submissions or mark course/module/lesson completion through admin contracts.
- Assignment attempt policy controls submission availability without creating grades or completion.

Verification:

- Contract tests for parent/admin and student permissions.
- Tests proving attempt policy does not create grades, credits, evidence, or completion.

### Phase 3 - Persistence And Migration

Build:

- Extend local persistence for attempt policy, cleared submission state, and completion statuses.
- Default existing assignments to a conservative attempt policy.
- Default existing lessons/modules/courses to explicit non-complete states.
- Preserve existing submissions, attachments, evidence, and assessment records.

Exit criteria:

- Existing data opens without destructive changes.
- New policy/status data survives restart.
- Cleared submissions remain traceable.

Verification:

- Persistence round-trip tests.
- Backward-compatibility tests with representative existing data.

### Phase 4 - Parent/Admin Assignment Attempt UI

Build:

- Add single/multiple attempt controls to assignment edit pages.
- Show attempt policy in assignment lists or detail summaries where useful.
- Add validation and save feedback using existing UI styling.

Exit criteria:

- Parent/admin can configure attempt policy clearly.
- Attempt policy is visible before review or student submission confusion occurs.
- UI does not imply grading or completion.

Verification:

- Application tests for update commands.
- Browser smoke check for assignment edit save and display.

### Phase 5 - Student Portal Attempt Behavior

Build:

- Update student assignment submission panel to honor attempt policy.
- Show active attempt status and resubmit availability.
- Allow returned or cleared work to become actionable according to policy.
- Keep admin preview free of real submission controls.

Exit criteria:

- Single-attempt assignments do not show an extra submit action while an active submission exists.
- Multiple-attempt assignments allow additional attempts as configured.
- Student cannot bypass policy with direct UI actions.

Verification:

- Student portal smoke checks for no submission, submitted, returned, cleared, single-attempt, and multiple-attempt states.
- Permission/contract tests for blocked student mutations.

### Phase 6 - Parent/Admin Submission Clearing UI

Build:

- Add clear-submission action to the parent/admin submission review surface.
- Require confirmation with plain warning that clearing does not delete retained records or files.
- Show cleared state and history where relevant.
- Update review queues and dashboard counts to exclude cleared active work while retaining history.

Exit criteria:

- Parent/admin can clear an active submission safely.
- Cleared work no longer blocks the student workflow when policy permits resubmission.
- Cleared work is not lost from retained records.

Verification:

- Contract tests for clear action, state transitions, and retained attachments.
- Browser smoke check for clear confirmation and post-clear student behavior.

### Phase 7 - Lesson And Module Completion UI

Build:

- Add parent/admin controls for lesson completion status.
- Add parent/admin controls for module completion status.
- Show completion indicators in course/module/lesson pages.
- Show student-facing status without admin mutation controls.

Exit criteria:

- Parent/admin can mark lesson and module status explicitly.
- Student can see status where helpful but cannot change parent-owned completion records.
- Completion does not create grades, evidence, credits, GPA, or official records.

Verification:

- Contract tests for lesson/module status commands.
- Browser smoke checks for admin and student views.

### Phase 8 - Course Completion Status UI

Build:

- Add course completion status/readiness controls separate from credit award.
- Show course completion status on course list/detail/dashboard where existing data supports it.
- Add warning copy that course completion is not credit award or transcript generation.

Exit criteria:

- Parent/admin can mark course completion/readiness explicitly.
- Course completion is traceable and separate from final grade and credit.
- No credit award, GPA, transcript, report card, or diploma behavior is introduced.

Verification:

- Contract tests proving course completion does not award credit.
- UI smoke checks for course status display and update.

### Phase 9 - Dashboard And Summary Integration

Build:

- Add dashboard summaries for active submissions, cleared submissions where useful, lesson/module completion counts, and course completion status.
- Use only real data, with placeholders where functionality is intentionally not built yet.
- Link summaries to filtered admin views when routes exist.

Exit criteria:

- Dashboard helps parent/admin see workflow status without inventing academic progress.
- Counts do not imply grades, credits, GPA, or graduation readiness.

Verification:

- Summary-count tests where practical.
- Browser smoke check for dashboard states.

### Phase 10 - Documentation And Context Updates

Build:

- Update canonical docs if attempt policy, clearing, or completion terminology changes.
- Update context packs only when summaries need to route future work differently.
- Update roadmaps/index references as needed.

Exit criteria:

- Docs match implemented behavior.
- Context packs remain under the 200-line limit.
- Deferred decisions remain listed.

Verification:

- Documentation checklist review.
- Context-pack line-count check if edited.

### Phase 11 - Final Verification And Handoff

Build:

- Run focused domain, application, persistence, and UI checks.
- Verify parent/admin and student role boundaries.
- Verify both parent/admin and true student portal flows.
- Prepare concise release notes or handoff notes.

Exit criteria:

- Attempt policy, clearing, and completion status work end to end.
- Student portal respects configured attempt policy.
- Parent/admin can clear submissions and update completion states.
- No grade, credit, GPA, transcript, report-card, diploma, backup, restore, or graduation behavior is introduced accidentally.

Verification:

- Automated test suite or focused project tests.
- Browser smoke checks for admin assignment edit, submission review clearing, student submission availability, lesson/module status, and course completion status.

## Deferred Decisions

These decisions are intentionally not resolved by this roadmap:

- Final grade calculation.
- GPA calculation.
- Credit award workflow.
- Transcript and report-card generation.
- Diploma or graduation-plan readiness.
- Automatic completion from submitted work, accepted evidence, or assessment records.
- Permanent deletion policy for student submissions and attachments.
- Whether completion can later be promoted into portfolio artifacts or official records.
- Whether multi-attempt assignments should support maximum attempt counts beyond single vs. multiple.

## Recommended Next Vertical Slice

After this roadmap is complete, the next best slice is final course-grade review and credit award readiness. That later slice can consume explicit completion status and assessment records while keeping credit awards, GPA, transcripts, and graduation decisions separate.
