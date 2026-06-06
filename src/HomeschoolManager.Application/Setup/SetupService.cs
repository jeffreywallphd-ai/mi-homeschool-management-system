using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Students;

namespace HomeschoolManager.Application.Setup;

public sealed class SetupService
{
    private readonly IHomeschoolRepository repository;
    public event Action? StudentsChanged;

    public SetupService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<SetupSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var household = await repository.GetHouseholdAsync(cancellationToken);
        var schoolProfile = await repository.GetSchoolProfileAsync(cancellationToken);
        var student = await repository.GetStudentAsync(cancellationToken);
        var students = await repository.GetStudentsAsync(cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);

        return new SetupSummary(
            household is not null,
            schoolProfile is not null,
            students.Count > 0 || student is not null,
            schoolYear is not null,
            household?.Name ?? "",
            household?.ParentGuardianName ?? "",
            schoolProfile?.SchoolName ?? "",
            student is null ? "" : $"{student.FirstName} {student.LastName}",
            schoolYear?.Name ?? "",
            students.Count);
    }

    public async Task<SetupDetail> GetDetailAsync(CancellationToken cancellationToken = default)
    {
        var household = await repository.GetHouseholdAsync(cancellationToken);
        var schoolProfile = await repository.GetSchoolProfileAsync(cancellationToken);
        var student = await repository.GetStudentAsync(cancellationToken);
        var students = await repository.GetStudentsAsync(cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var terms = schoolYear?.Terms.OrderBy(term => term.StartDate).ToArray() ?? [];
        var firstTerm = terms.ElementAtOrDefault(0);
        var secondTerm = terms.ElementAtOrDefault(1);

        return new SetupDetail(
            household?.Name ?? "Family Household",
            household?.ParentGuardianName ?? "",
            schoolProfile?.SchoolName ?? "Family Homeschool",
            schoolProfile?.AdministratorParentName ?? "",
            schoolProfile?.Jurisdiction ?? "Michigan",
            schoolProfile?.HomeschoolStartDate ?? new DateOnly(2026, 8, 24),
            schoolProfile?.OperatingBasis ?? "Michigan Exemption 3(f)",
            schoolProfile?.DiplomaSignatureName ?? "",
            schoolProfile?.DiplomaIssueCity ?? "",
            schoolProfile?.DiplomaIssueState ?? "Michigan",
            student?.FirstName ?? "",
            student?.LastName ?? "",
            student?.GradeLevel ?? 12,
            schoolYear?.Name ?? "2026-2027",
            schoolYear?.StartYear ?? 2026,
            schoolYear?.EndYear ?? 2027,
            firstTerm?.StartDate ?? new DateOnly(2026, 8, 24),
            firstTerm?.EndDate ?? new DateOnly(2026, 12, 18),
            secondTerm?.StartDate ?? new DateOnly(2027, 1, 11),
            secondTerm?.EndDate ?? new DateOnly(2027, 5, 28),
            students.Select(ToSetupItem).ToArray());
    }

    public async Task<IReadOnlyList<StudentSetupItem>> ListStudentsAsync(CancellationToken cancellationToken = default)
    {
        var students = await repository.GetStudentsAsync(cancellationToken);
        return students.Select(ToSetupItem).ToArray();
    }

    public async Task<OperationResult> CreateHouseholdAsync(
        UserContext user,
        CreateHouseholdCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        try
        {
            var household = new Household(Guid.NewGuid(), command.HouseholdName, command.ParentGuardianName);
            await repository.SaveHouseholdAsync(household, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ConfigureSchoolProfileAsync(
        UserContext user,
        ConfigureSchoolProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var household = await repository.GetHouseholdAsync(cancellationToken);
        if (household is null)
        {
            return OperationResult.Failure("Create a household before configuring the school profile.");
        }

        try
        {
            var profile = new SchoolProfile(
                Guid.NewGuid(),
                household.Id,
                command.SchoolName,
                command.AdministratorParentName,
                command.Jurisdiction,
                command.HomeschoolStartDate,
                command.OperatingBasis,
                command.DiplomaSignatureName,
                command.DiplomaIssueCity,
                command.DiplomaIssueState);

            await repository.SaveSchoolProfileAsync(profile, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> CreateStudentAsync(
        UserContext user,
        CreateStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var household = await repository.GetHouseholdAsync(cancellationToken);
        if (household is null)
        {
            return OperationResult.Failure("Create a household before adding a student.");
        }

        try
        {
            var student = new Student(Guid.NewGuid(), household.Id, command.FirstName, command.LastName, command.GradeLevel);
            await repository.SaveStudentAsync(student, cancellationToken);
            StudentsChanged?.Invoke();
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ConfigureSchoolYearAsync(
        UserContext user,
        ConfigureSchoolYearCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var student = await repository.GetStudentAsync(cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Create a student before configuring a school year.");
        }

        try
        {
            var terms = new[]
            {
                new Term(Guid.NewGuid(), "Semester 1", command.FirstTermStart, command.FirstTermEnd),
                new Term(Guid.NewGuid(), "Semester 2", command.SecondTermStart, command.SecondTermEnd)
            };

            var schoolYear = new SchoolYear(
                Guid.NewGuid(),
                student.Id,
                command.Name,
                command.StartYear,
                command.EndYear,
                terms);

            await repository.SaveSchoolYearAsync(schoolYear, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    private static StudentSetupItem ToSetupItem(Student student)
    {
        return new StudentSetupItem(student.Id, student.FirstName, student.LastName, student.GradeLevel);
    }
}
