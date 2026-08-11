using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace StayHub.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> emailSettings)
{
    private readonly EmailOptions _options = emailSettings.Value;

    public async Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient
        {
            ServerCertificateValidationCallback = ValidateServerCertificate
        };

        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    // Tolerates ONLY "the revocation check couldn't be completed" (common on
    // macOS/certain networks when the OCSP/CRL endpoint is unreachable) —
    // every other certificate problem (expired, wrong host, untrusted root,
    // actually revoked, etc.) still fails the handshake as normal. This is
    // narrower than sslPolicyErrors == SslPolicyErrors.None, not a blanket
    // "accept any certificate" bypass.
    private static bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None) return true;

        if (sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors || chain is null) return false;

        return chain.ChainStatus
            .Select(status => status.Status is X509ChainStatusFlags.NoError
                or X509ChainStatusFlags.RevocationStatusUnknown or X509ChainStatusFlags.OfflineRevocation)
            .All(acceptable => acceptable);
    }
}