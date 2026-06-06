using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record StudentCourseCard(
    Guid StudentId,
    Guid CourseId,
    string Title,
    string Description,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string CurrentGrade,
    int ModuleCount,
    int CompletedModuleCount,
    IReadOnlyList<string> TermNames);

public sealed record StudentCoursePage(
    Guid StudentId,
    string StudentFirstName,
    Guid CourseId,
    string Title,
    string Description,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string CurrentGrade,
    IReadOnlyList<string> TermNames,
    IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<StudentModuleLink> Modules);

public sealed record StudentCourseSyllabus(
    Guid StudentId,
    Guid CourseId,
    string Title,
    string Description,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string InstructionalMethods,
    IReadOnlyList<StudentResourceView> TextsAndResources,
    string AssessmentMethods,
    string GradingBasis,
    string Goals,
    IReadOnlyList<string> LearningObjectives,
    string PlannedSequence,
    string ParentNotes,
    IReadOnlyList<string> TermNames,
    IReadOnlyList<StudentModuleLink> Modules);

public sealed record StudentModulePage(
    Guid StudentId,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    Guid? PreviousModuleId,
    Guid? NextModuleId,
    int SequenceOrder,
    string Title,
    string Description,
    string TermName,
    string EstimatedLength,
    ModuleStatus Status,
    string Instructions,
    IReadOnlyList<StudentModuleObjectiveView> LearningObjectives,
    IReadOnlyList<StudentModuleResourceView> Resources,
    IReadOnlyList<StudentLessonView> Lessons,
    IReadOnlyList<StudentAssignmentView> Assignments,
    string AssignmentEvidencePlaceholder);

public sealed record StudentModuleLink(
    Guid ModuleId,
    int SequenceOrder,
    string Title,
    string TermName,
    ModuleStatus Status);

public sealed record StudentCourseDashboard(
    Guid StudentId,
    string StudentFirstName,
    IReadOnlyList<string> TermNames,
    IReadOnlyList<StudentCourseCard> Courses);

public sealed record StudentModuleObjectiveView(string Text, string LinkedCourseObjective);

public sealed record StudentModuleResourceView(
    string Name,
    string Link,
    string FileName,
    bool IsPhysicalResource);

public sealed record StudentResourceView(string Name, string Link);

public sealed record StudentLessonView(
    int SequenceOrder,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<StudentLessonResourceView> Resources);

public sealed record StudentLessonResourceView(
    string Name,
    LessonResourceType Type,
    string Url,
    string FileName,
    bool IsPhysicalResource,
    string SourceNote);

public sealed record StudentAssignmentView(
    int SequenceOrder,
    string Title,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Instructions,
    string EstimatedEffort,
    string DueTimingLabel,
    DateOnly? DueDate,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<string> RelatedLessonTitles,
    string RequiredOutput,
    bool IsPortfolioCandidate,
    AssignmentStatus Status);
