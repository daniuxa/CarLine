namespace CarLine.SubscriptionService.Email;

public enum SmtpTlsMode
{
    /// <summary>No TLS. Only for local dev servers.</summary>
    None = 0,

    /// <summary>STARTTLS (explicit TLS), typically on port 587.</summary>
    StartTls = 1,

    /// <summary>Implicit TLS (SSL-on-connect), typically on port 465.</summary>
    SslOnConnect = 2
}

public sealed class SmtpSettings
{
    public string From { get; set; } = "no-reply@carline.local";

    public string? Password { get; set; }
    public string? Username { get; set; }

    /// <summary>
    /// Preferred configuration: set TlsMode to StartTls (587) or SslOnConnect (465).
    /// EnableSsl is kept for backward compatibility.
    /// </summary>
    public SmtpTlsMode TlsMode { get; set; } = SmtpTlsMode.StartTls;

    // Backward-compat flag (older config). If set and TlsMode isn't specified, we'll treat true as StartTls.
    public bool EnableSsl { get; set; } = false;

    public int PortTLS { get; set; } = 587;
    public int PortSSL { get; set; } = 465;
    public string Host { get; set; } = string.Empty;
}
