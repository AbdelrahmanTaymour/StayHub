using System.Text.RegularExpressions;

namespace StayHub.Domain.Users;

public sealed partial record PhoneNumber
{
    private static readonly Regex PhoneRegex = MyRegex();

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    [GeneratedRegex(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    public static PhoneNumber Create(string value)
    {
        var cleanedValue = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cleanedValue) || !PhoneRegex.IsMatch(cleanedValue))
            throw new ArgumentException($"The phone number '{value}' is invalid.", nameof(value));

        return new PhoneNumber(cleanedValue);
    }

    public static implicit operator string(PhoneNumber phone)
    {
        return phone.Value;
    }
}