using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;

namespace HomeschoolManager.Application.Requirements;

public sealed class RequirementService
{
    private readonly IHomeschoolRepository repository;

    public RequirementService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OperationResult> SeedMichiganAsync(UserContext user, CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        await repository.SaveRequirementSeedAsync(
            MichiganRequirementSeed.CreateSet(),
            MichiganRequirementSeed.CreateAreas(),
            cancellationToken);

        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<RequirementChecklistItem>> GetChecklistAsync(CancellationToken cancellationToken = default)
    {
        var areas = await repository.GetRequirementAreasAsync(cancellationToken);
        return areas
            .OrderBy(area => SourceOrder(area.View))
            .ThenBy(area => area.Name)
            .Select(area => new RequirementChecklistItem(area.Id, area.View, area.Name, area.GradeBand, area.RequiredOrRecommended))
            .ToArray();
    }

    private static int SourceOrder(string source)
    {
        return source switch
        {
            "Statutory" => 0,
            "MDE Summary" => 1,
            "MMC Reference" => 2,
            _ => 99
        };
    }
}
