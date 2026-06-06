using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Household;

public sealed record SchoolProfile
{
    public Guid Id { get; init; }
    public Guid HouseholdId { get; init; }
    public string SchoolName { get; init; }
    public string AdministratorParentName { get; init; }
    public string Jurisdiction { get; init; }
    public DateOnly HomeschoolStartDate { get; init; }
    public string OperatingBasis { get; init; }
    public string DiplomaSignatureName { get; init; }
    public string DiplomaIssueCity { get; init; }
    public string DiplomaIssueState { get; init; }

    public SchoolProfile(
        Guid id,
        Guid householdId,
        string schoolName,
        string administratorParentName,
        string jurisdiction,
        DateOnly homeschoolStartDate,
        string operatingBasis,
        string diplomaSignatureName,
        string diplomaIssueCity,
        string diplomaIssueState)
    {
        if (householdId == Guid.Empty)
        {
            throw new DomainException("Household is required for a school profile.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        HouseholdId = householdId;
        SchoolName = Require.Text(schoolName, nameof(schoolName));
        AdministratorParentName = Require.Text(administratorParentName, nameof(administratorParentName));
        Jurisdiction = Require.Text(jurisdiction, nameof(jurisdiction));
        HomeschoolStartDate = homeschoolStartDate;
        OperatingBasis = Require.Text(operatingBasis, nameof(operatingBasis));
        DiplomaSignatureName = Require.Text(diplomaSignatureName, nameof(diplomaSignatureName));
        DiplomaIssueCity = Require.Text(diplomaIssueCity, nameof(diplomaIssueCity));
        DiplomaIssueState = Require.Text(diplomaIssueState, nameof(diplomaIssueState));
    }
}
