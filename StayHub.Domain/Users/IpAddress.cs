using System.Net;
using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public sealed record IpAddress
{
    internal static readonly Error Invalid = Error.Validation(
        "IpAddress.Invalid",
        "The provided IP address is invalid");

    private IpAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<IpAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsValid(value))
        {
            return Result.Failure<IpAddress>(Invalid);
        }

        return Result.Success(new IpAddress(value));
    }

    private static bool IsValid(string value)
    {
        return IPAddress.TryParse(value, out var parsed) && parsed.ToString() == value;
    }

    public static implicit operator string(IpAddress ip)
    {
        return ip.Value;
    }
}