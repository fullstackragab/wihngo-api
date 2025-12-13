namespace Wihngo.Configuration
{
    /// <summary>
    /// SMTP configuration for email sending
    /// </summary>
    public class SmtpConfiguration
    {
        /// <summary>
        /// SMTP server host (e.g., smtp.gmail.com, sandbox.smtp.mailtrap.io, smtp.sendgrid.net)
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// SMTP server port (typically 587 for TLS, 465 for SSL, or 25)
        /// </summary>
        public int Port { get; set; } = 587;

        /// <summary>
        /// Enable SSL/TLS encryption
        /// </summary>
        public bool UseSsl { get; set; } = true;

        /// <summary>
        /// SMTP username for authentication
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// SMTP password for authentication
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Email address to send from
        /// </summary>
        public string FromEmail { get; set; } = "noreply@wihngo.com";

        /// <summary>
        /// Display name for sender
        /// </summary>
        public string FromName { get; set; } = "Wihngo";

        /// <summary>
        /// Email provider type (SMTP, SendGrid)
        /// </summary>
        public string Provider { get; set; } = "SMTP";

        /// <summary>
        /// SendGrid API Key (if using SendGrid)
        /// </summary>
        public string? SendGridApiKey { get; set; }
    }
}
