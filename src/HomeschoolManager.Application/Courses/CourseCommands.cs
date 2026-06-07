using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CreateCourseCommand(
    string Title,
    string Description,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    Guid? StudentId = null);

public sealed record UpdateCourseCommand(
    Guid CourseId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue);

public sealed record SaveCourseDescriptionCommand(
    Guid CourseId,
    string Description,
    string InstructionalMethods,
    string MajorTopics,
    string TextsAndResources,
    string AssessmentMethods,
    string GradingBasis);

public sealed record SaveCurriculumPlanCommand(
    Guid CourseId,
    string Goals,
    string LearningObjectives,
    string MajorResources,
    string PlannedSequence,
    string ParentNotes);

public sealed record RequirementMappingCommand(Guid RequirementAreaId, CoverageLevel CoverageLevel, string Notes);

public sealed record SetCourseRequirementMappingsCommand(Guid CourseId, IReadOnlyList<RequirementMappingCommand> Mappings);

public sealed record CreateLearningModuleCommand(
    Guid CourseId,
    string Title,
    string Description,
    Guid? TermId,
    string EstimatedLength,
    string Instructions,
    IReadOnlyList<ModuleLearningObjectiveCommand> LearningObjectives,
    IReadOnlyList<ModuleResourceCommand> Resources,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status);

public sealed record UpdateLearningModuleCommand(
    Guid CourseId,
    Guid ModuleId,
    string Title,
    string Description,
    Guid? TermId,
    string EstimatedLength,
    string Instructions,
    IReadOnlyList<ModuleLearningObjectiveCommand> LearningObjectives,
    IReadOnlyList<ModuleResourceCommand> Resources,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status);

public sealed record ReorderLearningModulesCommand(Guid CourseId, IReadOnlyList<Guid> ModuleIds);

public sealed record DeleteLearningModuleCommand(Guid CourseId, Guid ModuleId, string ConfirmationText);

public sealed record ModuleLearningObjectiveCommand(string Text, string LinkedCourseObjective);

public sealed record ModuleResourceCommand(string Name, string Link, string FilePath, bool IsPhysicalResource);

public sealed record CreateLessonCommand(
    Guid CourseId,
    Guid ModuleId,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<LessonResourceCommand> Resources);

public sealed record UpdateLessonCommand(
    Guid CourseId,
    Guid ModuleId,
    Guid LessonId,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<LessonResourceCommand> Resources);

public sealed record ReorderLessonsCommand(Guid CourseId, Guid ModuleId, IReadOnlyList<Guid> LessonIds);

public sealed record DeleteLessonCommand(Guid CourseId, Guid ModuleId, Guid LessonId, string ConfirmationText);

public sealed record LessonResourceCommand(
    string Name,
    LessonResourceType Type,
    string Url,
    string FilePath,
    bool IsPhysicalResource,
    string SourceNote);

public sealed record CreateAssignmentCommand(
    Guid CourseId,
    Guid ModuleId,
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

public sealed record UpdateAssignmentCommand(
    Guid CourseId,
    Guid ModuleId,
    Guid AssignmentId,
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

public sealed record ReorderAssignmentsCommand(Guid CourseId, Guid ModuleId, IReadOnlyList<Guid> AssignmentIds);

public sealed record DeleteAssignmentCommand(Guid CourseId, Guid ModuleId, Guid AssignmentId, string ConfirmationText);

public sealed record CoursePackSelectionCommand(string TemplateId, string OptionId);

public sealed record CourseListActionCommand(Guid StudentId, IReadOnlyList<Guid> CourseIds, bool ApplyToEntireCourseList);

public sealed record ImportCoursePackCommand(
    string PackId,
    IReadOnlyList<string> TemplateIds,
    IReadOnlyList<CoursePackSelectionCommand> Selections,
    Guid? StudentId = null)
{
    public ImportCoursePackCommand(string packId, IReadOnlyList<string> templateIds)
        : this(packId, templateIds, [], null)
    {
    }
}
