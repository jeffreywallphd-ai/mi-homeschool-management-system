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
    IReadOnlyList<CourseTemplateRequirementMapping> RequirementMappings);

public sealed record CourseTemplateRequirementMapping(
    string RequirementAreaView,
    string RequirementAreaName,
    CoverageLevel CoverageLevel,
    string Notes);
