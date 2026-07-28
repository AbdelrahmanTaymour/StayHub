namespace StayHub.Domain.Abstractions;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4
}

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static readonly Error NullValue = new(
        "Error.NullValue",
        "The specified result value is null");

    public static Error NotFound(string code, string message)
    {
        return new Error(code, message, ErrorType.NotFound);
    }

    public static Error Conflict(string code, string message)
    {
        return new Error(code, message, ErrorType.Conflict);
    }

    public static Error Unauthorized(string code, string message)
    {
        return new Error(code, message, ErrorType.Unauthorized);
    }

    public static Error Validation(string code, string message)
    {
        return new Error(code, message, ErrorType.Validation);
    }

    public static Error Failure(string code, string message)
    {
        return new Error(code, message);
    }
}