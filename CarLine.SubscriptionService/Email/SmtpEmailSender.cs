using System.Net;
using System.Net.Mail;
using CarLine.SubscriptionService.Data.Entities;
using CarLine.SubscriptionService.Models;
using Microsoft.Extensions.Options;

namespace CarLine.SubscriptionService.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpSettings> options,
    ILogger<SmtpEmailSender> logger,
    ISubscriptionDigestTemplateBuilder templateBuilder) : IEmailSender
{
    private readonly SmtpSettings _settings = options.Value;

    public async Task SendNewCarsDigestAsync(
        string toEmail,
        SubscriptionEntity subscription,
        IReadOnlyList<MatchedCarDto> cars,
        CancellationToken cancellationToken)
    {
        if (cars.Count == 0) return;

        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            logger.LogWarning("SMTP is not configured (Smtp:Host is empty). Skipping email send for {Email}.", toEmail);
            return;
        }

        var subject = $"CarLine: {cars.Count} new car(s) matched your subscription";
        var lines = templateBuilder.BuildBodyLines(subscription, cars);

        using var msg = new MailMessage(_settings.From, toEmail, subject, string.Join(Environment.NewLine, lines));

        var (port, enableSsl) = ResolveSmtpPortAndSsl(_settings);
        using var client = new SmtpClient(_settings.Host, port);
        client.EnableSsl = enableSsl;
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.UseDefaultCredentials = false;

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        logger.LogInformation("Sending SMTP email via {Host}:{Port} (TLS mode: {TlsMode}, EnableSsl={EnableSsl}) to {Email}",
            _settings.Host, port, _settings.TlsMode, enableSsl, toEmail);

        // SmtpClient has no true async send with CancellationToken; SendMailAsync honors cancellation via token on some platforms.
        await client.SendMailAsync(msg, cancellationToken);

        logger.LogInformation("Sent subscription digest email to {Email} for subscription {SubscriptionId} with {Count} cars.",
            toEmail, subscription.Id, cars.Count);
    }

    private static (int Port, bool EnableSsl) ResolveSmtpPortAndSsl(SmtpSettings settings)
    {
        // Backward compatibility:
        // - Historically we used EnableSsl to mean STARTTLS.
        // - Now we prefer TlsMode.
        return settings.TlsMode switch
        {
            SmtpTlsMode.None => (settings.PortTLS != 0 ? settings.PortTLS : 25, false),
            SmtpTlsMode.SslOnConnect => (settings.PortSSL != 0 ? settings.PortSSL : 465, true),
            SmtpTlsMode.StartTls => (settings.PortTLS != 0 ? settings.PortTLS : 587, true),
            _ => settings.EnableSsl
                ? (settings.PortTLS != 0 ? settings.PortTLS : 587, true)
                : (settings.PortTLS != 0 ? settings.PortTLS : 25, false)
        };
    }
}
