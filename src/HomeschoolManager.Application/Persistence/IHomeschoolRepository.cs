using HomeschoolManager.Application.Courses;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Domain.Submissions;
using HomeschoolManager.Domain.Assessments;

namespace HomeschoolManager.Application.Persistence;

public interface IHomeschoolRepository
{
    Task<Household?> GetHouseholdAsync(CancellationToken cancellationToken = default);

    Task SaveHouseholdAsync(Household household, CancellationToken cancellationToken = default);

    Task<SchoolProfile?> GetSchoolProfileAsync(CancellationToken cancellationToken = default);

    Task SaveSchoolProfileAsync(SchoolProfile schoolProfile, CancellationToken cancellationToken = default);

    Task<Student?> GetStudentAsync(CancellationToken cancellationToken = default);

    Task<Student?> GetStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default);

    Task SaveStudentAsync(Student student, CancellationToken cancellationToken = default);

    Task<SchoolYear?> GetSchoolYearAsync(CancellationToken cancellationToken = default);

    Task SaveSchoolYearAsync(SchoolYear schoolYear, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequirementSet>> GetRequirementSetsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequirementArea>> GetRequirementAreasAsync(CancellationToken cancellationToken = default);

    Task SaveRequirementSeedAsync(
        RequirementSet requirementSet,
        IReadOnlyList<RequirementArea> requirementAreas,
        CancellationToken cancellationToken = default);

    Task SaveRequirementAreaAsync(RequirementArea requirementArea, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default);

    Task<Course?> GetCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task SaveCourseAsync(Course course, CancellationToken cancellationToken = default);

    Task DeleteCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignmentSubmission>> GetAssignmentSubmissionsAsync(CancellationToken cancellationToken = default);

    Task<AssignmentSubmission?> GetAssignmentSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);

    Task SaveAssignmentSubmissionAsync(AssignmentSubmission submission, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceRecord>> GetEvidenceRecordsAsync(CancellationToken cancellationToken = default);

    Task SaveEvidenceRecordAsync(EvidenceRecord evidenceRecord, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortfolioDraftItem>> GetPortfolioDraftItemsAsync(CancellationToken cancellationToken = default);

    Task<PortfolioDraftItem?> GetPortfolioDraftItemAsync(Guid portfolioDraftItemId, CancellationToken cancellationToken = default);

    Task SavePortfolioDraftItemAsync(PortfolioDraftItem item, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssessmentRecord>> GetAssessmentRecordsAsync(CancellationToken cancellationToken = default);

    Task<AssessmentRecord?> GetAssessmentRecordAsync(Guid assessmentId, CancellationToken cancellationToken = default);

    Task SaveAssessmentRecordAsync(AssessmentRecord assessmentRecord, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoursePackDefinition>> GetInstalledCoursePacksAsync(CancellationToken cancellationToken = default);

    Task SaveInstalledCoursePackAsync(CoursePackDefinition coursePack, CancellationToken cancellationToken = default);

    Task EnsureStoreCreatedAsync(CancellationToken cancellationToken = default);
}
