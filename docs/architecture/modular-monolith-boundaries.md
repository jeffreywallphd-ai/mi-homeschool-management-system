# Modular Monolith Boundaries

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: module and layer boundary discipline
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [System Overview](system-overview.md), [Domain Module Map](domain-module-map.md), [Coding Standards](../standards/coding-standards.md)
- Related tests: not yet implemented
- Supersedes: none

## Principle

Keep boundaries tight across all layers, including UI. The project should be heavily contract-backed and avoid common failure points such as null exceptions, implicit state, and loosely shaped data moving between modules.

## Layer Rules

| Layer | Responsibility | Boundary rule |
| --- | --- | --- |
| UI | Render screens, collect input, show validation/results | Uses explicit view models; does not mutate domain objects directly |
| Application | Commands, queries, workflows, validation coordination | Owns use-case contracts; calls domain services/entities |
| Domain | Business rules and invariants | No UI, persistence, or infrastructure concerns |
| Infrastructure | Database, files, documents, auth, backup | Implements interfaces; does not invent domain behavior |
| Tests | Verification of contracts and invariants | Exercises domain/application behavior and risky infrastructure |

## Contract Rules

- Public module operations should have explicit command/query models.
- Nullable values must be intentional and named by state, not accidental.
- Missing required values should fail validation before domain mutation.
- Cross-module calls should use stable identifiers and contracts, not direct mutable object sharing.
- UI event handlers should translate user input into typed requests.

## Module Coupling

Modules may coordinate through application services and shared identifiers. They should not reach into each other's persistence details.

## Abstraction Rule

Add abstractions when they protect boundaries or remove real complexity. Do not add speculative abstractions for imagined future features.
