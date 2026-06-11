# Student Course Client Roadmap

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: phased implementation plan for the student-facing course and module client
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Curriculum Planning Rules](../domain/curriculum-planning-rules.md), [Course Pack Rules](../domain/course-pack-rules.md), [Assessment and Grading Rules](../domain/assessment-and-grading-rules.md), [Blazor UI Pack](../context/packs/blazor-ui.pack.md)
- Related tests: `HomeschoolManager.Tests`
- Supersedes: none

## Slice Goal

Add a student-facing client that lets the student view parent/admin-created courses, course expectations, syllabi, and module learning materials in read-only form.

The student client should help the parent see what the administrative course/module setup looks like from the student's side, while preserving tight role boundaries.

The later accepted [Assignment Submissions and Evidence Roadmap](assignment-submissions-and-evidence-roadmap.md) extends the true student portal with assignment submission commands. That later slice does not change this roadmap's parent/admin preview boundary: preview remains read-only course, module, lesson, and assignment content.

## In Scope

- Student landing page listing all courses.
- Current grade display on the student landing page.
- Read-only student course landing page.
- Read-only course syllabus page compiled from course detail fields.
- Read-only student module detail page.
- Student navigation to courses, syllabi, and modules.
- Student-only routes and application read models.
- Parent/admin preview path if useful for review.
- Responsive layouts for mobile, tablet, and laptop/PC screens.

## Out of Scope

- Gradebook scoring implementation.
- Assignment submission workflow, now covered by the later assignment submissions roadmap.
- Attendance or activity logging.
- File evidence upload by the student, now covered only for assignment submissions by the later assignment submissions roadmap.
- Parent feedback, grade finalization, credit awards, transcripts, or report cards.
- Student mutation of courses, modules, mappings, grades, credits, or setup.

## Current Grade Constraint

The gradebook is not implemented yet. The first student landing page should expose a current-grade display slot and show a clear placeholder such as "Not recorded" or "No grade yet" until a gradebook service exists.

Do not infer grades from course progress, module completion, or planned credits.

## Phase 1: Student Read Contracts

### Build

- Add student-facing course list read model.
- Add student-facing course detail read model.
- Add student-facing syllabus read model.
- Add student-facing module read model.
- Include current-grade display field as nullable or display-only text.
- Include module resource items, objective links, instructions, evidence placeholders, and status.
- Reuse parent-owned course/module source data without exposing admin-only fields.

### Exit Criteria

- Student read models are complete and non-null-safe.
- Student read models do not expose requirement mapping editing or admin mutation fields.
- Current grade field is explicit and can safely display no grade.

### Verification

- Application tests cover student read access.
- Application tests confirm student mutation remains denied.
- Tests confirm no grade is inferred before gradebook exists.

## Phase 2: Student Navigation and Access Boundary

### Build

- Add student navigation entries only after student login.
- Route student startup to the student landing page.
- Keep parent/admin navigation separate from student navigation.
- Ensure student routes are reachable by student sessions and read-only for parent preview if implemented.
- Ensure logged-out users still only see Login.

### Exit Criteria

- Student can reach only student-facing course/module/syllabus pages.
- Parent/admin-only pages do not appear in student navigation.
- Direct access to admin mutation routes remains blocked by application authorization.

### Verification

- Manual navigation review.
- Tests for session role routing where practical.
- Browser smoke check when browser tooling is available.

## Phase 3: Student Landing Page

### Build

- Create a student home or dashboard page.
- Show all current courses as readable rows/cards.
- For each course show title, duration, planned credits, current grade, and module progress summary.
- Make each course row/card clickable.
- Use compact, scannable layout suitable for daily student use.

### Exit Criteria

- Student can see every course created/imported by parent/admin.
- Current grade displays clearly as actual grade or "No grade yet."
- Student can open a course from the landing page.

### Verification

- UI review for no admin controls.
- Tests for course list content.
- Empty-state review when no courses exist.

## Phase 4: Student Course Landing Page

### Build

- Create read-only course landing page.
- Show course title, description, duration, credits, and learning objectives.
- Include prominent link to the course syllabus.
- Include module links in sequence order.
- Show module status and semester placement where available.

### Exit Criteria

- Student can understand course purpose and expectations at a glance.
- Student can open the syllabus.
- Student can open each module.

### Verification

- UI review with imported default-pack courses.
- Tests confirm course objectives and module links are included.
- Accessibility check for link labels and headings.

## Phase 5: Student Course Syllabus Page

### Build

- Compile read-only syllabus from course detail data.
- Include course identity, description, instructional methods, texts/resources, assessment methods, grading basis, curriculum goals, learning objectives, planned sequence, parent notes if student-appropriate, and module outline.
- Exclude admin-only requirement mappings unless a future decision makes them student-facing.
- Use family-owned wording; do not imply accreditation, state approval, or legal certification.

### Exit Criteria

- Syllabus explains course purpose, materials, expectations, assessment, and grading basis.
- Syllabus is read-only and student-safe.
- Missing optional fields are handled gracefully.

### Verification

- Tests for syllabus model completeness.
- UI review for long text and itemized resources/objectives.
- Legal-language boundary scan.

## Phase 6: Student Module Page

### Build

- Create read-only module page.
- Show module title, description, semester, estimated length, status, instructions, objectives, linked course objectives, concrete resources, and assignment/evidence placeholder.
- Render links, physical resources, and uploaded-file references clearly.
- Provide navigation back to course and next/previous module links.

### Exit Criteria

- Student can use the module page to progress through learning materials.
- Module page has no edit, reorder, delete, mapping, or admin controls.
- Resources are clear enough for the student to act on.

### Verification

- Tests for module read model and resource rendering data.
- UI review for modules with links, physical resources, and no resources.
- Browser smoke check when available.

## Phase 7: Parent/Admin Preview Support

### Build

- Add a parent/admin link or route to preview student view.
- Preview should use the same student read models and pages where possible.
- Clearly label preview mode if parent/admin is viewing it.

### Exit Criteria

- Parent can inspect the student experience without switching accounts if preview is implemented.
- Preview does not bypass student read-model boundaries.

### Verification

- Manual review of parent preview route.
- Tests confirm preview does not expose mutation controls.

## Phase 8: Responsive Layout and Device Review

### Build

- Review student dashboard, course, syllabus, and module pages at mobile, tablet, and laptop/PC sizes.
- Use single-column layouts on narrow screens and two-column layouts only when space supports them.
- Keep cards, links, and navigation targets usable by touch.
- Ensure long titles, descriptions, resources, and objectives wrap without overlapping controls.
- Keep student pages readable without hidden admin-only controls.

### Exit Criteria

- Student pages work on mobile, tablet, and laptop/PC screens.
- Page columns collapse cleanly before content becomes cramped.
- Tap targets, course cards, syllabus links, module links, and previous/next module links remain easy to use.

### Verification

- Browser/device smoke check when browser tooling is available.
- CSS review for responsive breakpoints and overflow risk.
- Manual layout review using realistic default-pack course/module content.

## Phase 9: Closeout

### Build

- Update docs and context packs for student-client rules.
- Keep context packs under 200 lines.
- Add or update tests for read models, access boundaries, and student route assumptions.
- Record deferred gradebook, assignment, and feedback decisions.

### Exit Criteria

- Build passes.
- Tests pass.
- Student pages are read-only and role-safe.
- Current-grade placeholder is clearly documented until gradebook exists.
- No conflicts with identity/access, curriculum, legal-language, or UI guidance.

### Verification

- Build and app tests run.
- Context pack line-count check runs.
- Browser smoke check attempted.
- Final report names changed files, tests run, and known limitations.

## Deferred Decisions

- Exact gradebook model and how current grades are calculated.
- Assignment submission and evidence workflow.
- Whether students may mark module resources complete.
- Whether parent notes should always be student-facing or marked with a visibility setting.
- Whether course syllabus should later be exportable as a generated document.
