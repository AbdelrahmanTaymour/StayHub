using System.Net;
using System.Net.Mail;
using Hangfire;
using Microsoft.Extensions.Options;

namespace StayHub.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<EmailSettings> emailSettings)
{
    private readonly EmailSettings _settings = emailSettings.Value;

    [AutomaticRetry(Attempts = 5)]
    public async Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port);
        client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        client.EnableSsl = _settings.EnableSsl;

        using var message = new MailMessage();
        message.From = new MailAddress(_settings.FromAddress, _settings.FromName);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = false;

        message.To.Add(toAddress);

        await client.SendMailAsync(message, cancellationToken);
    }
}