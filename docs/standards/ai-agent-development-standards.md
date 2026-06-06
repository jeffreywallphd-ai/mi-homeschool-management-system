# AI-Agent Development Standards

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: automated agent behavior and context discipline
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Prompt Routing](../context/prompt-routing.md), [Change Impact Matrix](change-impact-matrix.md)
- Related tests: not yet implemented
- Supersedes: none

## Required Startup for Non-Trivial Tasks

Agents must read:

1. `docs/README.md`.
2. `docs/context/packs/index.pack.md`.
3. `docs/context/prompt-routing.md`.
4. `docs/standards/change-impact-matrix.md`.
5. Task-specific context packs and canonical docs named by routing.

## Agent Rules

- Inspect existing code/docs before changing them.
- Keep scope narrow.
- Preserve accepted ADRs.
- Do not invent legal, accreditation, auth, cloud, or migration policy.
- Use explicit contracts and avoid null-prone state.
- Update docs and tests when durable behavior changes.
- Report unresolved decisions instead of silently deciding them.

## Prohibited Shortcuts

- Treating context packs as stronger than canonical docs.
- Generating credentials without parent-defined standards.
- Calling records legally compliant.
- Adding broad abstractions to anticipate speculative features.
- Suppressing validation errors to make a build pass.
- Letting UI state bypass application/domain contracts.
