namespace InkFlow.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, string? error, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors?.ToList().AsReadOnly() ?? (error != null ? new List<string> { error }.AsReadOnly() : new List<string>().AsReadOnly());
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.FirstOrDefault(), errors);
}
