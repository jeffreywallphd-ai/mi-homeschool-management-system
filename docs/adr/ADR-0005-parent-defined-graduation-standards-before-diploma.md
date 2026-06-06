# ADR-0005: Parent-Defined Graduation Standards Before Diploma

- Status: accepted
- Last reviewed: 2026-06-06
- Deciders: parent/project owner
- Technical story: Diploma generation requires explicit internal family standards.
- Supersedes: none

## Context

MDE guidance describes report cards, transcripts, and diplomas as the responsibility of the homeschool family based on internal standards.

## Decision

The system must require a parent-defined graduation plan before generating a diploma. Seeded templates may help, but the parent must accept or edit the internal graduation standard.

## Consequences

- Graduation plans are first-class records.
- Diploma readiness depends on graduation-plan satisfaction or explicit parent waiver.
- Diploma wording must state or reference the family-issued basis.
- Agents must not generate diplomas from grade level alone.

## Follow-Up

Tests should verify that diploma generation is blocked without a graduation plan and required issue/signature data.
