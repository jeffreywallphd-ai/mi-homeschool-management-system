# Generated Records and Family Archive

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: operational handling of generated records and graduation archive contents
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Records and Credentials Use Cases](../product/records-and-credentials-use-cases.md), [Official Records Rules](../domain/official-records-rules.md), [Document Generation Architecture](../architecture/document-generation-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Generated Records

Generated records are output artifacts created from source records. They should be saved, versioned, and linked to source identifiers or source snapshots.

Transcript packet exports may include a human-readable transcript HTML file, a JSON manifest, a Markdown manifest summary, and a single PDF packet version for printing or sharing. When course descriptions are selected, they should travel with the transcript as supporting context rather than replacing source course records.

## Family Archive

The family archive should preserve records that may be needed years later.

Recommended graduation archive contents:

- Transcript.
- Diploma copy.
- Course-description packet.
- Report cards.
- Graduation plan.
- Test records.
- Portfolio index.
- Selected portfolio artifacts.
- External course or dual-enrollment records.
- Parent progress evaluations.

## Versioning

If a generated record is re-created after source data changes, the system should create a new generated document record instead of silently replacing the previous issued record.

## Wording

Generated records and archive exports must use family-issued or parent-issued wording where appropriate and must not imply state approval, accreditation, or MDE issuance.
