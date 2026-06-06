# Portfolio Evidence Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: portfolio artifacts, evidence tags, collections, and exports
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [Homesteading Portfolio Use Cases](../product/homesteading-portfolio-use-cases.md), [File and Artifact Taxonomy](file-and-artifact-taxonomy.md)
- Related tests: not yet implemented
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

## Assignment Candidates

Assignments may be marked as portfolio candidates when their expected output is likely to become useful evidence. That marker does not create a portfolio artifact by itself.

Creating a portfolio artifact should remain a separate parent/admin action so the parent can confirm the actual work, context, date, files, tags, and export selection.

## Export Rule

Portfolio export must preserve artifact context, not only files. Each exported artifact should have enough metadata for a future reader to understand what it is evidence of.
