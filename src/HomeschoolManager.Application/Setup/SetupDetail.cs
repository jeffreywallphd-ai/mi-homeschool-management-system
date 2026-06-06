namespace HomeschoolManager.Application.Setup;

public sealed record SetupDetail(
    string HouseholdName,
    string ParentGuardianName,
    string SchoolName,
    string AdministratorParentName,
    string Jurisdiction,
    DateOnly HomeschoolStartDate,
    string OperatingBasis,
    string DiplomaSignatureName,
    string DiplomaIssueCity,
    string DiplomaIssueState,
    string StudentFirstName,
    string StudentLastName,
    int StudentGradeLevel,
    string SchoolYearName,
    int StartYear,
    int EndYear,
    DateOnly FirstTermStart,
    DateOnly FirstTermEnd,
    DateOnly SecondTermStart,
    DateOnly SecondTermEnd);
