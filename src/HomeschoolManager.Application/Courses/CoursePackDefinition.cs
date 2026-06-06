using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CoursePackDefinition(
    string Id,
    string Name,
    string Description,
    string RequirementJurisdiction,
    IReadOnlyList<CourseTemplateDefinition> Courses);

public sealed record CourseTemplateDefinition(
    string TemplateId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CourseDescription Description,
    CurriculumPlan CurriculumPlan,
    IReadOnlyList<CourseTemplateRequirementMapping> RequirementMappings,
    IReadOnlyList<CourseTemplateModuleDefinition> Modules,
    string DefaultOptionId,
    IReadOnlyList<CourseTemplateOptionDefinition> Options)
{
    public CourseTemplateOptionDefinition DefaultOption =>
        Options.FirstOrDefault(option => string.Equals(option.OptionId, DefaultOptionId, StringComparison.OrdinalIgnoreCase))
        ?? Options.First();
}

public sealed record CourseTemplateOptionDefinition(
    string OptionId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CourseDescription Description,
    CurriculumPlan CurriculumPlan,
    IReadOnlyList<CourseTemplateRequirementMapping> RequirementMappings,
    IReadOnlyList<CourseTemplateModuleDefinition> Modules);

public sealed record CourseTemplateModuleDefinition(
    string ModuleId,
    int SequenceOrder,
    string Title,
    string Description,
    int? TermNumber,
    string EstimatedLength,
    string Instructions,
    IReadOnlyList<CourseTemplateModuleObjectiveDefinition> LearningObjectives,
    IReadOnlyList<CourseTemplateModuleResourceDefinition> Resources,
    IReadOnlyList<CourseTemplateLessonDefinition> Lessons,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status);

public sealed record CourseTemplateModuleObjectiveDefinition(string Text, string LinkedCourseObjective);

public sealed record CourseTemplateModuleResourceDefinition(string Name, string Link, bool IsPhysicalResource);

public sealed record CourseTemplateLessonDefinition(
    string LessonId,
    int SequenceOrder,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<CourseTemplateLessonResourceDefinition> Resources);

public sealed record CourseTemplateLessonResourceDefinition(
    string Name,
    LessonResourceType Type,
    string Url,
    bool IsPhysicalResource,
    string SourceNote);

public sealed record CourseTemplateRequirementMapping(
    string RequirementAreaView,
    string RequirementAreaName,
    CoverageLevel CoverageLevel,
    string Notes);
