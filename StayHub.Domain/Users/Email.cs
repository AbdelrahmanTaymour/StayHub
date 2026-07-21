namespace StayHub.Domain.Users;

public sealed record Email
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ApplicationException($"The email '{value}' is invalid");

        return new Email(value);
    }

    public static implicit operator string(Email email)
    {
        return email.Value;
    }
}