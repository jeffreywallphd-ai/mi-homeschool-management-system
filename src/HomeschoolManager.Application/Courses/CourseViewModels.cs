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
    IReadOnlyList<CourseTermView> Terms,
    IReadOnlyList<LearningModuleView> Modules,
    IReadOnlyList<CourseRequirementMappingView> Mappings);

public sealed record CourseTermView(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate);

public sealed record LearningModuleView(
    Guid Id,
    Guid CourseId,
    string SourceModuleId,
    int SequenceOrder,
    string Title,
    string Description,
    Guid? TermId,
    string TermName,
    string EstimatedLength,
    string Instructions,
    string MajorTopics,
    string LearningObjectives,
    IReadOnlyList<ModuleLearningObjectiveView> LearningObjectiveItems,
    string Resources,
    IReadOnlyList<ModuleResourceView> ResourceItems,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status,
    IReadOnlyList<LessonView> Lessons,
    IReadOnlyList<AssignmentView> Assignments,
    IReadOnlyList<AssignmentVariantView> AssignmentVariants);

public sealed record ModuleLearningObjectiveView(string Text, string LinkedCourseObjective);

public sealed record ModuleResourceView(string Name, string Link, string FilePath, bool IsPhysicalResource);

public sealed record LessonView(
    Guid Id,
    Guid ModuleId,
    string SourceLessonId,
    int SequenceOrder,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<LessonResourceView> Resources);

public sealed record LessonResourceView(
    Guid Id,
    string Name,
    LessonResourceType Type,
    string Url,
    string FilePath,
    bool IsPhysicalResource,
    string SourceNote);

public sealed record AssignmentView(
    Guid Id,
    Guid ModuleId,
    string SourceAssignmentId,
    int SequenceOrder,
    string Title,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Instructions,
    string EstimatedEffort,
    string DueTimingLabel,
    DateOnly? DueDate,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<Guid> LinkedLessonIds,
    string RequiredOutput,
    string ParentNotes,
    bool IsPortfolioCandidate,
    decimal? PlannedPoints,
    decimal? PlannedWeight,
    AssignmentStatus Status);

public sealed record AssignmentVariantView(
    string VariantId,
    string SourceAssignmentId,
    string Title,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Instructions,
    string EstimatedEffort,
    string DueTimingLabel,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<string> LinkedSourceLessonIds,
    string RequiredOutput,
    string ParentNotes,
    bool IsPortfolioCandidate,
    decimal? PlannedPoints,
    decimal? PlannedWeight,
    AssignmentStatus Status);

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

public sealed record CoursePackExportFile(
    string FileName,
    string ContentType,
    byte[] Content,
    bool IsArchive);

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
