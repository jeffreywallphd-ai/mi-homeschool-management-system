using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;

namespace HomeschoolManager.Application.Persistence;

public interface IHomeschoolRepository
{
    Task<Household?> GetHouseholdAsync(CancellationToken cancellationToken = default);

    Task SaveHouseholdAsync(Household household, CancellationToken cancellationToken = default);

    Task<SchoolProfile?> GetSchoolProfileAsync(CancellationToken cancellationToken = default);

    Task SaveSchoolProfileAsync(SchoolProfile schoolProfile, CancellationToken cancellationToken = default);

    Task<Student?> GetStudentAsync(CancellationToken cancellationToken = default);

    Task SaveStudentAsync(Student student, CancellationToken cancellationToken = default);

    Task<SchoolYear?> GetSchoolYearAsync(CancellationToken cancellationToken = default);

    Task SaveSchoolYearAsync(SchoolYear schoolYear, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequirementSet>> GetRequirementSetsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequirementArea>> GetRequirementAreasAsync(CancellationToken cancellationToken = default);

    Task SaveRequirementSeedAsync(
        RequirementSet requirementSet,
        IReadOnlyList<RequirementArea> requirementAreas,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default);

    Task<Course?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task SaveCourseAsync(Course course, CancellationToken cancellationToken = default);

    Task EnsureStoreCreatedAsync(CancellationToken cancellationToken = default);
}
