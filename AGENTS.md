# Agent Instructions

This repository is a Michigan homeschool management system focused on parent-owned records, evidence, credits, transcripts, diplomas, portfolio exports, and family archive packets.

## Required Startup for Non-Trivial Work

Before changing code or docs, read:

1. `docs/README.md`
2. `docs/context/packs/index.pack.md`
3. `docs/context/prompt-routing.md`
4. `docs/standards/change-impact-matrix.md`

Then load only the specialized context packs and canonical docs materially relevant to the task.

## Core Boundaries

- Preserve parent-owned records, not state filings.
- Do not imply Michigan legal compliance, MDE approval, state approval, accreditation, or legal certification.
- Keep Michigan as the first seeded jurisdiction, not a hard-coded app limit.
- Keep records and credentials first-class from the beginning.
- Require parent-defined graduation standards before diploma generation.
- Keep family data local-first unless a future accepted ADR changes that.
- Keep UI, application, domain, infrastructure, and tests tightly contract-backed.

## Context Pack Limit

No single file in `docs/context/packs/` may exceed 200 physical lines. If more detail is needed, update canonical docs and link to them from a compact pack.

## Legal-Facing Work

Before changing legal-facing wording, Michigan requirement behavior, generated-record wording, or coverage/checklist language, follow `docs/legal-requirements/legal-source-refresh-workflow.md`.

## High-Risk Actions

Stop and surface the issue before proceeding if a task would:

- Generate a diploma without an accepted parent-defined graduation plan.
- Let a student role edit grades, credits, graduation plans, official records, backup/restore, or admin settings.
- Move sensitive student data outside local storage.
- Bypass application/domain contracts from UI code.
- Require a deferred decision listed in `docs/adr/decision-readiness-register.md`.
