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
- Courses can carry internal subject labels, but visible coverage uses requirement mappings.
- Pack subject labels are preserved as internal categorization data.
- Courses designate one-semester or two-semester duration.
- Pack defaults include course description details and curriculum plan fields.
- Courses own learning modules as instructional topic units; courses remain transcript-facing.
- Modules include instructions, itemized objectives, source-backed lessons, status, optional term placement, and assignment/evidence placeholders.
- Modules do not include goals; module objectives are sufficient.
- Modules do not expose major topics in the UI or pack contract.
- Module objectives may optionally link to course objectives; pack defaults should repeatedly support each course objective.
- Lessons sit inside modules and include introductory text plus one or more concrete resources.
- Lesson resources, not module resources, are the active student-facing resource layer.
- Default Michigan pack totals 8 planned credits and keeps history separate from government/civics/economics.
- Default government/civics and U.S. history options map U.S. Constitution and Michigan Constitution coverage.
- Pack resources use one item per line, with `Name | URL` when a viewable link exists.
- Pack learning objectives use one objective per line and finish "students will be able to..." without repeating that lead-in.
- Curriculum plan does not duplicate texts/resources as major resources.
- Instructional methods, assessment methods, and grading basis can include hybrid defaults/options.
- Backfill may fill blank imported-course pack fields but must preserve parent-entered text.
- Backfill may upgrade recognizable legacy built-in default text to the current built-in default format.
- Backfill removes stale imported-course mappings to retired seed rows and adds missing current pack mappings.
- Backfill may add missing source-backed pack modules or lessons but must preserve parent-created or parent-edited content.
- Requirement mappings are explicit.
- Parent-added requirements behave like seeded requirement areas in mapping workflows.
- Activity records are internal evidence, not routine state filings.
- UI forms submit complete contracts.

## Common Failure Modes

- Inferring missing requirement mappings.
- Duplicating imported template courses on repeat pack imports.
- Turning resources or objectives into hard-to-edit text blocks.
- Treating attendance as a legal compliance counter.
