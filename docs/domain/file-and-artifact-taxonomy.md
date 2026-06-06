# File and Artifact Taxonomy

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: stored-file categories and artifact organization
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Local Data and File Storage](../architecture/local-data-and-file-storage.md), [Portfolio Evidence Rules](portfolio-evidence-rules.md)
- Related tests: not yet implemented
- Supersedes: none

## File Categories

The system should support files in these categories:

- Student profile.
- Assignment submission.
- Portfolio artifact.
- Test record.
- Project evidence.
- Reading log support.
- Fieldwork evidence.
- External course record.
- Dual-enrollment record.
- Curriculum resource.
- Course material.
- Lesson material.
- Generated report card.
- Generated transcript.
- Generated diploma.
- Generated course-description packet.
- Generated portfolio export.
- Generated graduation packet.
- Backup or archive manifest.

## Expected File Types

The system should tolerate common family-owned education files:

- PDF.
- Images.
- Word-processing documents.
- Spreadsheets.
- Plain text or markdown.
- Scanned handwritten work.
- Audio or video files when later supported.
- External certificates or provider records.

## Stored File Rules

Every stored file should have:

- Stable identifier.
- Student or owner context where applicable.
- Category.
- Original filename.
- Stored path.
- Content type when known.
- Size.
- Checksum.
- Created timestamp.

## Artifact vs File

A file is binary or document content. An artifact is educational evidence with meaning. A portfolio artifact may reference one or more stored files, but the artifact metadata carries the learning context.
