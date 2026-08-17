using System.Text.RegularExpressions;
using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public sealed partial record PhoneNumber
{
    internal static readonly Error Invalid = Error.Validation(
        "PhoneNumber.Invalid",
        "The provided phone number is invalid");

    private static readonly Regex PhoneRegex = MyRegex();

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    [GeneratedRegex(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    public static Result<PhoneNumber> Create(string value)
    {
        var cleanedValue = value.Trim();

        if (string.IsNullOrWhiteSpace(cleanedValue) || !PhoneRegex.IsMatch(cleanedValue))
        {
            return Result.Failure<PhoneNumber>(Invalid);
        }

        return Result.Success(new PhoneNumber(cleanedValue));
    }

    public static implicit operator string(PhoneNumber phone)
    {
        return phone.Value;
    }
}