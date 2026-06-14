using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Application.Courses;
using HomeschoolManager.Domain.Submissions;
using HomeschoolManager.Domain.Assessments;
using HomeschoolManager.Domain.Records;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class AppDataDocument
{
    public int SchemaVersion { get; set; } = 1;

    public Household? Household { get; set; }

    public SchoolProfile? SchoolProfile { get; set; }

    public Student? Student { get; set; }

    public List<Student> Students { get; set; } = [];

    public SchoolYear? SchoolYear { get; set; }

    public List<SchoolYear> SchoolYears { get; set; } = [];

    public List<RequirementSet> RequirementSets { get; set; } = [];

    public List<RequirementArea> RequirementAreas { get; set; } = [];

    public List<Course> Courses { get; set; } = [];

    public List<AssignmentSubmission> AssignmentSubmissions { get; set; } = [];

    public List<EvidenceRecord> EvidenceRecords { get; set; } = [];

    public List<PortfolioDraftItem> PortfolioDraftItems { get; set; } = [];

    public List<PortfolioDesign> PortfolioDesigns { get; set; } = [];

    public List<AssessmentRecord> AssessmentRecords { get; set; } = [];

    public List<TranscriptCourseRecord> TranscriptCourseRecords { get; set; } = [];

    public List<GraduationPlan> GraduationPlans { get; set; } = [];

    public List<DiplomaDesign> DiplomaDesigns { get; set; } = [];

    public List<CoursePackDefinition> InstalledCoursePacks { get; set; } = [];
}
