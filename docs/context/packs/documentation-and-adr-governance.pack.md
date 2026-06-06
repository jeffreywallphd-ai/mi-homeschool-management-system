# Documentation and ADR Governance Pack

Purpose: Documentation, ADR, and context-pack maintenance.

## Canonical Sources

- `docs/README.md`
- `docs/standards/documentation-standards.md`
- `docs/adr/README.md`
- `docs/adr/decision-readiness-register.md`
- `docs/context/prompt-routing.md`

## Must Preserve

- Accepted ADRs govern recorded decisions.
- Context packs summarize; they do not override.
- Deferred decisions must not be silently implemented.

## Common Failure Modes

- Updating context packs instead of canonical docs.
- Leaving docs map links stale after file changes.
