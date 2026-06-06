# Curriculum and Instruction Pack

Purpose: Course planning, lessons, instruction, activity, reading, project, and fieldwork records.

## Canonical Sources

- `docs/domain/curriculum-planning-rules.md`
- `docs/domain/course-pack-rules.md`
- `docs/domain/instruction-and-activity-record-rules.md`
- `docs/legal-requirements/requirement-mapping-rules.md`
- `docs/architecture/domain-module-map.md`

## Must Preserve

- Planning is course-first.
- Course packs import as editable parent-owned course records.
- Built-in pack import differs from future installed packs; installing adds external packs.
- Pack templates can expose dropdown choices with stable default options.
- Pack mappings must match the target jurisdiction seed vocabulary exactly.
- Courses can carry multiple subject labels.
- Pack subject labels are preserved for imported course records and coverage summaries.
- Courses designate one-semester or two-semester duration.
- Pack defaults include course description details and curriculum plan fields.
- Backfill may fill blank imported-course pack fields but must preserve parent-entered text.
- Requirement mappings are explicit.
- Activity records are internal evidence, not routine state filings.
- UI forms submit complete contracts.

## Common Failure Modes

- Inferring missing requirement mappings.
- Duplicating imported template courses on repeat pack imports.
- Treating attendance as a legal compliance counter.
