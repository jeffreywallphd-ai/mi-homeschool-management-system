using System.Text.Json;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class JsonHomeschoolRepository : IHomeschoolRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly AppDataPaths paths;
    private readonly SemaphoreSlim gate = new(1, 1);

    public JsonHomeschoolRepository(AppDataPaths paths)
    {
        this.paths = paths;
    }

    public async Task EnsureStoreCreatedAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(paths.DataDirectory);
            if (!File.Exists(paths.DatabasePath))
            {
                await SaveUnlockedAsync(new AppDataDocument(), cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<Household?> GetHouseholdAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.Household;
    }

    public async Task SaveHouseholdAsync(Household household, CancellationToken cancellationToken = default)
    {
        await MutateAsync(document => document.Household = household, cancellationToken);
    }

    public async Task<SchoolProfile?> GetSchoolProfileAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.SchoolProfile;
    }

    public async Task SaveSchoolProfileAsync(SchoolProfile schoolProfile, CancellationToken cancellationToken = default)
    {
        await MutateAsync(document => document.SchoolProfile = schoolProfile, cancellationToken);
    }

    public async Task<Student?> GetStudentAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.Student;
    }

    public async Task SaveStudentAsync(Student student, CancellationToken cancellationToken = default)
    {
        await MutateAsync(document => document.Student = student, cancellationToken);
    }

    public async Task<SchoolYear?> GetSchoolYearAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.SchoolYear;
    }

    public async Task SaveSchoolYearAsync(SchoolYear schoolYear, CancellationToken cancellationToken = default)
    {
        await MutateAsync(document => document.SchoolYear = schoolYear, cancellationToken);
    }

    public async Task<IReadOnlyList<RequirementSet>> GetRequirementSetsAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.RequirementSets;
    }

    public async Task<IReadOnlyList<RequirementArea>> GetRequirementAreasAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.RequirementAreas;
    }

    public async Task SaveRequirementSeedAsync(
        RequirementSet requirementSet,
        IReadOnlyList<RequirementArea> requirementAreas,
        CancellationToken cancellationToken = default)
    {
        await MutateAsync(
            document =>
            {
                document.RequirementSets.RemoveAll(set => set.Id == requirementSet.Id);
                document.RequirementSets.Add(requirementSet);

                var seedAreaIds = requirementAreas.Select(area => area.Id).ToHashSet();
                document.RequirementAreas.RemoveAll(area => seedAreaIds.Contains(area.Id));
                document.RequirementAreas.AddRange(requirementAreas);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.Courses;
    }

    public async Task<Course?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        return document.Courses.FirstOrDefault(course => course.Id == courseId);
    }

    public async Task SaveCourseAsync(Course course, CancellationToken cancellationToken = default)
    {
        await MutateAsync(
            document =>
            {
                document.Courses.RemoveAll(existing => existing.Id == course.Id);
                document.Courses.Add(course);
            },
            cancellationToken);
    }

    private async Task<AppDataDocument> LoadAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(paths.DataDirectory);
            if (!File.Exists(paths.DatabasePath))
            {
                var created = new AppDataDocument();
                await SaveUnlockedAsync(created, cancellationToken);
                return created;
            }

            await using var stream = File.OpenRead(paths.DatabasePath);
            return await JsonSerializer.DeserializeAsync<AppDataDocument>(stream, SerializerOptions, cancellationToken)
                ?? new AppDataDocument();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task MutateAsync(Action<AppDataDocument> mutate, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(paths.DataDirectory);
            AppDataDocument document;
            if (File.Exists(paths.DatabasePath))
            {
                await using var readStream = File.OpenRead(paths.DatabasePath);
                document = await JsonSerializer.DeserializeAsync<AppDataDocument>(readStream, SerializerOptions, cancellationToken)
                    ?? new AppDataDocument();
            }
            else
            {
                document = new AppDataDocument();
            }

            mutate(document);
            await SaveUnlockedAsync(document, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task SaveUnlockedAsync(AppDataDocument document, CancellationToken cancellationToken)
    {
        var tempPath = $"{paths.DatabasePath}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
        }

        File.Move(tempPath, paths.DatabasePath, true);
    }
}
