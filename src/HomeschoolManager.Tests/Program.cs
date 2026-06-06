using HomeschoolManager.Application.Courses;
using HomeschoolManager.Application.Requirements;
using HomeschoolManager.Application.Setup;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using HomeschoolManager.Infrastructure.Configuration;

var tests = new List<(string Name, Func<Task> Test)>
{
    ("Household requires parent name", () =>
    {
        AssertThrows<DomainException>(() => new Household(Guid.NewGuid(), "Family", ""));
        return Task.CompletedTask;
    }),
    ("Student role cannot create household", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new SetupService(repository);
        var result = await service.CreateHouseholdAsync(UserContext.Student("Student"), new CreateHouseholdCommand("Family", "Parent"));
        AssertFalse(result.Succeeded, "Student command should fail.");
    }),
    ("Setup commands persist and reload", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new SetupService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await service.CreateHouseholdAsync(parent, new CreateHouseholdCommand("Family", "Parent"))).Succeeded, "Household failed.");
        AssertTrue((await service.ConfigureSchoolProfileAsync(parent, new ConfigureSchoolProfileCommand(
            "Family Homeschool",
            "Parent",
            "Michigan",
            new DateOnly(2026, 8, 24),
            "Michigan Exemption 3(f)",
            "Parent",
            "Lansing",
            "Michigan"))).Succeeded, "School profile failed.");
        AssertTrue((await service.CreateStudentAsync(parent, new CreateStudentCommand("Student", "Learner", 12))).Succeeded, "Student failed.");
        AssertTrue((await service.ConfigureSchoolYearAsync(parent, new ConfigureSchoolYearCommand(
            "2026-2027",
            2026,
            2027,
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 12, 18),
            new DateOnly(2027, 1, 11),
            new DateOnly(2027, 5, 28)))).Succeeded, "School year failed.");

        var summary = await service.GetSummaryAsync();
        AssertTrue(summary.HasHousehold, "Missing household.");
        AssertTrue(summary.HasSchoolProfile, "Missing school profile.");
        AssertTrue(summary.HasStudent, "Missing student.");
        AssertTrue(summary.HasSchoolYear, "Missing school year.");
    }),
    ("Michigan seed is idempotent and keeps views distinct", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new RequirementService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await service.SeedMichiganAsync(parent)).Succeeded, "First seed failed.");
        AssertTrue((await service.SeedMichiganAsync(parent)).Succeeded, "Second seed failed.");

        var checklist = await service.GetChecklistAsync();
        AssertEqual(16, checklist.Count, "Unexpected seeded area count.");
        AssertEqual("Statutory", checklist[0].View, "Statutory requirements should be listed first.");
        AssertTrue(checklist.Any(item => item.View == "Statutory" && item.Name == "English Grammar"), "Missing statutory view area.");
        AssertTrue(checklist.Any(item => item.View == "MDE Summary" && item.Name == "U.S. Constitution"), "Missing MDE U.S. Constitution area.");
        AssertTrue(checklist.Any(item => item.View == "MMC Reference" && item.Name == "Personal Finance"), "Missing MMC personal finance reference area.");
        AssertFalse(checklist.Any(item => item.View != "Statutory" && item.Name == "English Language Arts"), "English Language Arts should be represented by statutory English subject rows.");
    }),
    ("Default Michigan course pack mappings match seeded requirement areas", (Func<Task>)(() =>
    {
        var seededAreas = MichiganRequirementSeed.CreateAreas()
            .Select(area => $"{area.View}:{area.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pack = DefaultCoursePacks.All.First(item => item.Id == DefaultCoursePacks.MichiganCollegeReadyPackId);
        AssertEqual("Michigan", pack.RequirementJurisdiction, "Default pack should target Michigan requirements.");
        foreach (var template in pack.Courses)
        {
            foreach (var option in template.Options)
            {
                foreach (var mapping in option.RequirementMappings)
                {
                    AssertTrue(
                        seededAreas.Contains($"{mapping.RequirementAreaView}:{mapping.RequirementAreaName}"),
                        $"Missing seeded requirement area for {template.TemplateId}/{option.OptionId}: {mapping.RequirementAreaView}:{mapping.RequirementAreaName}");
                }

                AssertFalse(string.IsNullOrWhiteSpace(option.Description.MajorTopics), $"Missing major topics for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.TextsAndResources), $"Missing texts/resources for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Description.TextsAndResources.Split(Environment.NewLine).Any(line => line.Contains('|', StringComparison.Ordinal)), $"Texts/resources should include named resource links for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.InstructionalMethods), $"Missing instructional methods for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Description.InstructionalMethods.Contains("Hybrid", StringComparison.OrdinalIgnoreCase), $"Instructional methods should include a hybrid option for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.AssessmentMethods), $"Missing assessment methods for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Description.AssessmentMethods.Contains("Hybrid", StringComparison.OrdinalIgnoreCase), $"Assessment methods should include a hybrid option for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.GradingBasis), $"Missing grading basis for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Description.GradingBasis.Contains("Hybrid", StringComparison.OrdinalIgnoreCase), $"Grading basis should include a hybrid option for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.Goals), $"Missing curriculum goals for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.LearningObjectives), $"Missing learning objectives for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.CurriculumPlan.LearningObjectives.Split(Environment.NewLine).Length >= 3, $"Learning objectives should be separated for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(option.CurriculumPlan.LearningObjectives.Contains("Upon completion", StringComparison.OrdinalIgnoreCase), $"Learning objectives should finish the standard sentence without repeating it for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(option.CurriculumPlan.LearningObjectives.Contains("course-level", StringComparison.OrdinalIgnoreCase), $"Learning objectives should be course-specific for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(option.CurriculumPlan.LearningObjectives.Contains("produce evidence suitable for course records", StringComparison.OrdinalIgnoreCase), $"Learning objectives should avoid generic recordkeeping language for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(string.IsNullOrWhiteSpace(option.CurriculumPlan.MajorResources), $"Major resources should be retired for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.PlannedSequence), $"Missing planned sequence for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.Count >= 3, $"Every default-pack option should include learning modules for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.SequenceOrder >= 1), $"Pack module order should be valid for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => !string.IsNullOrWhiteSpace(module.Instructions)), $"Pack modules should include instructions for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.LearningObjectives.Count > 0), $"Pack modules should include learning objectives for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.Resources.Count > 0), $"Pack modules should include concrete resources for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.Lessons.Count >= module.LearningObjectives.Count), $"Pack modules should include at least one lesson per module objective for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.SelectMany(module => module.Lessons).All(lesson => lesson.Resources.Count > 0), $"Pack lessons should include resources for {template.TemplateId}/{option.OptionId}.");
                foreach (var module in option.Modules)
                {
                    foreach (var objective in module.LearningObjectives)
                    {
                        AssertTrue(module.Lessons.Any(lesson => lesson.LinkedModuleObjective == objective.Text), $"Each module objective should have a linked lesson for {template.TemplateId}/{option.OptionId}: {objective.Text}");
                    }
                }
                foreach (var courseObjective in option.CurriculumPlan.LearningObjectives.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var linkedCount = option.Modules.SelectMany(module => module.LearningObjectives).Count(objective => objective.LinkedCourseObjective == courseObjective);
                    AssertTrue(linkedCount >= 2, $"Course objective should be supported by at least two module objectives for {template.TemplateId}/{option.OptionId}: {courseObjective}");
                }
                AssertTrue(option.Modules.All(module => !string.IsNullOrWhiteSpace(module.AssignmentEvidencePlaceholder)), $"Pack modules should include assignment/evidence placeholders for {template.TemplateId}/{option.OptionId}.");
            }
        }

        AssertEqual(8m, pack.Courses.Sum(course => course.DefaultOption.PlannedCreditValue), "Default pack should total eight planned credits.");

        return Task.CompletedTask;
    })),
    ("Student role cannot seed Michigan requirements", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new RequirementService(repository);
        var result = await service.SeedMichiganAsync(UserContext.Student("Student"));
        AssertFalse(result.Succeeded, "Student seed command should fail.");
    }),
    ("Parent requirements extend requirement lists and mappings", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertFalse((await requirementService.AddParentRequirementAsync(
            UserContext.Student("Student"),
            new AddParentRequirementCommand("Statutory", "Family Civics Project", "Grade 12"))).Succeeded, "Student should not add parent requirements.");
        AssertTrue((await requirementService.AddParentRequirementAsync(
            parent,
            new AddParentRequirementCommand("Statutory", "Family Civics Project", "Grade 12"))).Succeeded, "Parent requirement add failed.");

        var checklist = await requirementService.GetChecklistAsync();
        var parentRequirement = checklist.FirstOrDefault(item =>
            item.View == "Statutory" &&
            item.Name == "Family Civics Project" &&
            item.RequiredOrRecommended == "Parent");
        if (parentRequirement is null)
        {
            throw new InvalidOperationException("Parent requirement was not listed.");
        }
        var refreshedChecklist = await requirementService.GetChecklistAsync();
        AssertEqual(1, refreshedChecklist.Count(item => item.Name == "Family Civics Project"), "Parent requirement should not duplicate on seed refresh.");

        var createResult = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Civics Practicum", "Applied civic learning.", [], CourseDuration.OneSemester, 0.5m));
        AssertTrue(createResult.Succeeded, "Course create without visible subject input should pass.");
        var courseId = createResult.Value;
        AssertTrue((await courseService.SetRequirementMappingsAsync(parent, new SetCourseRequirementMappingsCommand(
            courseId,
            [new RequirementMappingCommand(parentRequirement.RequirementAreaId, CoverageLevel.Primary, "Parent-added coverage.")]))).Succeeded, "Parent requirement mapping failed.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        AssertTrue(coverage.Any(item => item.Name == "Family Civics Project" && item.IsMapped), "Parent requirement should appear in coverage summary.");
    }),
    ("Course requires title and valid planned credit", () =>
    {
        var studentId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", [], CourseDuration.TwoSemesters, 1, null, null, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", ["Science"], CourseDuration.TwoSemesters, 0, null, null, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", ["Science"], CourseDuration.TwoSemesters, 4, null, null, null, null, []));
        _ = new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []);
        return Task.CompletedTask;
    }),
    ("Learning module requires course, title, instructions, objectives, and valid order", () =>
    {
        var courseId = Guid.NewGuid();
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), Guid.Empty, "", 1, "Foundations", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 0, "Foundations", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 1, "", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 1, "Foundations", "", "", "", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 1, "Foundations", "", "", "Read and discuss.", "Topic", "", "", "", ModuleStatus.Planned));
        _ = new LearningModule(Guid.NewGuid(), courseId, "", 1, "Foundations", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "Draft assignment evidence.", ModuleStatus.Planned);
        return Task.CompletedTask;
    }),
    ("Course modules are course-owned and ordered without goals", () =>
    {
        var courseId = Guid.NewGuid();
        var course = new Course(courseId, Guid.NewGuid(), Guid.NewGuid(), "Biology", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []);
        var second = new LearningModule(Guid.NewGuid(), course.Id, "second", 2, "Second", "", "", "Instruction", "Topic", "Objective", "", "", ModuleStatus.Planned);
        var first = new LearningModule(Guid.NewGuid(), course.Id, "first", 1, "First", "", "", "Instruction", "Topic", "Objective", "", "", ModuleStatus.Planned);
        var otherCourseModule = new LearningModule(Guid.NewGuid(), Guid.NewGuid(), "other", 1, "Other", "", "", "Instruction", "Topic", "Objective", "", "", ModuleStatus.Planned);

        var updated = course.WithModules([second, first]);
        AssertEqual("First", updated.Modules[0].Title, "Modules should be ordered by sequence.");
        AssertEqual(1, updated.Modules[0].SequenceOrder, "Module sequence should be normalized.");
        AssertFalse(typeof(LearningModule).GetProperties().Any(property => property.Name == "Goals"), "Learning modules should not include a goals field.");
        AssertThrows<DomainException>(() => course.WithModules([otherCourseModule]));
        AssertThrows<DomainException>(() => course.WithModules([first, first with { Id = Guid.NewGuid() }]));
        return Task.CompletedTask;
    }),
    ("Lessons require module ownership intro text and resources", () =>
    {
        var moduleId = Guid.NewGuid();
        var resource = new LessonResource(Guid.NewGuid(), "OpenStax section", LessonResourceType.TextbookChapter, "https://openstax.org/", "", false, "Source note");
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), Guid.Empty, "", 1, "Lesson", "Intro", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 0, "Lesson", "Intro", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 1, "", "Intro", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 1, "Lesson", "", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 1, "Lesson", "Intro", "", []));
        _ = new Lesson(Guid.NewGuid(), moduleId, "source-lesson", 1, "Lesson", "Intro", "Objective", [resource]);
        return Task.CompletedTask;
    }),
    ("Requirement mapping requires course and requirement area", () =>
    {
        AssertThrows<DomainException>(() => new RequirementMapping(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), CoverageLevel.Primary, ""));
        AssertThrows<DomainException>(() => new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, CoverageLevel.Primary, ""));
        AssertThrows<DomainException>(() => new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (CoverageLevel)99, ""));
        _ = new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CoverageLevel.Supporting, "Practical evidence");
        return Task.CompletedTask;
    }),
    ("Course mappings reject duplicates and mismatched courses", () =>
    {
        var course = new Course(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Biology", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []);
        var requirementAreaId = Guid.NewGuid();
        var mapping = new RequirementMapping(Guid.NewGuid(), course.Id, requirementAreaId, CoverageLevel.Primary, "");
        var duplicate = new RequirementMapping(Guid.NewGuid(), course.Id, requirementAreaId, CoverageLevel.Secondary, "");
        var mismatched = new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CoverageLevel.Primary, "");

        AssertThrows<DomainException>(() => course.WithMappings([mismatched]));
        AssertThrows<DomainException>(() => course.WithMappings([mapping, duplicate]));
        _ = course.WithMappings([mapping]);
        return Task.CompletedTask;
    }),
    ("Parent can create course and student cannot mutate courses", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);

        var studentResult = await service.CreateCourseAsync(
            UserContext.Student("Student"),
            new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertFalse(studentResult.Succeeded, "Student course creation should fail.");

        var parentResult = await service.CreateCourseAsync(
            UserContext.ParentAdmin("Parent"),
            new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(parentResult.Succeeded, "Parent course creation should pass.");

        var courses = await service.ListCoursesAsync();
        AssertEqual(1, courses.Count, "Unexpected course count.");
        AssertEqual("Biology", courses[0].Title, "Unexpected course title.");
        AssertEqual("Life science overview.", courses[0].Description, "Unexpected course description.");
        AssertEqual(1, courses[0].SubjectAreas.Count, "Unexpected subject area count.");
    }),
    ("Parent can create update and reorder modules while student cannot mutate them", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");

        var courseResult = await service.CreateCourseAsync(parent, new CreateCourseCommand("Civics", "Government course.", [], CourseDuration.OneSemester, 0.5m));
        AssertTrue(courseResult.Succeeded, "Parent should create course.");
        var courseId = courseResult.Value;

        var denied = await service.CreateLearningModuleAsync(student, new CreateLearningModuleCommand(
            courseId,
            "Constitutional Foundations",
            "Overview",
            null,
            "2 weeks",
            "Read, discuss, and summarize source material.",
            Objectives("Explain constitutional principles."),
            Resources("Primary source excerpts"),
            "Short written explanation.",
            ModuleStatus.Planned));
        AssertFalse(denied.Succeeded, "Student should not create modules.");

        var first = await service.CreateLearningModuleAsync(parent, new CreateLearningModuleCommand(
            courseId,
            "Constitutional Foundations",
            "Overview",
            null,
            "2 weeks",
            "Read, discuss, and summarize source material.",
            Objectives("Explain constitutional principles."),
            Resources("Primary source excerpts"),
            "Short written explanation.",
            ModuleStatus.Planned));
        var second = await service.CreateLearningModuleAsync(parent, new CreateLearningModuleCommand(
            courseId,
            "Civic Participation",
            "Participation overview",
            null,
            "2 weeks",
            "Compare civic actions and document a response.",
            Objectives("Evaluate civic participation options."),
            Resources("iCivics resources"),
            "Reflection placeholder.",
            ModuleStatus.Planned));
        AssertTrue(first.Succeeded, "First module should create.");
        AssertTrue(second.Succeeded, "Second module should create.");

        var updateDenied = await service.UpdateLearningModuleAsync(student, new UpdateLearningModuleCommand(
            courseId,
            first.Value,
            "Denied",
            "",
            null,
            "",
            "Instruction",
            Objectives("Objective"),
            Resources("Resource"),
            "",
            ModuleStatus.Active));
        AssertFalse(updateDenied.Succeeded, "Student should not update modules.");

        var update = await service.UpdateLearningModuleAsync(parent, new UpdateLearningModuleCommand(
            courseId,
            first.Value,
            "Constitutional Foundations",
            "Overview",
            null,
            "3 weeks",
            "Read, discuss, summarize, and compare constitutional sources.",
            Objectives("Explain constitutional principles.", "Compare U.S. and Michigan constitutional structures."),
            Resources("Primary source excerpts"),
            "Short written explanation.",
            ModuleStatus.Active));
        AssertTrue(update.Succeeded, "Parent should update module.");

        var reorder = await service.ReorderLearningModulesAsync(parent, new ReorderLearningModulesCommand(courseId, [second.Value, first.Value]));
        AssertTrue(reorder.Succeeded, "Parent should reorder modules.");

        var modules = await service.ListModulesAsync(courseId);
        AssertEqual("Civic Participation", modules[0].Title, "Modules should be reordered.");
        AssertEqual(1, modules[0].SequenceOrder, "Reordered module sequence should normalize.");
        AssertEqual((int)ModuleStatus.Active, (int)modules[1].Status, "Module update should persist.");

        var detail = await service.GetModuleDetailAsync(courseId, first.Value);
        if (detail is null)
        {
            throw new InvalidOperationException("Module detail was not found.");
        }

        AssertTrue(detail.LearningObjectives.Contains("Michigan constitutional structures", StringComparison.OrdinalIgnoreCase), "Module detail should include updated objectives.");
        var deleteDenied = await service.DeleteLearningModuleAsync(parent, new DeleteLearningModuleCommand(courseId, first.Value, "delete"));
        AssertFalse(deleteDenied.Succeeded, "Module deletion should require exact confirmation text.");
        var deleteResult = await service.DeleteLearningModuleAsync(parent, new DeleteLearningModuleCommand(courseId, first.Value, "Delete"));
        AssertTrue(deleteResult.Succeeded, "Parent should delete module after confirmation.");
        AssertFalse((await service.ListModulesAsync(courseId)).Any(module => module.Id == first.Value), "Deleted module should be removed.");
    })),
    ("Parent can create update and reorder lessons while student cannot mutate them", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Cells",
                "Cell biology module.",
                null,
                "2 weeks",
                "Study cells.",
                Objectives("Explain cell structure."),
                [],
                "Cell diagram evidence.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");

        var studentCreate = await service.CreateLessonAsync(
            UserContext.Student("Student"),
            new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertFalse(studentCreate.Succeeded, "Student should not create lessons.");

        var firstLesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertTrue(firstLesson.Succeeded, "Lesson create failed.");
        var secondLesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(course.Value, module.Value, "Cell evidence", "Use diagrams to explain cell structures.", "Explain cell structure.", LessonResources("Cell diagram")));
        AssertTrue(secondLesson.Succeeded, "Second lesson create failed.");

        var update = await service.UpdateLessonAsync(
            parent,
            new UpdateLessonCommand(course.Value, module.Value, firstLesson.Value, "Cell structure and function", "Read, watch, and summarize cell structures.", "Explain cell structure.", LessonResources("OpenStax Biology 2e")));
        AssertTrue(update.Succeeded, "Lesson update failed.");
        var reorder = await service.ReorderLessonsAsync(parent, new ReorderLessonsCommand(course.Value, module.Value, [secondLesson.Value, firstLesson.Value]));
        AssertTrue(reorder.Succeeded, "Lesson reorder failed.");

        var lessons = await service.ListLessonsAsync(course.Value, module.Value);
        AssertEqual("Cell evidence", lessons[0].Title, "Lessons should reorder.");
        AssertEqual("Cell structure and function", lessons[1].Title, "Lesson update should persist.");

        var deleteDenied = await service.DeleteLessonAsync(parent, new DeleteLessonCommand(course.Value, module.Value, secondLesson.Value, "delete"));
        AssertFalse(deleteDenied.Succeeded, "Lesson delete should require exact confirmation.");
        var delete = await service.DeleteLessonAsync(parent, new DeleteLessonCommand(course.Value, module.Value, secondLesson.Value, "Delete"));
        AssertTrue(delete.Succeeded, "Lesson delete failed.");
        AssertFalse((await service.ListLessonsAsync(course.Value, module.Value)).Any(lesson => lesson.Id == secondLesson.Value), "Deleted lesson should be removed.");
    }),
    ("Module autosave preserves existing lessons", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Civics", "Government overview.", ["Civics"], CourseDuration.OneSemester, 0.5m));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Constitutional foundations",
                "Study constitutional principles.",
                null,
                "2 weeks",
                "Read, discuss, and write about constitutional foundations.",
                Objectives("Explain constitutional principles."),
                [],
                "Written explanation.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(
                course.Value,
                module.Value,
                "The constitutional convention",
                "Read and explain why the federal Constitution replaced the Articles of Confederation.",
                "Explain constitutional principles.",
                LessonResources("OpenStax U.S. Constitution")));
        AssertTrue(lesson.Succeeded, "Lesson create failed.");

        var update = await service.UpdateLearningModuleAsync(
            parent,
            new UpdateLearningModuleCommand(
                course.Value,
                module.Value,
                "Constitutional foundations and federalism",
                "Study constitutional principles and federalism.",
                null,
                "3 weeks",
                "Read, discuss, and write about constitutional foundations and federalism.",
                Objectives("Explain constitutional principles."),
                [],
                "Written explanation and discussion notes.",
                ModuleStatus.Active));
        AssertTrue(update.Succeeded, "Module update failed.");

        var lessons = await service.ListLessonsAsync(course.Value, module.Value);
        AssertEqual(1, lessons.Count, "Module update should preserve existing lessons.");
        AssertEqual("The constitutional convention", lessons[0].Title, "Existing lesson content should be preserved.");
    }),
    ("Student course client reads courses syllabi and modules without inferred grades", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");
        var setupService = new SetupService(repository);
        var courseService = new CourseService(repository);
        var studentService = new StudentCourseService(repository);

        var import = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(import.Succeeded, "Default pack import should succeed.");

        AssertTrue((await setupService.CreateStudentAsync(parent, new CreateStudentCommand("Younger", "Learner", 9))).Succeeded, "Second child should be added.");
        var configuredStudents = await setupService.ListStudentsAsync();
        var primaryStudent = configuredStudents.First(item => item.FirstName == "Student");
        var secondStudent = configuredStudents.First(item => item.FirstName == "Younger");

        var dashboard = await studentService.ListCoursesAsync(student, primaryStudent.Id);
        AssertTrue(dashboard.Succeeded, "Student course dashboard should load.");
        AssertEqual("Student", dashboard.Value!.StudentFirstName, "Dashboard should welcome the selected student.");
        AssertTrue(dashboard.Value.TermNames.Count == 2, "Dashboard should include configured semester headings.");
        AssertTrue(dashboard.Value.Courses.Count > 0, "Student should see imported courses.");
        var firstCourse = dashboard.Value.Courses.First();
        AssertEqual("No grade yet", firstCourse.CurrentGrade, "Student client should not infer grades before gradebook exists.");
        AssertTrue(firstCourse.TermNames.Count > 0, "Course cards should expose semester placement.");

        var secondDashboard = await studentService.ListCoursesAsync(student, secondStudent.Id);
        AssertTrue(secondDashboard.Succeeded, "Second child dashboard should load.");
        AssertEqual(0, secondDashboard.Value!.Courses.Count, "A child should not see another child's courses.");
        AssertFalse((await studentService.GetCourseAsync(student, firstCourse.CourseId, secondStudent.Id)).Succeeded, "Wrong child path should not expose the course.");

        var course = await studentService.GetCourseAsync(student, firstCourse.CourseId, primaryStudent.Id);
        AssertTrue(course.Succeeded, "Student course page should load.");
        AssertTrue(course.Value!.LearningObjectives.Count > 0, "Student course page should include course objectives.");
        AssertTrue(course.Value.Modules.Count > 0, "Student course page should include module links.");
        AssertTrue(course.Value.TermNames.Count == 2, "Student course page should include configured semester headings.");

        var syllabus = await studentService.GetSyllabusAsync(student, firstCourse.CourseId, primaryStudent.Id);
        AssertTrue(syllabus.Succeeded, "Student syllabus should load.");
        AssertFalse(string.IsNullOrWhiteSpace(syllabus.Value!.InstructionalMethods), "Syllabus should include instructional methods.");
        AssertTrue(syllabus.Value.TextsAndResources.Count > 0, "Syllabus should include course resources.");
        AssertTrue(syllabus.Value.TermNames.Count == 2, "Syllabus should include configured semester headings.");

        var module = await studentService.GetModuleAsync(student, firstCourse.CourseId, course.Value.Modules[0].ModuleId, primaryStudent.Id);
        AssertTrue(module.Succeeded, "Student module page should load.");
        AssertFalse(string.IsNullOrWhiteSpace(module.Value!.Instructions), "Module page should include instructions.");
        AssertTrue(module.Value.LearningObjectives.Count > 0, "Module page should include objectives.");
        AssertTrue(module.Value.Lessons.Count > 0, "Module page should include lessons.");
        AssertTrue(module.Value.Lessons.All(lesson => lesson.Resources.Count > 0), "Student lessons should include lesson resources.");

        var parentPreview = await studentService.GetCourseAsync(parent, firstCourse.CourseId, primaryStudent.Id);
        AssertTrue(parentPreview.Succeeded, "Parent should be able to preview student course read model.");
    }),
    ("Course details and mapping persist and coverage shows unmapped areas", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");
        var createResult = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Homestead Biology", "Biology through soil and ecology.", ["Science", "Writing"], CourseDuration.TwoSemesters, 1));
        AssertTrue(createResult.Succeeded, "Course create failed.");
        var courseId = createResult.Value;
        if (courseId == Guid.Empty)
        {
            throw new InvalidOperationException("Course id was not returned.");
        }

        AssertTrue((await courseService.SaveCourseDescriptionAsync(parent, new SaveCourseDescriptionCommand(
            courseId,
            "Biology through soil, plants, and ecology.",
            "Reading, field observation, and project work.",
            "Soil science; plant biology.",
            "Textbook and field notes.",
            "Projects and parent evaluation.",
            "Letter grade"))).Succeeded, "Description save failed.");

        AssertTrue((await courseService.SaveCurriculumPlanAsync(parent, new SaveCurriculumPlanCommand(
            courseId,
            "Understand living systems in practical contexts.",
            "Explain soil biology and plant growth.",
            "Biology text and garden records.",
            "Fall soil work, spring plant study.",
            "Practical homesteading emphasis."))).Succeeded, "Plan save failed.");

        var checklist = await requirementService.GetChecklistAsync();
        var science = checklist.First(item => item.Name == "Science" && item.View == "Statutory");
        AssertTrue((await courseService.SetRequirementMappingsAsync(parent, new SetCourseRequirementMappingsCommand(
            courseId,
            [new RequirementMappingCommand(science.RequirementAreaId, CoverageLevel.Primary, "Main science course")]))).Succeeded, "Mapping failed.");

        var detail = await courseService.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertEqual("Biology through soil, plants, and ecology.", detail.Description, "Description did not persist.");
        AssertTrue(detail.SubjectAreas.Contains("Writing"), "Multiple subject areas should persist.");
        AssertEqual(1, detail.Mappings.Count, "Mapping did not persist.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        AssertTrue(coverage.Any(item => item.Name == "Science" && item.IsMapped), "Science should be mapped.");
        AssertTrue(coverage.Any(item => item.Name == "English Grammar" && !item.IsMapped), "Unmapped areas should remain visible.");
    }),
    ("Default course pack imports once and creates multi-subject transcript courses", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");
        var firstImport = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(firstImport.Succeeded, "Course pack import failed.");
        AssertTrue(firstImport.Value > 0, "Course pack should import courses.");

        var secondImport = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(secondImport.Succeeded, "Second course pack import failed.");
        AssertEqual(0, secondImport.Value, "Second import should skip existing template courses.");

        var courses = await courseService.ListCoursesAsync();
        var english = courses.First(course => course.Title == "English Language Arts 12");
        AssertTrue(english.Description.Contains("literature", StringComparison.OrdinalIgnoreCase), "Course list should include the course description.");
        AssertTrue(english.SubjectAreas.Contains("Reading"), "ELA course should include reading.");
        AssertTrue(english.SubjectAreas.Contains("English Grammar"), "ELA course should include grammar.");
        AssertEqual((int)CourseDuration.TwoSemesters, (int)english.Duration, "ELA should be a two-semester course.");

        var government = courses.First(course => course.Title == "Government and Economics");
        AssertEqual((int)CourseDuration.TwoSemesters, (int)government.Duration, "Government and Economics should be the two-semester default social studies course.");

        var history = courses.First(course => course.Title == "U.S. History and Geography");
        AssertEqual((int)CourseDuration.TwoSemesters, (int)history.Duration, "Default history should be a two-semester course.");

        var math = courses.First(course => course.Title == "Precalculus");
        AssertTrue(math.Description.Contains("college-preparatory", StringComparison.OrdinalIgnoreCase), "Default math should be Precalculus with its description.");

        var science = courses.First(course => course.Title == "Physics");
        AssertTrue(science.Description.Contains("motion", StringComparison.OrdinalIgnoreCase), "Default science should be Physics with its description.");

        var capstone = courses.First(course => course.Title == "Experiential Capstone");
        AssertTrue(capstone.SubjectAreas.Contains("Elective"), "Capstone should behave like an elective.");

        var worldLanguage = courses.First(course => course.Title == "Spanish");
        AssertTrue(worldLanguage.SubjectAreas.Contains("World Language"), "Default world language should preserve its subject label.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        AssertEqual("Civics", coverage.First().Name, "Statutory coverage groups should be listed first.");
        AssertTrue(coverage.Any(item => item.Name == "Reading" && item.Source == "Statutory" && item.IsMapped), "Reading should be mapped through the statutory English subject rows.");
        AssertTrue(coverage.Any(item => item.Name == "Writing" && item.Source == "Statutory" && item.IsMapped), "Writing should be mapped through the statutory English subject rows.");
        AssertTrue(coverage.Any(item => item.Name == "U.S. Constitution" && item.Source == "MDE Summary" && item.IsMapped), "U.S. Constitution should be mapped by the default government and U.S. history courses.");
        AssertTrue(coverage.Any(item => item.Name == "Michigan Constitution" && item.Source == "MDE Summary" && item.IsMapped), "Michigan Constitution should be mapped by the default government and U.S. history courses.");
        var mathematics = coverage.First(item => item.Name == "Mathematics");
        AssertTrue(mathematics.Source.Contains("Statutory", StringComparison.Ordinal), "Mathematics should include statutory source.");
        AssertEqual(mathematics.CourseTitles.Count, mathematics.CourseTitles.Distinct().Count(), "Coverage summary should not duplicate course titles.");

        var governmentDetail = await courseService.GetCourseDetailAsync(government.Id);
        if (governmentDetail is null)
        {
            throw new InvalidOperationException("Government course detail was not found.");
        }

        AssertTrue(governmentDetail.Modules.Count >= 3, "Imported default course should include learning modules.");
        AssertTrue(governmentDetail.Modules.All(module => !string.IsNullOrWhiteSpace(module.Instructions)), "Imported modules should include instructions.");
    })),
    ("Selected course pack import imports only selected templates", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");
        var import = await courseService.ImportCoursePackAsync(
            parent,
            new ImportCoursePackCommand(
                DefaultCoursePacks.MichiganCollegeReadyPackId,
                [],
                [
                    new CoursePackSelectionCommand("ela-12", "ela-12"),
                    new CoursePackSelectionCommand("math-12", "calculus-i"),
                    new CoursePackSelectionCommand("science", "environmental-science")
                ]));
        AssertTrue(import.Succeeded, "Selected course pack import failed.");
        AssertEqual(3, import.Value, "Selected import should import three courses.");

        var courses = await courseService.ListCoursesAsync();
        AssertEqual(3, courses.Count, "Unexpected selected course count.");
        AssertTrue(courses.Any(course => course.Title == "English Language Arts 12"), "ELA should import.");
        AssertTrue(courses.Any(course => course.Title == "Calculus I"), "Selected math option should import.");
        AssertTrue(courses.Any(course => course.Title == "Environmental Science"), "Selected science option should import.");
        AssertFalse(courses.Any(course => course.Title == "Physics"), "Unselected default science option should not import.");
    })),
    ("Michigan course pack import repairs stale requirement seed", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var staleAreas = MichiganRequirementSeed.CreateAreas()
            .Where(area => area.View != "MMC Reference")
            .ToArray();
        await repository.SaveRequirementSeedAsync(MichiganRequirementSeed.CreateSet(), staleAreas);

        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var import = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(import.Succeeded, "Course pack import should repair missing Michigan requirement areas.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        var personalFinance = coverage.First(item => item.Name == "Personal Finance");
        AssertTrue(personalFinance.Source.Contains("MMC Reference", StringComparison.Ordinal), "MMC personal finance reference should be restored.");
        AssertTrue(personalFinance.IsMapped, "Personal Finance should be mapped after seed repair.");
    })),
    ("Imported course mappings migrate away from stale duplicate requirement rows", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var requirementSet = MichiganRequirementSeed.CreateSet();
        var staleEnglishMde = new RequirementArea(
            DeterministicGuid("MDE Summary:English Language Arts"),
            requirementSet.Id,
            "English Language Arts",
            "",
            "All grades",
            "Guidance",
            "MDE Summary");
        var staleEnglishMmc = new RequirementArea(
            DeterministicGuid("MMC Reference:English Language Arts"),
            requirementSet.Id,
            "English Language Arts",
            "",
            "High school",
            "Reference",
            "MMC Reference");
        var staleAreas = MichiganRequirementSeed.CreateAreas()
            .Concat([staleEnglishMde, staleEnglishMmc])
            .ToArray();
        await repository.SaveRequirementSeedAsync(requirementSet, staleAreas);

        var courseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "English Language Arts 12",
            ["English Language Arts"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "ela-12",
            null,
            null,
            [
                new RequirementMapping(Guid.NewGuid(), courseId, staleEnglishMde.Id, CoverageLevel.Primary, "Old MDE mapping."),
                new RequirementMapping(Guid.NewGuid(), courseId, staleEnglishMmc.Id, CoverageLevel.Primary, "Old MMC mapping.")
            ]));

        var service = new CourseService(repository);
        var coverage = await service.GetCoverageSummaryAsync();
        AssertFalse(coverage.Any(item => item.Name == "English Language Arts"), "Duplicate ELA requirement rows should be removed from coverage lists.");
        AssertTrue(coverage.Any(item => item.Name == "Reading" && item.Source == "Statutory" && item.IsMapped), "Imported ELA course should migrate to statutory Reading coverage.");
        AssertTrue(coverage.Any(item => item.Name == "Writing" && item.Source == "Statutory" && item.IsMapped), "Imported ELA course should migrate to statutory Writing coverage.");

        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertFalse(detail.Mappings.Any(mapping => mapping.RequirementAreaName == "English Language Arts"), "Stale ELA mappings should be removed from imported course details.");
    })),
    ("Imported courses backfill default-pack modules without replacing parent modules", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        var parentModule = new LearningModule(
            Guid.NewGuid(),
            courseId,
            "",
            1,
            "Parent custom module",
            "Parent-created module.",
            "1 week",
            "Parent-created instructions.",
            "Parent topic",
            "Explain the parent-selected topic.",
            "Parent resource",
            "Parent evidence placeholder.",
            ModuleStatus.Active);
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Precalculus",
            ["Mathematics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "math-12",
            null,
            null,
            [],
            [parentModule]));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertTrue(detail.Modules.Any(module => module.Title == "Parent custom module" && module.Instructions == "Parent-created instructions."), "Backfill should preserve parent-created module text.");
        AssertTrue(detail.Modules.Any(module => !string.IsNullOrWhiteSpace(module.SourceModuleId)), "Backfill should add built-in pack modules.");
        AssertTrue(detail.Modules.Where(module => !string.IsNullOrWhiteSpace(module.SourceModuleId)).All(module => module.Lessons.Count > 0), "Backfilled source modules should include lessons.");
        AssertTrue(detail.Modules.SelectMany(module => module.Lessons).All(lesson => lesson.Resources.Count > 0), "Backfilled lessons should include resources.");
        AssertTrue(detail.Modules.Count > 1, "Backfill should keep parent module and add starter modules.");
    })),
    ("Imported source modules with no lessons are backfilled", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        var existingPackModule = new LearningModule(
            Guid.NewGuid(),
            courseId,
            "personal-finance-module-1",
            1,
            "Budgeting and related topics",
            "Existing imported module without lesson rows.",
            "3-5 weeks",
            "Existing module instructions.",
            "",
            "Explain budgeting and credit decisions.",
            "",
            "Existing evidence placeholder.",
            ModuleStatus.Planned,
            null,
            [new ModuleLearningObjective("Explain budgeting and credit decisions.", "")],
            [],
            []);
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Personal Finance",
            ["Mathematics"],
            CourseDuration.OneSemester,
            0.5m,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "personal-finance",
            null,
            null,
            [],
            [existingPackModule]));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        var backfilledModule = detail.Modules.First(module => module.SourceModuleId == "personal-finance-module-1");
        AssertTrue(backfilledModule.Lessons.Count > 0, "Existing imported source modules with empty lessons should be backfilled.");
        AssertTrue(backfilledModule.Lessons.All(lesson => lesson.Resources.Count > 0), "Backfilled source lessons should include resources.");
    })),
    ("Current imported modules with stripped lessons are detected as changed", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var import = await service.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(import.Succeeded, "Default pack import should succeed.");

        var importedCourse = (await repository.GetCoursesAsync()).First(course =>
            string.Equals(course.SourceTemplateId, "personal-finance", StringComparison.OrdinalIgnoreCase));
        AssertTrue(importedCourse.Modules.All(module => module.Lessons.Count > 0), "Imported modules should start with lessons.");

        var strippedModules = importedCourse.Modules
            .Select(module => module.WithLessons([]))
            .ToArray();
        await repository.SaveCourseAsync(importedCourse.WithModules(strippedModules));
        var strippedCourse = await repository.GetCourseAsync(importedCourse.Id);
        AssertTrue(strippedCourse?.Modules.All(module => module.Lessons.Count == 0) ?? false, "Test setup should strip lessons from otherwise current imported modules.");

        var detail = await service.GetCourseDetailAsync(importedCourse.Id);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertTrue(detail.Modules.All(module => module.Lessons.Count > 0), "Backfill comparison should detect missing nested lessons and save the repair.");
        AssertTrue(detail.Modules.SelectMany(module => module.Lessons).All(lesson => lesson.Resources.Count > 0), "Repaired lessons should include resources.");
    })),
    ("Imported government and U.S. history courses backfill constitution mappings", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");

        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var areas = await repository.GetRequirementAreasAsync();
        var civics = areas.First(area => area.View == "Statutory" && area.Name == "Civics");
        var history = areas.First(area => area.View == "Statutory" && area.Name == "History");

        var governmentCourseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            governmentCourseId,
            student.Id,
            schoolYear.Id,
            "Government and Economics",
            ["Social Studies", "Civics", "Economics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "social-studies",
            null,
            null,
            [new RequirementMapping(Guid.NewGuid(), governmentCourseId, civics.Id, CoverageLevel.Primary, "Old civics mapping.")]));

        var historyCourseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            historyCourseId,
            student.Id,
            schoolYear.Id,
            "U.S. History and Geography",
            ["History", "Social Studies"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "history",
            null,
            null,
            [new RequirementMapping(Guid.NewGuid(), historyCourseId, history.Id, CoverageLevel.Primary, "Old history mapping.")]));

        var courseService = new CourseService(repository);
        var governmentDetail = await courseService.GetCourseDetailAsync(governmentCourseId);
        var historyDetail = await courseService.GetCourseDetailAsync(historyCourseId);
        if (governmentDetail is null || historyDetail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertTrue(governmentDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "U.S. Constitution"), "Government course should backfill U.S. Constitution coverage.");
        AssertTrue(governmentDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "Michigan Constitution"), "Government course should backfill Michigan Constitution coverage.");
        AssertTrue(historyDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "U.S. Constitution"), "U.S. history course should backfill U.S. Constitution coverage.");
        AssertTrue(historyDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "Michigan Constitution"), "U.S. history course should backfill Michigan Constitution coverage.");
    })),
    ("Imported course detail backfill fills blank pack fields without overwriting parent text", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Precalculus",
            ["Mathematics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "math-12",
            new CourseDescription("Parent custom description.", "", "", "", "", ""),
            CurriculumPlan.Empty,
            []));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertEqual("Parent custom description.", detail.Description, "Backfill should not overwrite parent description.");
        AssertTrue(detail.MajorTopics.Contains("Advanced functions", StringComparison.OrdinalIgnoreCase), "Backfill should fill major topics.");
        AssertTrue(detail.TextsAndResources.Contains("OpenStax Precalculus", StringComparison.OrdinalIgnoreCase), "Backfill should fill resources.");
        AssertTrue(detail.InstructionalMethods.Contains("Hybrid", StringComparison.OrdinalIgnoreCase), "Backfill should fill instructional methods.");
        AssertTrue(detail.AssessmentMethods.Contains("formative", StringComparison.OrdinalIgnoreCase), "Backfill should fill assessment methods.");
        AssertTrue(detail.GradingBasis.Contains("Mastery", StringComparison.OrdinalIgnoreCase), "Backfill should fill grading basis.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.Goals), "Backfill should fill curriculum goals.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.LearningObjectives), "Backfill should fill learning objectives.");
        AssertTrue(string.IsNullOrWhiteSpace(detail.MajorResources), "Backfill should not restore retired curriculum resources.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.PlannedSequence), "Backfill should fill planned sequence.");
    })),
    ("Imported course detail backfill upgrades legacy pack defaults", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Precalculus",
            ["Mathematics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "math-12",
            new CourseDescription(
                "Parent custom description.",
                "Explicit instruction with guided practice, discussion, independent reading or problem work, applied projects, and parent feedback.",
                "Advanced functions; trigonometry.",
                "OpenStax Precalculus; Khan Academy Precalculus; CK-12 Precalculus; Desmos graphing activities.",
                "Ongoing formative checks, reviewed assignments, discussion or conference notes, quizzes or problem sets where appropriate.",
                "Mastery-aligned letter grade using parent-reviewed evidence."),
            new CurriculumPlan(
                "Parent custom goals.",
                "Explain major concepts in Precalculus; apply course skills in written, oral, practical, or problem-based work; use appropriate vocabulary and resources; and produce evidence suitable for course records.",
                "OpenStax Precalculus; Khan Academy Precalculus; CK-12 Precalculus; Desmos graphing activities.",
                "Parent custom sequence.",
                "Parent custom notes."),
            []));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertEqual("Parent custom description.", detail.Description, "Backfill should not overwrite parent description.");
        AssertTrue(detail.TextsAndResources.Contains('|'), "Legacy resources should upgrade to linked item rows.");
        AssertTrue(detail.InstructionalMethods.Contains("Hybrid", StringComparison.OrdinalIgnoreCase), "Legacy instructional methods should upgrade to the hybrid default.");
        AssertTrue(detail.AssessmentMethods.Contains("Hybrid", StringComparison.OrdinalIgnoreCase), "Legacy assessment methods should upgrade to the hybrid default.");
        AssertTrue(detail.GradingBasis.Contains("Hybrid", StringComparison.OrdinalIgnoreCase), "Legacy grading basis should upgrade to the hybrid default.");
        AssertTrue(detail.LearningObjectives.Split(Environment.NewLine).Length >= 3, "Legacy learning objectives should upgrade to separated objective rows.");
        AssertFalse(detail.LearningObjectives.Contains("produce evidence suitable for course records", StringComparison.OrdinalIgnoreCase), "Legacy learning objectives should upgrade away from generic recordkeeping language.");
        AssertEqual("Parent custom goals.", detail.Goals, "Backfill should not overwrite parent goals.");
        AssertEqual("Parent custom sequence.", detail.PlannedSequence, "Backfill should not overwrite parent sequence.");
    }))
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        await test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Failures:");
    foreach (var failure in failures)
    {
        Console.WriteLine(failure);
    }

    return 1;
}

return 0;

static async Task<JsonHomeschoolRepository> CreateRepositoryAsync()
{
    var root = Path.Combine(Path.GetTempPath(), "HomeschoolManagerTests", Guid.NewGuid().ToString("N"));
    var options = Options.Create(new HomeschoolManagerOptions
    {
        DataRoot = root,
        UseDevelopmentDataRoot = true
    });
    var repository = new JsonHomeschoolRepository(new AppDataPaths(options));
    await repository.EnsureStoreCreatedAsync();
    return repository;
}

static async Task CreateSetupAsync(JsonHomeschoolRepository repository)
{
    var setupService = new SetupService(repository);
    var parent = UserContext.ParentAdmin("Parent");
    AssertTrue((await setupService.CreateHouseholdAsync(parent, new CreateHouseholdCommand("Family", "Parent"))).Succeeded, "Household setup failed.");
    AssertTrue((await setupService.CreateStudentAsync(parent, new CreateStudentCommand("Student", "Learner", 12))).Succeeded, "Student setup failed.");
    AssertTrue((await setupService.ConfigureSchoolYearAsync(parent, new ConfigureSchoolYearCommand(
        "2026-2027",
        2026,
        2027,
        new DateOnly(2026, 8, 24),
        new DateOnly(2026, 12, 18),
        new DateOnly(2027, 1, 11),
        new DateOnly(2027, 5, 28)))).Succeeded, "School year setup failed.");
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message)
{
    AssertTrue(!condition, message);
}

static void AssertEqual<T>(T expected, T actual, string message)
    where T : IEquatable<T>
{
    if (!expected.Equals(actual))
    {
        throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
    }
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

static Guid DeterministicGuid(string value)
{
    var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
    return new Guid(bytes);
}

static IReadOnlyList<ModuleLearningObjectiveCommand> Objectives(params string[] objectives)
{
    return objectives.Select(objective => new ModuleLearningObjectiveCommand(objective, "")).ToArray();
}

static IReadOnlyList<ModuleResourceCommand> Resources(params string[] resources)
{
    return resources.Select(resource => new ModuleResourceCommand(resource, "", "", true)).ToArray();
}

static IReadOnlyList<LessonResourceCommand> LessonResources(params string[] resources)
{
    return resources
        .Select(resource => new LessonResourceCommand(
            resource,
            LessonResourceType.Reading,
            "https://example.com/resource",
            "",
            false,
            "Test resource."))
        .ToArray();
}
