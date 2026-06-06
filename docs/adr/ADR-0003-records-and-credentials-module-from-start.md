# ADR-0003: Records and Credentials Module From the Start

- Status: accepted
- Last reviewed: 2026-06-06
- Deciders: parent/project owner
- Technical story: Records and credentials must be first-class, not late-stage reporting add-ons.
- Supersedes: none

## Context

High-school homeschool records become important later for college, employment, military, trade school, transfer, and personal archive. Transcripts, diplomas, course descriptions, and portfolio exports need source data collected throughout the year.

## Decision

The app will include Records, Credits, Portfolio, Documents, and Backup/Export concerns from the beginning, even if the first UI is simple.

## Consequences

- Courses need descriptions and credit data early.
- Gradebook and portfolio records must preserve evidence.
- Report cards, transcripts, diplomas, and packets must be supported by the domain model.
- Generated documents should trace back to source records.

## Follow-Up

Keep V1 scope aligned with records and credentials, not only assignment tracking.
