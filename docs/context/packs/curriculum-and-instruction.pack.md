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
- Courses imported from packs become editable parent-owned course records.
- Courses are student-owned; admin course lists, creation, import, and coverage summaries are scoped to the selected student.
- Archived courses are retained records but are hidden from active course lists and coverage summaries.
- Delete must not remove courses with student work; archive those courses instead.
- A `.coursepack` is a single-course JSON artifact with course detail fields and module references only.
- A `.coursepack` must not embed module, lesson, or assignment bodies.
- A `.courseplanpack` is a plan manifest that lists course offerings by pacing period.
- A course plan bundle is a `.zip` artifact: plan manifest, course folders, one coursepack per course, and module folders containing modulepack, lessonpack, and assignmentpack files.
- Course plan bundle JSON files should be UTF-8 without a byte order marker.
- Importing copies courses from an available plan, course plan zip bundle, or single-course coursepack into the student's course list.
- Legacy multi-course `.coursepack` install may remain for compatibility, but the preferred full-plan exchange format is a `.zip` course plan bundle.
- Re-importing a newer version of the same course plan zip should update by stable source ids and must not duplicate existing active courses, modules, lessons, or assignments.
- Pack downloads include source identity metadata: publisher id, pack id, pack version, and source namespace.
- External source ids are references only; internal records use app-owned GUIDs.
- Update matching uses pack identity plus item source ids, not source ids alone.
- Blank or sample external ids may be repaired on import, but non-template packs should provide deliberate stable source ids.
- The same course-pack template may be imported for different students without being treated as a duplicate.
- Pack templates can expose dropdown choices with stable default options.
- Pack mappings must match the target jurisdiction seed vocabulary exactly.
- Unmatched pack mappings should not block import; leave them for parent review after import.
- Courses can carry internal subject labels, but visible coverage uses requirement mappings.
- Pack subject labels are preserved as internal categorization data.
- Courses designate one-semester or two-semester duration.
- Pack defaults include course description details and curriculum plan fields.
- Courses own learning modules as instructional topic units; courses remain transcript-facing.
- Modules include instructions, itemized objectives, source-backed lessons, source-backed assignments, status, optional term placement, and assignment/evidence notes.
- Courses, modules, and lessons may carry completion status for progress tracking; status must not award credit or create grades.
- Modules do not include goals; module objectives are sufficient.
- Modules do not expose major topics in the UI or pack contract.
- Module objectives may optionally link to course objectives; pack defaults should repeatedly support each course objective.
- A `.modulepack` is a course-level JSON artifact with one module shell; importing appends a module to the selected course.
- Module packs include module details plus lightweight lesson/assignment sequencing references, not lesson or assignment bodies.
- Lessons sit inside modules and include introductory text plus one or more concrete resources.
- Lesson resources, not module resources, are the active student-facing resource layer.
- Lessons should carry enough structure to guide student work: metadata, lesson objectives, success criteria, workflow steps, resources, practice/problem sets, portfolio connections, rubrics, reflection prompts, and parent notes.
- Student lesson views must not show expected answers, worked solutions, or parent/instructor notes by default.
- Lesson resources include how the student should use the resource, not just where the resource lives.
- Lessons may link to module assignments so students can see which assignments use the lesson material.
- A `.lessonpack` is a module-level JSON artifact with one or more lessons; importing appends lessons to the selected module.
- Lesson pack assignment links use assignment source ids and assignment titles; missing assignment links should not block import.
- Lesson pack import/export must not create courses or replace existing module lessons.
- Future lesson packs with attached files should use a zip archive containing the `.lessonpack` JSON and referenced files.
- Assignments sit inside modules and define concrete student work tied to module objectives.
- Assignments may link to lessons so the expected work points back to specific readings, videos, files, or physical resources.
- Assignments should carry enough structure to guide and review work: summary, goal, deliverables, submission formats, assignment resources, workflow steps, assessment skills, checklist, portfolio connection, rubric, revision policy, completion criteria, reflection prompts, evidence requirements, and scoring plan.
- Assignment variants use instructional method profiles, including a hybrid/default option and digital profile.
- Assignment status and planned points are planning fields, not grades or credit awards.
- Assignment attempt policy controls student submission workflow only.
- Assignment submission structure may define a single submission or a multi-draft workflow across linked lessons.
- Multi-draft assignment draft count defines lesson draft slots; the final linked draft is the final assignment submission.
- Assignment rubrics, scoring, completion, and evidence requirements are planning support until a parent/admin records an evaluation or evidence item.
- A `.assignmentpack` is a module-level JSON artifact with one or more assignments; importing appends assignments to the selected module.
- Assignment pack lesson links use lesson source ids and lesson titles; missing lesson links should not block import.
- Future assignment packs with attached files should use a zip archive containing the `.assignmentpack` JSON and referenced files.
- Default Michigan pack totals 8 planned credits and keeps history separate from government/civics/economics.
- Default government/civics and U.S. history options map U.S. Constitution and Michigan Constitution coverage.
- Pack resources use one item per line, with `Name | URL` when a viewable link exists.
- Pack learning objectives use one objective per line and finish "students will be able to..." without repeating that lead-in.
- Curriculum plan does not duplicate texts/resources as major resources.
- Instructional methods, assessment methods, and grading basis can include hybrid defaults/options.
- Backfill may fill blank imported-course pack fields but must preserve parent-entered text.
- Backfill may upgrade recognizable legacy built-in default text to the current built-in default format.
- Backfill removes stale imported-course mappings to retired seed rows and adds missing current pack mappings.
- Backfill may add missing source-backed pack modules, lessons, or assignments but must preserve parent-created or parent-edited content.
- Requirement mappings are explicit.
- Parent-added requirements behave like seeded requirement areas in mapping workflows.
- Activity records are internal evidence, not routine state filings.
- UI forms submit complete contracts.

## Common Failure Modes

- Inferring missing requirement mappings.
- Duplicating imported template courses on repeat pack imports.
- Turning resources or objectives into hard-to-edit text blocks.
- Treating assignment status as completion evidence or a grade.
- Treating attendance as a legal compliance counter.
