# Non-Goals

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: product boundaries and features the system must not imply
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Legal Language Boundaries](../legal-requirements/legal-language-boundaries.md), [Michigan Homeschool Context](../legal-requirements/michigan-homeschool-context.md)
- Related tests: not yet implemented
- Supersedes: none

## Non-Goals

The system is not:

- A legal-advice product.
- A state compliance certification system.
- An accreditation system.
- A state filing or MDE submission system.
- A guarantee that a family is compliant with Michigan law.
- A public school replacement SIS.
- A cloud-first multi-family school administration platform.
- A transcript clearinghouse.
- A diploma authority separate from the parent/legal guardian.

## Prohibited Product Claims

The product must not say:

- "You are compliant with Michigan law."
- "This diploma is state-approved."
- "This transcript is accredited."
- "MDE has accepted these records."

Preferred language:

- "Your records show coverage of the Michigan homeschool subject areas you selected."
- "This transcript/diploma is parent-issued based on your family's internal graduation standards."

## Implementation Boundary

Do not add features that require legal interpretation, state submission, accreditation, or third-party credential validation unless a future accepted ADR and product-scope update explicitly authorizes that work.
