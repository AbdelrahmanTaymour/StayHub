using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public sealed record Email
{
    internal static readonly Error Invalid = Error.Validation(
        "Email.Invalid",
        "The provided email is invalid");

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsValid(value))
        {
            return Result.Failure<Email>(Invalid);
        }

        return Result.Success(new Email(value));
    }

    private static bool IsValid(string value)
    {
        var parts = value.Split('@');

        return parts.Length == 2
               && !string.IsNullOrWhiteSpace(parts[0])
               && !string.IsNullOrWhiteSpace(parts[1]);
    }

    public static implicit operator string(Email email)
    {
        return email.Value;
    }
}