namespace HomeschoolManager.Domain.Common;

public static class Require
{
    public static string Text(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    public static DateOnly OnOrBefore(DateOnly value, DateOnly maxValue, string fieldName)
    {
        if (value > maxValue)
        {
            throw new DomainException($"{fieldName} cannot be in the future.");
        }

        return value;
    }

    public static int Year(int value, string fieldName)
    {
        if (value < 1900 || value > 2200)
        {
            throw new DomainException($"{fieldName} must be a valid year.");
        }

        return value;
    }
}
