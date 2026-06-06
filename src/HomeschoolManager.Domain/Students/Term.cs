using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Students;

public sealed record Term
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    public Term(Guid id, string name, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new DomainException("Term end date cannot be before start date.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = Require.Text(name, nameof(name));
        StartDate = startDate;
        EndDate = endDate;
    }
}
