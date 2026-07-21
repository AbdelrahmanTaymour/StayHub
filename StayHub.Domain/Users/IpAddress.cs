using System.Net;

namespace StayHub.Domain.Users;

public sealed record IpAddress
{
    private IpAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static IpAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value, out _))
            throw new ArgumentException($"The IP address '{value}' is invalid.", nameof(value));

        return new IpAddress(value);
    }

    public static implicit operator string(IpAddress ip)
    {
        return ip.Value;
    }
}