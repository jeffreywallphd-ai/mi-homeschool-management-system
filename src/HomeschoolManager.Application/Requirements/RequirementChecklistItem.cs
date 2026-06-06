namespace HomeschoolManager.Application.Requirements;

public sealed record RequirementChecklistItem(
    Guid RequirementAreaId,
    string View,
    string Name,
    string GradeBand,
    string RequiredOrRecommended);
