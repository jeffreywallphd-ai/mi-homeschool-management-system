using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class AppDataDocument
{
    public int SchemaVersion { get; set; } = 1;

    public Household? Household { get; set; }

    public SchoolProfile? SchoolProfile { get; set; }

    public Student? Student { get; set; }

    public SchoolYear? SchoolYear { get; set; }

    public List<RequirementSet> RequirementSets { get; set; } = [];

    public List<RequirementArea> RequirementAreas { get; set; } = [];

    public List<Course> Courses { get; set; } = [];
}
