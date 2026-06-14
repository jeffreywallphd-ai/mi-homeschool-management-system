namespace HomeschoolManager.Application.Setup;

public sealed record SetupSummary(
    bool HasHousehold,
    bool HasSchoolProfile,
    bool HasStudent,
    bool HasSchoolYear,
    string HouseholdName,
    string ParentGuardianName,
    string SchoolName,
    string StudentName,
    string SchoolYearName,
    int StudentCount = 0,
    bool IsComplete = false,
    IReadOnlyList<string>? MissingRequiredAreas = null);

public sealed record StudentSetupItem(Guid Id, string FirstName, string LastName, int GradeLevel)
{
    public string DisplayName => $"{FirstName} {LastName}";
}
