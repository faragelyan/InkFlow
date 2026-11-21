namespace InkFlow.Application.Common.Results;

public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, string? error, IEnumerable<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static Result<T> Success(T value) => new(value, true, null);
    public static new Result<T> Failure(string error) => new(default, false, error);
    public static new Result<T> Failure(IEnumerable<string> errors) => new(default, false, errors.FirstOrDefault(), errors);
}
