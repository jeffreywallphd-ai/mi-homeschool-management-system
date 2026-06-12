# Portfolio Evidence Rules

- Status: accepted
- Last reviewed: 2026-06-12
- Canonical for: portfolio artifacts, evidence tags, collections, and exports
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [Homesteading Portfolio Use Cases](../product/homesteading-portfolio-use-cases.md), [File and Artifact Taxonomy](file-and-artifact-taxonomy.md)
- Related tests: `src/HomeschoolManager.Tests/Program.cs`
- Supersedes: none

## Portfolio Purpose

The portfolio preserves selected evidence of learning, especially work that is richer than a gradebook entry: projects, writing, practical work, photos, tests, fieldwork, reading, external learning, and parent evaluations.

## Artifact Metadata

Portfolio artifacts should include:

- Student.
- Title.
- Description.
- Date or date range.
- Artifact type.
- Course or subject context.
- Evidence tags.
- Attached file references where applicable.
- Export selection status.

## Evidence Tags

Evidence tags may represent:

- Requirement areas.
- Courses.
- Skills.
- Portfolio categories.
- Graduation packet inclusion.

## Collections

Collections group artifacts for review or export. Examples:

- Senior-year portfolio.
- Science evidence.
- Writing samples.
- Homesteading projects.
- Graduation packet selections.

## Portfolio Authorship by Grade Band

The portfolio is not parent-only for middle-school and high-school students. For grade 6 and above, the student should be the primary author and organizer of the working portfolio design, while the parent provides guidance, suggestions, and final approval before the portfolio is treated as reviewed family-issued output.

For K-5 students, the parent may retain full control of portfolio selection, organization, text, and export decisions.

For grade 6 and above, student portfolio authoring may include:

- Suggesting assignments or accepted evidence for inclusion.
- Creating, renaming, reordering, and removing custom portfolio sections before approval.
- Writing section headings, section introductions, and portfolio narrative text.
- Completing guided text prompts such as "This portfolio shows the student's..."
- Choosing which accepted evidence or assignment-derived artifacts belong in each section.
- Reordering portfolio items and adding student reflection or explanation text.
- Submitting the portfolio design for parent review.

Student portfolio authoring must not change grades, credits, assessments, evidence-record facts, transcripts, diplomas, report cards, backup/restore settings, or generated official packet history.

## Parent Review and Approval

Parent/admin review remains required before the working portfolio design becomes an approved portfolio packet or graduation/archive packet component.

For grade 6 and above, the parent/admin role is guidance and final approval. The parent/admin should be able to review the student's section structure, item placement, narrative text, and selected evidence; leave section-level and item-level suggestions; make edits when needed; request revision; and approve the portfolio for preview/export.

For K-5 students, the parent/admin may author and control the portfolio directly.

## Assignment Candidates

Assignments may be marked as portfolio candidates when their expected output is likely to become useful evidence. That marker does not create a portfolio artifact by itself.

Creating reviewed portfolio evidence should remain separate from assignment planning so actual work, context, date, files, tags, and export selection are confirmed before final approval.

## Student Portfolio Drafts

Students may curate portfolio entries from already accepted evidence in the true student portal. Student-controlled metadata may include display title, section, reflection, reason for choosing the work, skills shown, design sort order, and whether the item should be included in the current portfolio design.

Student portfolio authoring does not create grades, credits, transcripts, diplomas, report cards, or export packets. It references existing accepted evidence records and must not duplicate files or bypass the evidence review workflow.

Parent/admin review is required before a student-authored portfolio design is treated as approved for a reviewed portfolio packet. Parent/admin may approve, edit, suggest revision, request revision, or exclude items from the reviewed portfolio set.

## Export Rule

Portfolio export must preserve artifact context, not only files. Each exported artifact should have enough metadata for a future reader to understand what it is evidence of.

Official portfolio exports are created from parent/admin-approved snapshots, not from the mutable working design. Later edits to the working design must not silently alter a prior approved snapshot or an archive packet generated from that snapshot.

The approved portfolio archive packet should include:

- A printable portfolio report.
- Structured and human-readable manifests.
- Section, item, course, module, assignment, reflection, parent-note, skill, file, and checksum metadata where available.
- Accepted evidence files when the stored files are present.
- Plain warnings for missing evidence files.

Student access may show an approved portfolio preview, but the student role must not create official portfolio exports or archive packets.
