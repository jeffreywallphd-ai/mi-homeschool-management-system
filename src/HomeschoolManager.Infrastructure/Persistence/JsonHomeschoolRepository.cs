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

                document.RequirementAreas.RemoveAll(area =>
                    area.RequirementSetId == requirementSet.Id &&
                    !string.Equals(area.RequiredOrRecommended, "Parent", StringComparison.OrdinalIgnoreCase));
                document.RequirementAreas.AddRange(requirementAreas);
            },
            cancellationToken);
    }

    public async Task SaveRequirementAreaAsync(
        RequirementArea requirementArea,
        CancellationToken cancellationToken = default)
    {
        await MutateAsync(
            document =>
            {
                document.RequirementAreas.RemoveAll(existing => existing.Id == requirementArea.Id);
                document.RequirementAreas.Add(requirementArea);
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
        var tempPath = $"{paths.DatabasePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
            }

            await MoveWithRetryAsync(tempPath, paths.DatabasePath, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                TryDeleteTempFile(tempPath);
            }
        }
    }

    private static async Task MoveWithRetryAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                File.Move(sourcePath, destinationPath, true);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientFileAccess(ex))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
        }
    }

    private static bool IsTransientFileAccess(Exception ex)
    {
        return ex is IOException or UnauthorizedAccessException;
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            File.Delete(tempPath);
        }
        catch (Exception ex) when (IsTransientFileAccess(ex))
        {
            // A later save can safely ignore a stale unique temp file.
        }
    }
}
