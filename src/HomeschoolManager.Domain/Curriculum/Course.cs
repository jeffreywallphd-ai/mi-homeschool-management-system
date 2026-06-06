using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record Course
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid SchoolYearId { get; init; }
    public string Title { get; init; }
    public IReadOnlyList<string> SubjectAreas { get; init; }
    public string SubjectArea => string.Join(", ", SubjectAreas);
    public CourseDuration Duration { get; init; }
    public decimal PlannedCreditValue { get; init; }
    public string? SourcePackId { get; init; }
    public string? SourceTemplateId { get; init; }
    public CourseDescription Description { get; init; }
    public CurriculumPlan CurriculumPlan { get; init; }
    public IReadOnlyList<RequirementMapping> RequirementMappings { get; init; }

    public Course(
        Guid id,
        Guid studentId,
        Guid schoolYearId,
        string title,
        IReadOnlyList<string> subjectAreas,
        CourseDuration duration,
        decimal plannedCreditValue,
        string? sourcePackId,
        string? sourceTemplateId,
        CourseDescription? description,
        CurriculumPlan? curriculumPlan,
        IReadOnlyList<RequirementMapping>? requirementMappings)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a course.");
        }

        if (schoolYearId == Guid.Empty)
        {
            throw new DomainException("School year is required for a course.");
        }

        if (plannedCreditValue <= 0 || plannedCreditValue > 3)
        {
            throw new DomainException("Planned credit value must be greater than 0 and no more than 3.");
        }

        if (!Enum.IsDefined(duration))
        {
            throw new DomainException("Course duration is not recognized.");
        }

        var normalizedSubjects = subjectAreas
            .Select(subject => subject.Trim())
            .Where(subject => subject.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedSubjects.Length == 0)
        {
            throw new DomainException("At least one subject area is required for a course.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        SchoolYearId = schoolYearId;
        Title = Require.Text(title, nameof(title));
        SubjectAreas = normalizedSubjects;
        Duration = duration;
        PlannedCreditValue = plannedCreditValue;
        SourcePackId = string.IsNullOrWhiteSpace(sourcePackId) ? null : sourcePackId.Trim();
        SourceTemplateId = string.IsNullOrWhiteSpace(sourceTemplateId) ? null : sourceTemplateId.Trim();
        Description = description ?? CourseDescription.Empty;
        CurriculumPlan = curriculumPlan ?? CurriculumPlan.Empty;
        RequirementMappings = requirementMappings ?? [];
    }

    public Course WithDescription(CourseDescription description)
    {
        return this with { Description = description };
    }

    public Course WithCurriculumPlan(CurriculumPlan curriculumPlan)
    {
        return this with { CurriculumPlan = curriculumPlan };
    }

    public Course WithMappings(IReadOnlyList<RequirementMapping> mappings)
    {
        var invalidMapping = mappings.FirstOrDefault(mapping => mapping.CourseId != Id);
        if (invalidMapping is not null)
        {
            throw new DomainException("Requirement mappings must belong to the course being updated.");
        }

        var duplicate = mappings
            .GroupBy(mapping => mapping.RequirementAreaId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException("A course can only have one mapping per requirement area.");
        }

        return this with { RequirementMappings = mappings };
    }
}
