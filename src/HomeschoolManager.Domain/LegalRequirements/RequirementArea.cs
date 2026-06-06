using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.LegalRequirements;

public sealed record RequirementArea
{
    public Guid Id { get; init; }
    public Guid RequirementSetId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string GradeBand { get; init; }
    public string RequiredOrRecommended { get; init; }
    public string View { get; init; }

    public RequirementArea(
        Guid id,
        Guid requirementSetId,
        string name,
        string description,
        string gradeBand,
        string requiredOrRecommended,
        string view)
    {
        if (requirementSetId == Guid.Empty)
        {
            throw new DomainException("Requirement set is required for a requirement area.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        RequirementSetId = requirementSetId;
        Name = Require.Text(name, nameof(name));
        Description = description.Trim();
        GradeBand = Require.Text(gradeBand, nameof(gradeBand));
        RequiredOrRecommended = Require.Text(requiredOrRecommended, nameof(requiredOrRecommended));
        View = Require.Text(view, nameof(view));
    }
}
