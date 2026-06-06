using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.LegalRequirements;

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

        await MichiganRequirementSeedRefresh.EnsureCurrentAsync(repository, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<RequirementChecklistItem>> GetChecklistAsync(CancellationToken cancellationToken = default)
    {
        await RefreshMichiganRequirementSeedAsync(cancellationToken);
        var areas = await repository.GetRequirementAreasAsync(cancellationToken);
        return areas
            .OrderBy(area => SourceOrder(area.View))
            .ThenBy(area => area.Name)
            .Select(area => new RequirementChecklistItem(area.Id, area.View, area.Name, area.GradeBand, area.RequiredOrRecommended))
            .ToArray();
    }

    public async Task<OperationResult> AddParentRequirementAsync(
        UserContext user,
        AddParentRequirementCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        await RefreshMichiganRequirementSeedAsync(cancellationToken);
        var view = NormalizeView(command.View);
        if (view is null)
        {
            return OperationResult.Failure("Choose a requirement source.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return OperationResult.Failure("Requirement area is required.");
        }

        if (string.IsNullOrWhiteSpace(command.GradeBand))
        {
            return OperationResult.Failure("Grade band is required.");
        }

        var requirementSet = MichiganRequirementSeed.CreateSet();
        var areas = await repository.GetRequirementAreasAsync(cancellationToken);
        if (areas.Any(area =>
            area.RequirementSetId == requirementSet.Id &&
            string.Equals(area.View, view, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(area.Name, command.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult.Failure("A requirement with that source and area already exists.");
        }

        await repository.SaveRequirementAreaAsync(
            new RequirementArea(
                Guid.NewGuid(),
                requirementSet.Id,
                command.Name,
                "Parent-added requirement area.",
                command.GradeBand,
                "Parent",
                view),
            cancellationToken);
        return OperationResult.Success();
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

    private static string? NormalizeView(string view)
    {
        return view.Trim() switch
        {
            "Statutory" => "Statutory",
            "MDE Summary" => "MDE Summary",
            "MMC Reference" => "MMC Reference",
            _ => null
        };
    }

    private async Task RefreshMichiganRequirementSeedAsync(CancellationToken cancellationToken)
    {
        await MichiganRequirementSeedRefresh.EnsureCurrentAsync(repository, cancellationToken);
    }
}
