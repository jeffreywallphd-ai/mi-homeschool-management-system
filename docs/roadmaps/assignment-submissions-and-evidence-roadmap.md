# Assignment Submissions and Evidence Roadmap

Status: accepted  
Last reviewed: 2026-06-11  
Canonical for: phased implementation of student assignment submissions, parent/admin review, local file attachments, and parent-owned evidence records.

## Related Documentation

- `../README.md`
- `../context/packs/index.pack.md`
- `../context/packs/assessment-credits-graduation.pack.md`
- `../context/packs/portfolio-and-files.pack.md`
- `../context/packs/local-data-backup-restore.pack.md`
- `../context/packs/documentation-and-adr-governance.pack.md`
- `../context/prompt-routing.md`
- `../standards/change-impact-matrix.md`
- `../standards/documentation-standards.md`
- `../standards/security-and-privacy-standards.md`
- `../domain/assessment-and-grading-rules.md`
- `../domain/portfolio-evidence-rules.md`
- `../domain/file-and-artifact-taxonomy.md`
- `../architecture/local-data-and-file-storage.md`
- `course-module-assignments-roadmap.md`
- `student-course-client-roadmap.md`

## Documentation Governance Alignment

This roadmap follows the repository guidance in `docs/README.md`:

- Accepted ADRs and canonical docs remain the source of truth; context packs only summarize them.
- Code, tests, docs, and context packs must be updated together when this slice changes behavior or terminology.
- Deferred decisions must stay explicit instead of being decided implicitly during implementation.
- Any context-pack edits must keep each `docs/context/packs/` file under 200 physical lines.
- Legal-facing wording must not imply Michigan legal compliance, MDE approval, state approval, accreditation, or legal certification.

## Slice Goal

Add a complete vertical slice that lets a student submit assignment work from the true student portal and lets the parent/admin review that submitted work as a parent-owned local record.

This slice must preserve the separation between:

- Parent/admin student preview: read-only copy of courses, modules, lessons, and assignments inside the admin build.
- True student portal: student-facing build where actual student submissions can be created.

The result should make submitted work usable as reviewed educational evidence without turning it into grades, credits, transcripts, diplomas, or automatic portfolio artifacts.

## In Scope

- Student submission flow in the true student portal only.
- Text responses or notes for an assignment submission.
- File attachments stored locally with required metadata and checksums.
- Parent/admin submission inbox or review surface.
- Parent/admin review states, notes, and return/accept actions.
- Evidence records connected to student, course, module, lesson when applicable, assignment, submission, and stored files.
- Portfolio-candidate marking that does not automatically create a portfolio artifact.
- Dashboard touchpoints that surface existing counts such as students, assigned courses, and submissions needing review.
- Tests for application contracts, permission boundaries, persistence, and file metadata behavior.

## Out Of Scope

- Grade calculation, GPA, credit award, transcript generation, report cards, or diploma generation.
- Student editing of grades, credits, graduation plans, official records, backups, restore, or admin settings.
- Automatic portfolio artifact creation.
- Cloud storage, sync folders, external uploads, or network file persistence.
- Automatic backup scheduling, encryption-at-rest policy, or cloud backup decisions.
- Legal compliance claims or state approval wording.
- A full rubric or gradebook scoring model.

## Design Direction

### Object Model

Use clear separation between submitted content, stored files, evidence records, and later portfolio artifacts.

- `AssignmentSubmission`: the student's work package for a specific assignment.
- `StoredFile`: local file content plus metadata. Paths are storage details, not domain identity.
- `EvidenceRecord`: parent-owned record that summarizes reviewed student work and links to submissions and files.
- `PortfolioArtifact`: later curated portfolio item. This slice may mark evidence as a portfolio candidate, but must not create one automatically.

### Submission Statuses

Statuses describe workflow, not academic performance.

- `Draft`: local student work in progress, if drafts are supported in the implementation.
- `Submitted`: work is ready for parent/admin review.
- `Returned`: parent/admin returned the work with notes.
- `Accepted`: parent/admin accepted the work as reviewed evidence.
- `Archived`: retained historical submission no longer active for review.

Missing, returned, or unreviewed submissions must not be treated as zero, pass, fail, or complete for grading or credit purposes.

### Required File Metadata

Every stored attachment must include:

- Stable file id.
- Category.
- Original filename.
- Stored path.
- Content type.
- Size.
- Checksum.
- Created timestamp.
- Owning student id.
- Related submission id.

## Student Portal Experience

The true student portal should add submission controls directly to the assignment experience:

- Assignment details remain the primary focus.
- A submit-work panel includes text response, optional notes, attachment picker, attachment list, and submit action.
- Submitted work displays status, submitted timestamp, parent/admin feedback when available, and returned-work guidance.
- No admin controls, grading controls, official record controls, backup controls, or settings links are exposed.
- Parent/admin preview routes inside the admin build must not show working submission controls.

Visual treatment should match the redesigned UI language: restrained panels, clear hierarchy, status badges, compact attachment rows, and icon-led actions from the attached design skill.

## Parent/Admin Experience

Parent/admin should get a review workflow without losing the existing course and assignment structure.

- Home dashboard: show students, assigned courses, recent activity placeholders where needed, and submissions needing review when available.
- Course/module/assignment pages: show whether an assignment has submitted work and review status.
- Submission review page or panel: show student, course, assignment, submitted response, attachments, timestamps, and parent review notes.
- Review actions: return to student, accept as reviewed evidence, and optionally mark as portfolio candidate.
- Evidence view: show accepted evidence records with links back to assignment, submission, and stored files.

The admin student preview remains an admin-side copy of course/module/lesson/assignment content only. It should not expose real submissions, submission history, or student-created files.

## Storage And Persistence Design

This slice must remain local-first under the established app data location. Do not introduce cloud storage or sync-folder behavior.

Recommended local path shape:

```text
files/students/{studentId}/submissions/{submissionId}/{storedFileId}.{extension}
```

Implementation details:

- Generate safe stored filenames independent of the original filename.
- Preserve original filename in metadata.
- Compute and store checksums at attachment time.
- Use a temporary-write then commit pattern so failed saves do not leave orphaned domain records.
- If a file save succeeds but metadata persistence fails, mark the file for cleanup or complete cleanup immediately.
- Never log file contents, student submission text, private notes, grades, or sensitive student details.

## Phases

### Phase 1 - Contract And Documentation Pass

Build:

- Reconfirm the canonical docs listed above.
- Define the exact submission, stored file, evidence, and review status contracts.
- Identify docs and context packs that must be updated with the code change.

Exit criteria:

- No open contradiction with accepted ADRs or canonical docs.
- Deferred decisions are listed instead of decided implicitly.
- The slice boundary remains submission/evidence, not grading or portfolio generation.

Verification:

- Documentation review against `docs/README.md`.
- Change-impact review using `docs/standards/change-impact-matrix.md`.

### Phase 2 - Domain And Application Contracts

Build:

- Add domain models or records for submissions, review status, and evidence records.
- Add application commands and queries for submit, return, accept, list pending reviews, and list evidence.
- Enforce that student commands cannot modify parent-owned academic records.
- Enforce that admin preview cannot create or mutate real submissions.

Exit criteria:

- Student role can create submission records only through the true student portal contract.
- Parent/admin remains the only role that can accept evidence or write review notes.
- Assignment status and submission status remain separate from grades.

Verification:

- Contract tests for allowed and denied commands.
- Tests proving returned or missing work does not create grades or credits.

### Phase 3 - Local File Attachment Storage

Build:

- Add attachment storage using generated stored filenames.
- Record required metadata and checksum for each attachment.
- Validate file type, size, and empty-file behavior according to existing standards.
- Add failure handling for partial writes.

Exit criteria:

- Each stored attachment has complete metadata.
- Original filenames are display metadata only.
- Domain identity does not depend on file paths.

Verification:

- Tests for checksum creation, metadata persistence, safe path generation, and orphan prevention.
- Tests for invalid file and failed-write behavior.

### Phase 4 - Persistence And Migration

Build:

- Extend the local persistence shape for submissions, evidence records, and stored file metadata.
- Preserve existing course, module, lesson, and assignment data.
- Add any required versioning or migration logic.

Exit criteria:

- Existing data opens without destructive changes.
- New submission data survives app restart.
- Attachments remain linked after restart.

Verification:

- Persistence round-trip tests.
- Migration/backward-compatibility tests using representative seeded data.

### Phase 5 - Student Portal Submission UI

Build:

- Add submit-work UI to the true student portal assignment view.
- Show submission status, returned notes, accepted state, and attachment list.
- Keep submission routes and actions out of the admin preview.
- Keep UI styling consistent with the existing redesign.

Exit criteria:

- Student can submit work on the wifi-accessible student build.
- Student cannot reach admin pages or parent-owned record actions from the student build.
- Admin preview remains read-only course content.

Verification:

- Browser smoke test for true student submission.
- Browser smoke test confirming admin preview does not expose submission actions.
- Permission tests for blocked student actions.

### Phase 6 - Parent/Admin Review UI

Build:

- Add submissions-needing-review entry points to the parent/admin dashboard.
- Add review surface showing response text, files, assignment context, and student context.
- Add return and accept-as-evidence actions.
- Add visual status indicators across dashboard, course, module, and assignment views.

Exit criteria:

- Parent/admin can review submitted work without changing grade, credit, transcript, or diploma state.
- Accepted work creates or updates an evidence record.
- Returned work is visible to the student with parent/admin notes.

Verification:

- Browser smoke test for review, return, and accept flows.
- Contract tests proving accepted evidence does not create grades, credits, or portfolio artifacts.

### Phase 7 - Evidence And Portfolio Bridge

Build:

- Add evidence-list views or filters for accepted assignment evidence.
- Add portfolio-candidate flag where useful.
- Keep portfolio artifact creation as a separate explicit parent/admin action for a later slice.

Exit criteria:

- Evidence records preserve assignment context, file links, and review metadata.
- Portfolio-candidate marking is visible but does not create artifacts automatically.

Verification:

- Tests for evidence creation and metadata.
- Tests proving no automatic portfolio artifact is created.

### Phase 8 - Dashboard Integration

Build:

- Add dashboard summaries using existing data only.
- Show counts for students, assigned courses, pending reviews, and recent reviewed evidence where available.
- Use placeholders only where the underlying data does not exist yet.

Exit criteria:

- Dashboard is useful without inventing progress or completion behavior.
- Placeholder sections are visually clear but do not imply computed academic status.

Verification:

- Browser smoke test for dashboard states with no submissions, pending submissions, and accepted evidence.

### Phase 9 - Documentation And Context Updates

Build:

- Update canonical docs touched by this slice.
- Update context packs only when their summaries need to change.
- Add or update any user-facing start instructions if the student portal command or port changes.

Exit criteria:

- Docs reflect the implemented behavior.
- Context packs remain compact and under the 200-line limit.
- No README guidance is bypassed.

Verification:

- Documentation checklist from `docs/standards/documentation-standards.md`.
- Line-count check for edited context packs.

### Phase 10 - Final Verification And Release Notes

Build:

- Run focused tests for domain, application, persistence, and UI smoke coverage.
- Verify both builds start independently with the intended host/port behavior.
- Prepare a concise handoff note.

Exit criteria:

- Parent/admin area remains localhost-only.
- True student portal remains wifi-accessible on its separate port.
- Both builds share the same local core data source without sharing admin routes.
- No unhandled Blazor error appears in normal dashboard, preview, submission, or review flows.

Verification:

- Automated test suite or focused project tests.
- Manual browser verification for admin, admin preview, student portal, submission, and review.
- Host binding check for admin and student build start commands.

## Deferred Decisions

These decisions are intentionally not resolved by this roadmap:

- Final gradebook scoring and rubric model.
- Whether accepted evidence can later be promoted to a portfolio artifact from a dedicated portfolio workflow.
- Whether assignment completion should contribute to module, course, credit, transcript, or graduation-plan progress.
- Encryption-at-rest policy.
- Automatic backup scheduling.
- Cloud sync or external storage.
- Large media handling, transcoding, preview generation, and long-term media retention policy.

## Recommended Next Vertical Slice

After this roadmap is complete, the next best slice is a parent/admin gradebook and assessment slice that consumes reviewed assignments and evidence without weakening parent control over grades, credits, transcripts, or graduation standards.
