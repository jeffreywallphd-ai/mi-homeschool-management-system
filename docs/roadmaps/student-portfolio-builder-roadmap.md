# Student Portfolio Creation and Preview Roadmap

- Status: implemented through the local creation/preview/export slice
- Last reviewed: 2026-06-12
- Canonical for: portfolio creation, parent review, preview, approved-snapshot export, and archive-packet preparation
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Portfolio Evidence Rules](../domain/portfolio-evidence-rules.md), [Official Records Rules](../domain/official-records-rules.md), [User and Household Model](../product/user-and-household-model.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Portfolio and Files Pack](../context/packs/portfolio-and-files.pack.md)
- Related tests: `src/HomeschoolManager.Tests/Program.cs`
- Supersedes: earlier student-curated portfolio draft roadmap scope in this file

## Purpose

Build a portfolio creation workflow where middle-school and high-school students can author the working official portfolio design, while the parent provides guidance and final approval. For K-5 students, the parent may author and control the portfolio directly.

The roadmap should produce a local-first portfolio preview that can later become a reviewed portfolio packet or graduation/archive packet component without implying state approval, accreditation, or legal certification.

## Scope

- Grade-band-aware portfolio permissions.
- Student-authored portfolio workspace for grade 6 and above.
- Parent-authored portfolio workspace for K-5.
- Custom portfolio sections with headings, introductions, and sort order.
- Portfolio-level narrative prompts and free-text responses, including prompts such as "This portfolio shows the student's..."
- Assignment and accepted-evidence suggestion flow.
- Placement of selected assignments/evidence into custom sections.
- Student reflections, reasons for inclusion, and skills shown.
- Parent/admin review with section-level and item-level suggestion boxes.
- Parent/admin edits, revision requests, exclusions, and final approval.
- Portfolio preview that renders the current design before export.
- Approved-snapshot preview that remains distinct from later working edits.
- Parent/admin archive packet export from approved snapshots.
- Printable HTML, JSON manifest, Markdown manifest, and accepted evidence files in the archive packet.

## Non-Scope

- Student editing grades, credits, assessments, graduation plans, evidence-record facts, backup/restore, or admin settings.
- Student final approval or export of official family-issued packets.
- Automatic creation of portfolio evidence from planned assignments.
- Cloud sync, external sharing, or hosted review.
- Transcript, diploma, report-card, or GPA changes.
- Legal compliance certification language.

## Permission Model

### K-5

Parent/admin may create sections, write introductions, choose items, organize the portfolio, add narrative text, review, approve, and export.

Student access may view approved portfolio material when enabled, but direct authoring is optional and parent-controlled.

### Grade 6 And Above

Student may:

- Suggest assignments and accepted evidence.
- Create, rename, reorder, and remove working sections.
- Write section introductions and portfolio narrative text.
- Place accepted evidence or accepted assignment-derived work into sections.
- Reorder items.
- Add reflections, reasons for choosing work, and skills shown.
- Submit the portfolio design for parent review.

Parent/admin may:

- View and edit all student-authored portfolio content.
- Add section-level and item-level suggestions.
- Request revision with clear guidance.
- Exclude items from the approved set.
- Approve the design for preview/export.
- Own the final family-issued portfolio packet.
- Create the official approved portfolio archive packet.

Students may view the latest approved portfolio preview, but they do not create official exports or archive packets.

## Data Model Direction

Use explicit application contracts and keep source records separate:

- `PortfolioDesign`: student-owned or parent-owned working portfolio for one student and school year or packet purpose.
- `PortfolioSection`: custom heading, introduction, sort order, and review state.
- `PortfolioDesignItem`: reference to accepted evidence or accepted submission-derived work, section placement, display title, reflection, skills shown, and sort order.
- `PortfolioNarrative`: portfolio-level prompted text blocks and freeform text.
- `PortfolioSuggestion`: parent/admin suggestion tied to a design, section, narrative block, or item.
- `PortfolioApproval`: parent/admin approval snapshot with issue date, approver, and approved design version.
- `PortfolioExport`: generated archive packet created from a `PortfolioApproval` snapshot, with printable HTML, manifests, file metadata, warnings, and selected evidence-file content.

The implementation may adapt names to existing domain conventions, but it should preserve the distinction between accepted evidence, student-authored design, parent suggestions, and approved/exported packet output.

## Implemented Export Slice

The approved portfolio export slice creates a local ZIP archive from an approved snapshot only. Export does not read from the mutable working design except to locate the requested approved snapshot.

Archive packet contents:

- `portfolio.html`: printable portfolio report with cover, narrative text, table of contents, sections, items, reflections, parent notes, skills, source context, and file links.
- `manifest.json`: structured manifest with schema, student, snapshot, section, item, file, checksum, and warning metadata.
- `manifest.md`: human-readable manifest for family archive review.
- `evidence-files/`: accepted evidence files copied from stored submission files when present.

Guardrails:

- Parent/admin role creates the official export.
- Student role may view the approved preview but cannot create the official archive packet.
- Later student or parent edits return the current design to Working and do not change prior approved snapshots or exports based on them.
- Missing evidence files are reported in the manifest instead of being silently ignored.
- Export must not mutate grades, credits, assessments, evidence facts, course completion, transcripts, diplomas, report cards, backup settings, or current portfolio draft state.

## Design Direction

The portfolio builder should feel like a structured document editor, not a gradebook table. The first screen should be the working portfolio itself, with enough controls to organize and preview the report without sending the student through admin-style record screens.

### Student Builder Layout

Use a two-column workbench on desktop and a single-column stack on mobile.

Left/main column:

- Portfolio title and status.
- Portfolio narrative prompts.
- Ordered portfolio sections.
- Items inside each section.
- Inline controls to edit headings, introductions, reflections, and item placement.

Right/support column:

- Accepted evidence picker.
- Assignment suggestions waiting for evidence.
- Parent suggestions grouped by section or item.
- Preview and submit-for-review actions.

The student should always be able to answer: what is in my portfolio, what section is it in, what still needs attention, and what will my parent see?

### Student Builder Controls

Use clear controls rather than hidden drag-only behavior:

- Add Section button.
- Section title input.
- Section introduction textarea.
- Move Up and Move Down buttons for sections and items.
- Add Evidence button from the evidence picker.
- Suggested Section dropdown or menu for placing an item.
- Item display title input.
- Student reflection textarea.
- Skills shown textarea or chip-style entry.
- Remove from portfolio action that removes only the working design item, not the source evidence.
- Preview button.
- Submit for Parent Review button.

Drag and drop may be added later, but keyboard-accessible move controls should exist first.

### Parent Review Layout

The parent/admin review page should mirror the student builder closely so review happens in the same structure the student authored.

Left/main column:

- Portfolio approval status and student identity.
- Portfolio narrative.
- Sections and section introductions.
- Items with source context, submitted files, student reflections, and parent notes.
- Inline parent suggestion boxes at section and item level.

Right/support column:

- Review summary: sections, items, unresolved suggestions, excluded items.
- Preview approved candidate.
- Request Revision.
- Approve Portfolio Design.
- For K-5, authoring controls equivalent to the student builder.

The parent/admin should be able to leave guidance without losing student authorship. Direct edits are allowed, but suggestion boxes should be the default review path for grade 6 and above.

### Preview Design

Preview should render like a report packet, not like an editor:

- Cover/title block with student name, school year or portfolio purpose, and status.
- Portfolio narrative text.
- Table of contents when more than one section exists.
- Each section heading and introduction.
- Each item with display title, course/module/assignment context, date, student reflection, skills shown, and selected parent notes.
- File preview or file list for attached evidence.
- A small status label for working, needs revision, approval candidate, or approved snapshot.

The preview must not show editing controls, suggestion boxes, or admin-only details. The parent approval view may show a review banner above the preview, but the printable/exportable preview should stay clean.

### Review Statuses

Use plain statuses:

- Working.
- Submitted for review.
- Needs revision.
- Ready for approval.
- Approved.

At section and item level, use:

- No suggestions.
- Suggestions open.
- Resolved.
- Excluded.

Approval should freeze a snapshot. Later edits should move the design back to Working or create a new working version without changing the approved snapshot.

### Empty States and Starter Content

New portfolios should start with helpful structure, not a blank wall:

- A default title such as "High School Portfolio" or "Middle School Portfolio".
- A starter narrative prompt: "This portfolio shows the student's..."
- Starter sections such as Academic Work, Projects, Writing, Practical Skills, and Reflections, editable by the student or parent.
- A visible accepted-evidence picker with a message when no accepted evidence exists yet.

Starter sections are suggestions only and should be removable or renameable.

## Phase 1 - Align Existing Portfolio Workspace

- Rename UI language from narrow "draft item" wording to working portfolio design wording.
- Show student access to portfolio creation clearly in the student portal navigation.
- Show parent/admin review as guidance and final approval for grade 6 and above.
- Keep K-5 flows parent-controlled.
- Preserve current accepted-evidence selection behavior.

Exit criteria:

- Student can find the portfolio builder without relying on admin review pages.
- Parent review page names the workflow as portfolio design review, not only item approval.
- Tests confirm students cannot mutate grades, credits, assessments, evidence facts, or packet approval.

## Phase 2 - Sections and Narrative

- Add custom section creation, rename, reorder, introduction text, and remove/archive behavior.
- Add portfolio-level narrative prompts and text areas.
- Include starter prompts such as "This portfolio shows the student's..." with editable completion text.
- Add autosave or explicit save with plain status messaging.

Exit criteria:

- Grade 6+ student can create and organize sections.
- K-5 parent can perform the same actions in admin.
- Parent can view all section and narrative text during review.
- Validation blocks empty required headings before approval.

## Phase 3 - Item Placement and Assignment Suggestions

- Let students suggest assignments for the portfolio before submission or after accepted evidence exists.
- Let students place accepted evidence and accepted assignment-derived items into sections.
- Allow per-item display title, reflection, reason for inclusion, skills shown, and sort order.
- Keep planned assignment suggestions distinct from completed accepted evidence.

Exit criteria:

- Planned assignment suggestions do not appear as completed evidence.
- Accepted evidence can be placed in one or more appropriate portfolio sections if domain rules allow.
- Parent/admin sees source context, file count, submission preview link, and student explanation for each item.

## Phase 4 - Parent Suggestions and Review

- Add parent/admin suggestion boxes at portfolio, section, narrative, and item levels.
- Add review statuses such as needs revision, ready for approval, approved, and excluded.
- Let parent/admin edit text directly when appropriate and leave guidance without overwriting student text.
- Add student-facing revision view that groups parent suggestions by section.

Exit criteria:

- Parent can request revision with targeted comments.
- Student can see what needs work without seeing admin-only records.
- Parent approval remains required before preview/export is marked reviewed.

## Phase 5 - Portfolio Preview

- Render a local preview of the portfolio design.
- Include a cover/title block, portfolio narrative, sections in selected order, section introductions, and items with title, source context, student reflection, parent notes selected for inclusion, and file links or previews.
- Provide preview modes for student working view and parent approval view.
- Add print-friendly styling as a bridge to later PDF or packet export.

Exit criteria:

- Student can preview the working portfolio.
- Parent can preview the approval candidate.
- Preview labels clearly distinguish working, needs revision, and approved states.
- Preview does not imply legal compliance, accreditation, state approval, or MDE approval.

## Phase 6 - Approval Snapshot and Export Preparation

- Create parent/admin approval snapshot for the reviewed design.
- Record approved section order, item order, narrative text, included evidence references, and approval timestamp.
- Prepare export manifest metadata for later portfolio packet generation.
- Keep generated export files separate from source records.

Exit criteria:

- Parent approval freezes a versioned portfolio design snapshot.
- Later student edits create a new working version instead of silently changing the approved snapshot.
- Export preparation preserves evidence context and file references.

## Verification Gates

- Application tests for student, parent, and K-5/grade 6+ permission boundaries.
- Tests proving student portfolio authoring does not mutate grades, credits, assessments, evidence facts, official packet approval, or generated document history.
- Tests proving parent approval snapshots are versioned.
- UI checks for desktop and mobile student portfolio creation.
- UI checks for parent/admin review, suggestion boxes, and preview.
- Prohibited wording scan for compliance, accreditation, state approval, MDE approval, or legal certification claims.

## Portfolio Completion Vertical Slice

This slice should complete usable portfolio functionality, not only establish the first data model. It should deliver an end-to-end flow from evidence selection through student authorship, parent review, approval, and clean preview.

Implementation note, 2026-06-12: the local creation and preview slice is implemented in the application contracts and Blazor UI. Students in grade 6 and above can author the working portfolio design, sections, narratives, item placement, reflections, and planned assignment suggestions. Parent/admin review can edit, suggest, request revision, resolve suggestions, approve, and create versioned approval snapshots. K-5 portfolio editing remains parent/admin controlled. PDF generation, zip export, and graduation packet integration remain follow-on work.

### Slice Scope

- Grade-band-aware portfolio behavior for K-5 versus grade 6 and above.
- Student portal portfolio builder for grade 6 and above.
- Parent/admin portfolio builder for K-5.
- Portfolio title, purpose, and narrative prompts.
- Custom sections with heading, introduction, sort order, status, and parent suggestions.
- Accepted evidence picker.
- Assignment suggestion list that remains distinct from completed accepted evidence.
- Portfolio items assigned to sections.
- Item title, reflection, skills shown, source context, sort order, and include/exclude state.
- Parent/admin review with section-level and item-level suggestion boxes.
- Student revision view grouped by parent suggestions.
- Parent/admin request revision, mark suggestion resolved, exclude item, and approve design.
- Working preview for the student.
- Approval candidate preview for the parent/admin.
- Approved snapshot preview after final approval.

### Slice Non-Scope

- PDF generation.
- Zip export.
- Graduation packet integration.
- Drag-and-drop ordering.
- Rich text editing beyond plain text or simple multiline text.
- Cloud sharing or remote review.

These can follow once the complete local creation and approval loop is reliable.

### Slice Screen Set

- Student Portfolio Builder at `/student/portfolio`.
- Student Portfolio Preview from the builder.
- Parent Portfolio Review at `/portfolio`.
- Parent Portfolio Preview from review.
- K-5 Parent Portfolio Builder mode within the parent/admin portfolio area.

### Slice Acceptance Criteria

- Grade 6+ student can create a portfolio design with at least one custom section, narrative text, and one accepted evidence item.
- Grade 6+ student can suggest an assignment for future portfolio inclusion without creating completed evidence.
- Grade 6+ student can submit the portfolio design for parent review.
- Parent/admin can leave a suggestion on the portfolio narrative, a section, and an item.
- Student can see suggestions grouped by where they apply and revise the relevant text or placement.
- Parent/admin can approve the design once required text and at least one included item exist.
- Approved preview is readable as a portfolio report and does not show editing controls.
- K-5 student portfolio creation is parent/admin controlled.
- Student cannot approve, export, or mutate official packet history.
- Student portfolio authoring cannot alter grades, credits, assessments, evidence facts, submissions, stored files, or graduation records.
- Parent approval creates a versioned snapshot so later edits do not silently change the approved portfolio.

### Slice Verification

- Contract tests for grade-band permissions.
- Contract tests for section, narrative, item placement, suggestion, revision, and approval commands.
- Contract tests proving assignment suggestions do not create accepted evidence.
- Contract tests proving approval snapshots are immutable after later working edits.
- UI route/build checks for student and parent/admin pages.
- Browser checks for desktop and mobile portfolio builder and preview.

Completing this slice should leave only document-generation/export packaging as follow-on work. The portfolio itself should feel usable.
