# ADR-0001: Parent-Owned Records, Not State Filings

- Status: accepted
- Last reviewed: 2026-06-06
- Deciders: parent/project owner
- Technical story: Records should support family evidence and credentials without implying state submission.
- Supersedes: none

## Context

Michigan exemption (f) home-schooling does not require routine reporting to MDE. MDE guidance states parents are responsible for records including gradebooks, progress reports, transcripts, and diplomas.

## Decision

The system will model homeschool records as parent-owned evidence artifacts and family-issued records, not state-submitted compliance filings.

## Consequences

- The product will prioritize records, evidence, generated documents, and archives.
- The app will not include MDE submission workflows for ordinary exemption (f) use.
- Coverage summaries will avoid legal-certification language.
- Generated credentials will be clearly parent-issued or family-issued.

## Follow-Up

All legal-facing wording must follow [Legal Language Boundaries](../legal-requirements/legal-language-boundaries.md).
