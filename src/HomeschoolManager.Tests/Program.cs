using HomeschoolManager.Application.Courses;
using HomeschoolManager.Application.Requirements;
using HomeschoolManager.Application.Setup;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Household;
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
        AssertEqual(26, checklist.Count, "Unexpected seeded area count.");
        AssertEqual("Statutory", checklist[0].View, "Statutory requirements should be listed first.");
        AssertTrue(checklist.Any(item => item.View == "Statutory" && item.Name == "English Grammar"), "Missing statutory view area.");
        AssertTrue(checklist.Any(item => item.View == "MDE Summary" && item.Name == "English Language Arts"), "Missing normalized MDE English Language Arts area.");
        AssertTrue(checklist.Any(item => item.View == "MDE Summary" && item.Name == "Civics"), "Missing normalized MDE Civics area.");
        AssertTrue(checklist.Any(item => item.View == "MMC Reference" && item.Name == "English Language Arts"), "Missing MMC reference view area.");
    }),
    ("Default Michigan course pack mappings match seeded requirement areas", () =>
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
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.InstructionalMethods), $"Missing instructional methods for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.AssessmentMethods), $"Missing assessment methods for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.GradingBasis), $"Missing grading basis for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.Goals), $"Missing curriculum goals for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.LearningObjectives), $"Missing learning objectives for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.MajorResources), $"Missing major resources for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.PlannedSequence), $"Missing planned sequence for {template.TemplateId}/{option.OptionId}.");
            }
        }

        return Task.CompletedTask;
    }),
    ("Student role cannot seed Michigan requirements", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new RequirementService(repository);
        var result = await service.SeedMichiganAsync(UserContext.Student("Student"));
        AssertFalse(result.Succeeded, "Student seed command should fail.");
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
        var englishLanguageArts = coverage.First(item => item.Name == "English Language Arts");
        AssertTrue(englishLanguageArts.Source.Contains("MDE Summary", StringComparison.Ordinal), "English Language Arts should include MDE summary source.");
        AssertTrue(englishLanguageArts.Source.Contains("MMC Reference", StringComparison.Ordinal), "English Language Arts should include MMC reference source.");
        AssertTrue(englishLanguageArts.IsMapped, "English Language Arts should be mapped.");
        var mathematics = coverage.First(item => item.Name == "Mathematics");
        AssertTrue(mathematics.Source.Contains("Statutory", StringComparison.Ordinal), "Mathematics should include statutory source.");
        AssertTrue(mathematics.Source.Contains("MDE Summary", StringComparison.Ordinal), "Mathematics should include MDE summary source.");
        AssertEqual(mathematics.CourseTitles.Count, mathematics.CourseTitles.Distinct().Count(), "Coverage summary should not duplicate course titles.");
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
        var englishLanguageArts = coverage.First(item => item.Name == "English Language Arts");
        AssertTrue(englishLanguageArts.Source.Contains("MMC Reference", StringComparison.Ordinal), "MMC ELA reference should be restored.");
        AssertTrue(englishLanguageArts.IsMapped, "English Language Arts should be mapped after seed repair.");
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
        AssertTrue(detail.InstructionalMethods.Contains("Explicit instruction", StringComparison.OrdinalIgnoreCase), "Backfill should fill instructional methods.");
        AssertTrue(detail.AssessmentMethods.Contains("formative", StringComparison.OrdinalIgnoreCase), "Backfill should fill assessment methods.");
        AssertTrue(detail.GradingBasis.Contains("Mastery", StringComparison.OrdinalIgnoreCase), "Backfill should fill grading basis.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.Goals), "Backfill should fill curriculum goals.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.LearningObjectives), "Backfill should fill learning objectives.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.MajorResources), "Backfill should fill curriculum resources.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.PlannedSequence), "Backfill should fill planned sequence.");
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
