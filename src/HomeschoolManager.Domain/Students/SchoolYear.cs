using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Students;

public sealed record SchoolYear
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string Name { get; init; }
    public int StartYear { get; init; }
    public int EndYear { get; init; }
    public IReadOnlyList<Term> Terms { get; init; }

    public SchoolYear(Guid id, Guid studentId, string name, int startYear, int endYear, IReadOnlyList<Term> terms)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a school year.");
        }

        if (endYear < startYear)
        {
            throw new DomainException("End year cannot be before start year.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        Name = Require.Text(name, nameof(name));
        StartYear = Require.Year(startYear, nameof(startYear));
        EndYear = Require.Year(endYear, nameof(endYear));
        Terms = terms.Count == 0 ? throw new DomainException("At least one term is required.") : terms;
    }
}
