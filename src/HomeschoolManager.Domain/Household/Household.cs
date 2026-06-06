using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Household;

public sealed record Household
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string ParentGuardianName { get; init; }

    public Household(Guid id, string name, string parentGuardianName)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = Require.Text(name, nameof(name));
        ParentGuardianName = Require.Text(parentGuardianName, nameof(parentGuardianName));
    }
}
