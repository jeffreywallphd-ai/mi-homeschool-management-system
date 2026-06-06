# System Overview

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: overall architecture shape
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Modular Monolith Boundaries](modular-monolith-boundaries.md), [Domain Module Map](domain-module-map.md)
- Related tests: not yet implemented
- Supersedes: none

## Architecture

The system should be a local-first modular monolith with clear layers:

- Web/UI.
- Application.
- Domain.
- Infrastructure.
- Tests.

The intended stack remains ASP.NET Core + Blazor + SQLite unless a future ADR changes it. The stack detail is not yet separately accepted; this document governs the shape, not every implementation package.

## System Responsibilities

- Store parent-owned homeschool records locally.
- Model Michigan as the first jurisdiction profile.
- Manage courses, lessons, assignments, submissions, grades, credits, portfolio artifacts, files, and official records.
- Generate family-issued documents and archive exports.
- Back up and restore family data.

## Boundary Principle

Every layer, including UI, must have tight contracts. UI components should use explicit view models and commands. Application handlers should validate command contracts. Domain objects should enforce invariants. Infrastructure should persist validated state without becoming the source of business rules.

## Non-Architecture

The system is not a microservice platform, cloud-first SaaS, or state-submission workflow.
