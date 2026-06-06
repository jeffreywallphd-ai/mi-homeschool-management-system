using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CourseListItem(
    Guid Id,
    string Title,
    string SubjectArea,
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
    string SubjectArea,
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
    string View,
    string Name,
    string GradeBand,
    bool IsMapped,
    CoverageLevel? CoverageLevel,
    IReadOnlyList<string> CourseTitles);
