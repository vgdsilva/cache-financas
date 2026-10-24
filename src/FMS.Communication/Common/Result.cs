namespace FMS.Communication.Common;

public enum ErrorType
{
    None,
    Validation,
    Unauthorized,
    Locked,
    NotFound
}

public class Result<T>
{
    public T?        Value     { get; private init; }
    public ErrorType ErrorType { get; private init; }
    public string?   Error     { get; private init; }
    public bool      IsSuccess => ErrorType == ErrorType.None;

    public static Result<T> Success(T value) => new() { Value = value };
    public static Result<T> Failure(ErrorType type, string error) => new() { ErrorType = type, Error = error };
}
