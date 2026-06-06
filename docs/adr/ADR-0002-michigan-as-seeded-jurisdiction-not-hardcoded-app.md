# ADR-0002: Michigan as Seeded Jurisdiction, Not Hardcoded App

- Status: accepted
- Last reviewed: 2026-06-06
- Deciders: parent/project owner
- Technical story: Support Michigan first while preserving jurisdiction extensibility.
- Supersedes: none

## Context

The first real use case is a Michigan homeschool year. The system needs Michigan requirement areas and source-backed wording, but future jurisdiction profiles should remain possible.

## Decision

Michigan will be seeded as the first jurisdiction profile. The app will not hard-code its entire domain, UI, or records model around Michigan as the only possible jurisdiction.

## Consequences

- Requirement sets and areas are modeled as data.
- Courses map to requirement areas through explicit mappings.
- Generated wording can reference the active jurisdiction profile.
- Future jurisdictions can be added without rewriting the core homeschool record model.

## Follow-Up

Seed Michigan statutory and MDE summary requirement areas separately.
