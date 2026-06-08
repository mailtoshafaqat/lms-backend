namespace Lms.Shared.Common;

/// <summary>Lightweight result for use-case outcomes without throwing for expected failures.</summary>
public class Result
{
    protected Result(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(bool succeeded, T? value, string? error) : base(succeeded, error) => Value = value;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error) => new(false, default, error);
}
