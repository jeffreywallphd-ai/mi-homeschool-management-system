using HomeschoolManager.Domain.LegalRequirements;

namespace HomeschoolManager.Application.Requirements;

public static class MichiganRequirementSeed
{
    private static readonly Guid RequirementSetId = Guid.Parse("2cdb9208-b6d6-45e9-bd54-703a532cbdd7");

    public static RequirementSet CreateSet()
    {
        return new RequirementSet(
            RequirementSetId,
            "Michigan",
            "MCL 380.1561(3)(f)",
            new DateOnly(2026, 6, 6),
            "Michigan is seeded as the first jurisdiction profile. Records show selected subject-area coverage; they do not certify legal compliance.");
    }

    public static IReadOnlyList<RequirementArea> CreateAreas()
    {
        var statutory = new[]
        {
            "Reading",
            "Spelling",
            "Mathematics",
            "Science",
            "History",
            "Civics",
            "Literature",
            "Writing",
            "English Grammar"
        };

        var mdeGuidance = new[]
        {
            "U.S. Constitution",
            "Michigan Constitution"
        };

        var mmcReference = new[]
        {
            "Online Learning Experience",
            "Personal Finance",
            "Physical Education and Health",
            "Visual, Performing, and Applied Arts",
            "World Language"
        };

        var areas = new List<RequirementArea>();
        areas.AddRange(statutory.Select(name => Area(name, "All grades", "Required", "Statutory")));
        areas.AddRange(mdeGuidance.Select(name => Area(name, IsUpperGradeMdeArea(name) ? "Grades 10-12" : "All grades", "Guidance", "MDE Summary")));
        areas.AddRange(mmcReference.Select(name => Area(name, "High school", "Reference", "MMC Reference")));
        return areas;
    }

    private static bool IsUpperGradeMdeArea(string name)
    {
        return name.Contains("Constitution", StringComparison.Ordinal) ||
            name.Contains("Civics", StringComparison.Ordinal);
    }

    private static RequirementArea Area(string name, string gradeBand, string requiredOrRecommended, string view)
    {
        return new RequirementArea(
            DeterministicGuid($"{view}:{name}"),
            RequirementSetId,
            name,
            "",
            gradeBand,
            requiredOrRecommended,
            view);
    }

    private static Guid DeterministicGuid(string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }
}
