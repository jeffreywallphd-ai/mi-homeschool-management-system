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
    string SchoolYearName);
