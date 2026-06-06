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
        AssertEqual(17, checklist.Count, "Unexpected seeded area count.");
        AssertTrue(checklist.Any(item => item.View == "Statutory" && item.Name == "English Grammar"), "Missing statutory view area.");
        AssertTrue(checklist.Any(item => item.View == "MDE Summary" && item.Name == "Michigan Constitution"), "Missing MDE summary view area.");
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
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "", "Science", 1, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", "Science", 0, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", "Science", 4, null, null, []));
        _ = new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", "Science", 1, null, null, []);
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
        var course = new Course(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Biology", "Science", 1, null, null, []);
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
            new CreateCourseCommand("Biology", "Science", 1));
        AssertFalse(studentResult.Succeeded, "Student course creation should fail.");

        var parentResult = await service.CreateCourseAsync(
            UserContext.ParentAdmin("Parent"),
            new CreateCourseCommand("Biology", "Science", 1));
        AssertTrue(parentResult.Succeeded, "Parent course creation should pass.");

        var courses = await service.ListCoursesAsync();
        AssertEqual(1, courses.Count, "Unexpected course count.");
        AssertEqual("Biology", courses[0].Title, "Unexpected course title.");
    }),
    ("Course details and mapping persist and coverage shows unmapped areas", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");
        var createResult = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Homestead Biology", "Science", 1));
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
        AssertEqual(1, detail.Mappings.Count, "Mapping did not persist.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        AssertTrue(coverage.Any(item => item.Name == "Science" && item.IsMapped), "Science should be mapped.");
        AssertTrue(coverage.Any(item => item.Name == "English Grammar" && !item.IsMapped), "Unmapped areas should remain visible.");
    })
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
