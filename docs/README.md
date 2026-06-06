# Documentation Governance

## Purpose

This documentation is the control system for the Michigan homeschool management system. It preserves the product vision, domain language, legal posture, architecture, implementation standards, and AI-agent routing rules needed to build the project without drift.

The app is intended to help a parent manage a 12th-grade homeschool year in Michigan by maintaining credible parent-owned records: curriculum plans, instruction and activity records, assessment evidence, grades, credits, report cards, transcripts, diplomas, course descriptions, portfolio exports, and graduation archive packets.

## Documentation Areas and Authority

| Directory | Role | Canonical? |
| --- | --- | ---: |
| `docs/product/` | Product vision, release scope, use cases, roadmap, and non-goals | Yes for product scope |
| `docs/domain/` | Homeschool vocabulary, records lifecycle, grading, credits, portfolio, files, and official-record rules | Yes for domain behavior |
| `docs/legal-requirements/` | Michigan requirement model, legal-language boundaries, source posture, and jurisdiction seed rules | Yes for legal-reference modeling |
| `docs/architecture/` | Intended system structure, module boundaries, local storage, documents, backup, testing, and UI contracts | Yes for technical architecture |
| `docs/adr/` | Durable product-technical and architectural decisions | Yes for recorded decisions |
| `docs/standards/` | Coding, documentation, naming, testing, security, accessibility, backup, and AI-agent standards | Yes for implementation standards |
| `docs/context/` | Compact AI-oriented context routing and summary packs derived from canonical docs | No; routing and summarization only |
| `docs/roadmaps/` | Implementation roadmaps for bounded vertical slices | Yes for implementation sequencing once accepted |
| `docs/operations/` | Local operation, backup, restore, generated-record handling, archive export, upgrades, and recovery expectations | Yes after acceptance |
| `docs/templates/` | Reusable documentation templates | No; authoring support only |

## Authority Precedence

1. Accepted ADRs govern the specific decisions they record unless superseded.
2. Current product, domain, legal-requirements, architecture, standards, and operations docs govern within their areas.
3. Context packs summarize and route agents to relevant canonical guidance; they do not override canonical sources.
4. Temporary plans, task prompts, issue discussions, and chat history are not canonical unless intentionally promoted into documentation.

Do not quietly choose between conflicting canonical documents. Surface the conflict and correct or supersede the appropriate document as part of the work.

## Current Foundation

Product scope is defined by:

- [Product Vision and Scope](product/product-vision-and-scope.md)
- [V1 Scope](product/v1-scope.md)
- [User and Household Model](product/user-and-household-model.md)
- [Records and Credentials Use Cases](product/records-and-credentials-use-cases.md)
- [Homesteading Portfolio Use Cases](product/homesteading-portfolio-use-cases.md)
- [Roadmap](product/roadmap.md)
- [Non-Goals](product/non-goals.md)

Domain behavior is defined by:

- [Glossary](domain/glossary.md)
- [Homeschool Record Lifecycle](domain/homeschool-record-lifecycle.md)
- [Curriculum Planning Rules](domain/curriculum-planning-rules.md)
- [Instruction and Activity Record Rules](domain/instruction-and-activity-record-rules.md)
- [Assessment and Grading Rules](domain/assessment-and-grading-rules.md)
- [Credits and Graduation Rules](domain/credits-and-graduation-rules.md)
- [Official Records Rules](domain/official-records-rules.md)
- [Portfolio Evidence Rules](domain/portfolio-evidence-rules.md)
- [File and Artifact Taxonomy](domain/file-and-artifact-taxonomy.md)

Michigan legal-reference modeling is defined by:

- [Legal Requirements README](legal-requirements/README.md)
- [Michigan Homeschool Context](legal-requirements/michigan-homeschool-context.md)
- [Requirement Set Model](legal-requirements/requirement-set-model.md)
- [Michigan Requirement Areas](legal-requirements/michigan-requirement-areas.md)
- [Requirement Mapping Rules](legal-requirements/requirement-mapping-rules.md)
- [Recordkeeping Position](legal-requirements/recordkeeping-position.md)
- [Legal Language Boundaries](legal-requirements/legal-language-boundaries.md)
- [Source Review Log](legal-requirements/source-review-log.md)

Architecture is defined by:

- [System Overview](architecture/system-overview.md)
- [Modular Monolith Boundaries](architecture/modular-monolith-boundaries.md)
- [ASP.NET Blazor SQLite Stack](architecture/aspnet-blazor-sqlite-stack.md)
- [Domain Module Map](architecture/domain-module-map.md)
- [Local Data and File Storage](architecture/local-data-and-file-storage.md)
- [Document Generation Architecture](architecture/document-generation-architecture.md)
- [Backup Restore and Export Architecture](architecture/backup-restore-and-export-architecture.md)
- [Identity and Access Architecture](architecture/identity-and-access-architecture.md)
- [Testing Architecture](architecture/testing-architecture.md)

Remaining deferred architecture decisions are tracked in [Decision Readiness Register](adr/decision-readiness-register.md).

Repository standards are defined by:

- [Standards README](standards/README.md)
- [Documentation Standards](standards/documentation-standards.md)
- [AI-Agent Development Standards](standards/ai-agent-development-standards.md)
- [Coding Standards](standards/coding-standards.md)
- [Naming and Domain Language Standards](standards/naming-and-domain-language-standards.md)
- [Testing and Verification Standards](standards/testing-and-verification-standards.md)
- [Security and Privacy Standards](standards/security-and-privacy-standards.md)
- [Accessibility and Nontechnical UX Standards](standards/accessibility-and-nontechnical-ux-standards.md)
- [Data Retention Backup and Recovery Standards](standards/data-retention-backup-and-recovery-standards.md)
- [Change Impact Matrix](standards/change-impact-matrix.md)

Task-specific AI context is defined by:

- [Context README](context/README.md)
- [Prompt Routing](context/prompt-routing.md)
- `docs/context/packs/*.pack.md`

Implementation roadmaps are currently defined by:

- [Roadmaps README](roadmaps/README.md)
- [First Vertical Slice Roadmap](roadmaps/first-vertical-slice-roadmap.md)

Operations requirements are currently defined by:

- [Backup Restore and Archive Export](operations/backup-restore-and-archive-export.md)
- [Generated Records and Family Archive](operations/generated-records-and-family-archive.md)
- [Local Installation and Data Location](operations/local-installation-and-data-location.md)
- [Upgrades Migrations and Recovery](operations/upgrades-migrations-and-recovery.md)

## Documentation Update Rule

Future changes must update relevant documentation in the same change set when they alter product scope, domain vocabulary, legal-reference behavior, official-record semantics, credits/GPA/graduation behavior, architecture boundaries, data storage, generated documents, backup/export/recovery behavior, privacy posture, or implementation standards.
