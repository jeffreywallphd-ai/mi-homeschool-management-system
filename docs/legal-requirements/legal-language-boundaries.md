# Legal Language Boundaries

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: allowed and disallowed legal/compliance wording
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Non-Goals](../product/non-goals.md), [Michigan Homeschool Context](michigan-homeschool-context.md)
- Related tests: not yet implemented
- Supersedes: none

## Allowed Framing

Use:

- "Parent-owned records."
- "Family-issued transcript."
- "Parent-issued diploma."
- "Selected Michigan subject-area coverage."
- "Based on parent-defined graduation standards."
- "Internal homeschool records."

## Disallowed Framing

Do not use:

- "Compliant with Michigan law."
- "Legally certified."
- "State-approved."
- "MDE-approved."
- "Accredited."
- "Official state record."
- "MDE submission."

## UI Guidance

Checklist screens should show coverage and missing records, not legal compliance status.

Document-generation screens should identify the basis for records:

- Parent-owned source records.
- Parent-defined standards.
- Family-issued credential.

## Review Rule

Any feature that changes legal-facing wording, generated-record wording, requirement coverage wording, or compliance-like status labels must review this document and the source review log.
