# Context Documentation

`docs/context/` helps AI agents and human prompt authors assemble accurate, minimum-sufficient context for implementation and review tasks.

Context packs are routing aids. They do not replace canonical product, domain, legal, architecture, standards, operations, or ADR documents.

## Authority Hierarchy

1. Accepted ADRs.
2. Canonical product/domain/legal/architecture/standards/operations docs.
3. Decision-readiness register.
4. Context packs and prompt routing.
5. Task prompt.

## Baseline Context Rule

For every non-trivial task, begin with:

```text
docs/README.md
docs/context/packs/index.pack.md
docs/context/prompt-routing.md
docs/standards/change-impact-matrix.md
```

Then add only the packs and canonical docs materially relevant to the task.

## Pack Inventory

- [Repository Baseline Pack](packs/index.pack.md)
- [Product and Domain Pack](packs/product-and-domain.pack.md)
- [Legal Michigan Pack](packs/legal-michigan.pack.md)
- [Curriculum and Instruction Pack](packs/curriculum-and-instruction.pack.md)
- [Assessment Credits Graduation Pack](packs/assessment-credits-graduation.pack.md)
- [Records and Credentials Pack](packs/records-and-credentials.pack.md)
- [Portfolio and Files Pack](packs/portfolio-and-files.pack.md)
- [Document Generation Pack](packs/document-generation.pack.md)
- [Local Data Backup Restore Pack](packs/local-data-backup-restore.pack.md)
- [Blazor UI Pack](packs/blazor-ui.pack.md)
- [Testing and Diagnostics Pack](packs/testing-and-diagnostics.pack.md)
- [Documentation and ADR Governance Pack](packs/documentation-and-adr-governance.pack.md)

## Maintenance Rule

Update packs only after canonical docs change in a way that affects repeated task execution.

## Pack Line Budget

Each individual context pack must stay at or below 200 physical lines. If a pack would exceed 200 lines, split it into narrower packs or move durable detail into canonical documentation and link to it from the pack.
