using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.LegalRequirements;

namespace HomeschoolManager.Application.Requirements;

public static class MichiganRequirementSeedRefresh
{
    public static async Task EnsureCurrentAsync(
        IHomeschoolRepository repository,
        CancellationToken cancellationToken = default)
    {
        var currentSet = MichiganRequirementSeed.CreateSet();
        var currentAreas = MichiganRequirementSeed.CreateAreas();
        var savedSets = await repository.GetRequirementSetsAsync(cancellationToken);
        var savedAreas = await repository.GetRequirementAreasAsync(cancellationToken);

        if (SeedMatches(savedSets, savedAreas, currentSet, currentAreas))
        {
            return;
        }

        await repository.SaveRequirementSeedAsync(currentSet, currentAreas, cancellationToken);
    }

    private static bool SeedMatches(
        IReadOnlyList<RequirementSet> savedSets,
        IReadOnlyList<RequirementArea> savedAreas,
        RequirementSet currentSet,
        IReadOnlyList<RequirementArea> currentAreas)
    {
        var savedSet = savedSets.FirstOrDefault(set => set.Id == currentSet.Id);
        if (savedSet is null || savedSet != currentSet)
        {
            return false;
        }

        var relevantSavedAreas = savedAreas
            .Where(area =>
                area.RequirementSetId == currentSet.Id &&
                !string.Equals(area.RequiredOrRecommended, "Parent", StringComparison.OrdinalIgnoreCase))
            .OrderBy(area => area.Id)
            .ToArray();
        var orderedCurrentAreas = currentAreas
            .OrderBy(area => area.Id)
            .ToArray();

        return relevantSavedAreas.SequenceEqual(orderedCurrentAreas);
    }
}
