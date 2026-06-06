namespace HomeschoolManager.Application.Common;

public sealed record OperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static OperationResult Success() => new(true, []);

    public static OperationResult Failure(params string[] errors) => new(false, errors);
}

public sealed record OperationResult<T>(bool Succeeded, T? Value, IReadOnlyList<string> Errors)
{
    public static OperationResult<T> Success(T value) => new(true, value, []);

    public static OperationResult<T> Failure(params string[] errors) => new(false, default, errors);
}
