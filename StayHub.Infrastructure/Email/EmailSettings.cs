namespace StayHub.Infrastructure.Email;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string Host { get; init; }

    public int Port { get; init; }

    public string Username { get; init; }

    public string Password { get; init; }

    public string FromAddress { get; init; }

    public string FromName { get; init; }

    public bool EnableSsl { get; init; } = true;
}