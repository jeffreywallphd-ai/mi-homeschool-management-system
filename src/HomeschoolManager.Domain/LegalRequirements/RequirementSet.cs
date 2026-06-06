using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.LegalRequirements;

public sealed record RequirementSet
{
    public Guid Id { get; init; }
    public string Jurisdiction { get; init; }
    public string Basis { get; init; }
    public DateOnly EffectiveDate { get; init; }
    public string Notes { get; init; }

    public RequirementSet(Guid id, string jurisdiction, string basis, DateOnly effectiveDate, string notes)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Jurisdiction = Require.Text(jurisdiction, nameof(jurisdiction));
        Basis = Require.Text(basis, nameof(basis));
        EffectiveDate = effectiveDate;
        Notes = notes.Trim();
    }
}
