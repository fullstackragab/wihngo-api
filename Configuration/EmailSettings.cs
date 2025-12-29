namespace Wihngo.Configuration;

public class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Wihngo";
    public string SupportInboxEmail { get; set; } = string.Empty;
    public string SalesInboxEmail { get; set; } = string.Empty;
    public string AdminInboxEmail { get; set; } = string.Empty;
    public string RegisterFromEmail { get; set; } = string.Empty;
    public string WelcomeFromEmail { get; set; } = "welcome@wihngo.com";
    public string DataResidency { get; set; } = "global"; // "eu" or "global"

    /// <summary>Frontend URL for generating deep links in emails</summary>
    public string FrontendUrl { get; set; } = "https://wihngo.com";

    /// <summary>URL to Wihngo logo for email templates (should be hosted on S3/CDN)</summary>
    public string LogoUrl { get; set; } = string.Empty;
}
