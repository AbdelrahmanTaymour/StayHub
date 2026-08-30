using System.Collections.Concurrent;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Integration;

public sealed record SentEmail(Email To, string Subject, string Body);

public sealed class TestEmailService : IEmailService
{
    private readonly ConcurrentBag<SentEmail> _sentEmails = new();

    public IReadOnlyCollection<SentEmail> SentEmails => _sentEmails.ToArray();

    public Task SendAsync(Email email, string subject, string body)
    {
        _sentEmails.Add(new SentEmail(email, subject, body));

        return Task.CompletedTask;
    }
}