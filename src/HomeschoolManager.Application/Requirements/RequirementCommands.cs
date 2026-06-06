namespace HomeschoolManager.Application.Requirements;

public sealed record AddParentRequirementCommand(
    string View,
    string Name,
    string GradeBand);
