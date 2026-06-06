using HomeschoolManager.Application.Requirements;
using HomeschoolManager.Application.Setup;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
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
