namespace HomeschoolManager.Application.Setup;

public sealed record CreateHouseholdCommand(string HouseholdName, string ParentGuardianName);

public sealed record ConfigureSchoolProfileCommand(
    string SchoolName,
    string AdministratorParentName,
    string Jurisdiction,
    DateOnly HomeschoolStartDate,
    string OperatingBasis,
    string DiplomaSignatureName,
    string DiplomaIssueCity,
    string DiplomaIssueState);

public sealed record CreateStudentCommand(string FirstName, string LastName, int GradeLevel);

public sealed record ConfigureSchoolYearCommand(
    string Name,
    int StartYear,
    int EndYear,
    DateOnly FirstTermStart,
    DateOnly FirstTermEnd,
    DateOnly SecondTermStart,
    DateOnly SecondTermEnd);
