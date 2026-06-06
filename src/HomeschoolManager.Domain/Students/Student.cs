using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Students;

public sealed record Student
{
    public Guid Id { get; init; }
    public Guid HouseholdId { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public int GradeLevel { get; init; }

    public Student(Guid id, Guid householdId, string firstName, string lastName, int gradeLevel)
    {
        if (householdId == Guid.Empty)
        {
            throw new DomainException("Household is required for a student.");
        }

        if (gradeLevel is < 0 or > 12)
        {
            throw new DomainException("Grade level must be between 0 and 12.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        HouseholdId = householdId;
        FirstName = Require.Text(firstName, nameof(firstName));
        LastName = Require.Text(lastName, nameof(lastName));
        GradeLevel = gradeLevel;
    }
}
