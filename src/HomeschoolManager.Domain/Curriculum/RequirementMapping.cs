using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record RequirementMapping
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public Guid RequirementAreaId { get; init; }
    public CoverageLevel CoverageLevel { get; init; }
    public string Notes { get; init; }

    public RequirementMapping(
        Guid id,
        Guid courseId,
        Guid requirementAreaId,
        CoverageLevel coverageLevel,
        string notes)
    {
        if (courseId == Guid.Empty)
        {
            throw new DomainException("Course is required for a requirement mapping.");
        }

        if (requirementAreaId == Guid.Empty)
        {
            throw new DomainException("Requirement area is required for a requirement mapping.");
        }

        if (!Enum.IsDefined(coverageLevel))
        {
            throw new DomainException("Coverage level is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CourseId = courseId;
        RequirementAreaId = requirementAreaId;
        CoverageLevel = coverageLevel;
        Notes = notes.Trim();
    }
}
