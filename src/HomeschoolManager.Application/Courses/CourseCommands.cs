using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CreateCourseCommand(
    string Title,
    string Description,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue);

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

public sealed record CoursePackSelectionCommand(string TemplateId, string OptionId);

public sealed record ImportCoursePackCommand(
    string PackId,
    IReadOnlyList<string> TemplateIds,
    IReadOnlyList<CoursePackSelectionCommand> Selections)
{
    public ImportCoursePackCommand(string packId, IReadOnlyList<string> templateIds)
        : this(packId, templateIds, [])
    {
    }
}
