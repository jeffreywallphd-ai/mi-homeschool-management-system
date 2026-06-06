using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CourseListItem(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    int MappingCount);

public sealed record CourseRequirementMappingView(
    Guid RequirementAreaId,
    string RequirementAreaName,
    string RequirementView,
    CoverageLevel CoverageLevel,
    string Notes);

public sealed record CourseDetail(
    Guid Id,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string Description,
    string InstructionalMethods,
    string MajorTopics,
    string TextsAndResources,
    string AssessmentMethods,
    string GradingBasis,
    string Goals,
    string LearningObjectives,
    string MajorResources,
    string PlannedSequence,
    string ParentNotes,
    IReadOnlyList<CourseRequirementMappingView> Mappings);

public sealed record CoverageSummaryItem(
    Guid RequirementAreaId,
    string Source,
    string Name,
    string GradeBand,
    bool IsMapped,
    CoverageLevel? CoverageLevel,
    IReadOnlyList<string> CourseTitles);

public sealed record CoursePackSummary(
    string Id,
    string Name,
    string Description,
    int CourseCount,
    decimal TotalPlannedCredits);

public sealed record CoursePackDetail(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<CoursePackCourseView> Courses);

public sealed record CoursePackCourseView(
    string TemplateId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string Description,
    string DefaultOptionId,
    IReadOnlyList<CoursePackOptionView> Options);

public sealed record CoursePackOptionView(
    string OptionId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string Description);
