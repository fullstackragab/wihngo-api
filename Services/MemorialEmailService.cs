namespace Wihngo.Services
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using SendGrid;
    using SendGrid.Helpers.Mail;
    using Wihngo.Configuration;
    using Wihngo.Services.Interfaces;

    public class MemorialNotificationDto
    {
        public required string SupporterEmail { get; set; }
        public required string SupporterName { get; set; }
        public required string BirdName { get; set; }
        public string? BirdImageUrl { get; set; }
        public DateTime? MemorialDate { get; set; }
        public string? MemorialReason { get; set; }
        public Guid BirdId { get; set; }
        public decimal TotalSupportGiven { get; set; }
        public string? Language { get; set; }
    }

    public interface IMemorialEmailService
    {
        Task<bool> SendMemorialNotificationAsync(MemorialNotificationDto dto);
    }

    /// <summary>
    /// Service for sending gentle, compassionate memorial notifications to bird supporters.
    /// Tone: Calm, thankful, respectful, never defensive.
    /// </summary>
    public class MemorialEmailService : IMemorialEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<MemorialEmailService> _logger;
        private readonly SendGridClient? _client;
        private readonly string _frontendUrl;
        private readonly string _wihngoLogoUrl;

        private const string DefaultLogoUrl = "https://amzn-s3-wihngo-bucket.s3.us-west-2.amazonaws.com/assets/logo.png";

        public MemorialEmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<MemorialEmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _frontendUrl = !string.IsNullOrEmpty(_emailSettings.FrontendUrl)
                ? _emailSettings.FrontendUrl
                : "https://wihngo.com";
            _wihngoLogoUrl = DefaultLogoUrl;

            if (!string.IsNullOrEmpty(_emailSettings.ApiKey))
            {
                var options = new SendGridClientOptions { ApiKey = _emailSettings.ApiKey };
                if (_emailSettings.DataResidency?.ToLower() == "eu")
                {
                    options.SetDataResidency("eu");
                }
                _client = new SendGridClient(options);
            }

            _logger.LogInformation("MemorialEmailService initialized");
        }

        public async Task<bool> SendMemorialNotificationAsync(MemorialNotificationDto dto)
        {
            var isArabic = dto.Language?.ToLower() == "ar";

            var subject = isArabic
                ? $"تحديث عن {dto.BirdName}"
                : $"An update about {dto.BirdName}";

            var htmlBody = isArabic
                ? BuildArabicEmailHtml(dto)
                : BuildEnglishEmailHtml(dto);

            var plainTextBody = isArabic
                ? BuildArabicPlainText(dto)
                : BuildEnglishPlainText(dto);

            return await SendEmailAsync(dto.SupporterEmail, subject, htmlBody, plainTextBody);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
        {
            try
            {
                if (_client == null || string.IsNullOrEmpty(_emailSettings.ApiKey))
                {
                    _logger.LogWarning("Email not configured. Would send memorial notification to {Email}", toEmail);
                    return false;
                }

                var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                var to = new EmailAddress(toEmail);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextBody, htmlBody);

                // Disable all tracking
                msg.SetClickTracking(false, false);
                msg.SetOpenTracking(false);
                msg.SetGoogleAnalytics(false);
                msg.SetSubscriptionTracking(false);

                var response = await _client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Memorial notification email sent to {Email}", toEmail);
                    return true;
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("SendGrid failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send memorial notification to {Email}", toEmail);
                return false;
            }
        }

        private string BuildEnglishEmailHtml(MemorialNotificationDto dto)
        {
            var memorialDate = dto.MemorialDate?.ToString("MMMM d, yyyy") ?? "recently";
            var memorialPageUrl = $"{_frontendUrl}/birds/{dto.BirdId}/memorial";

            var birdImageSection = !string.IsNullOrWhiteSpace(dto.BirdImageUrl)
                ? $@"<img src=""{dto.BirdImageUrl}"" alt=""{dto.BirdName}"" width=""120"" height=""120"" style=""border-radius:50%; margin-bottom:16px; object-fit:cover; border: 4px solid #e6e8e4;"" />"
                : @"<div style=""width:120px; height:120px; border-radius:50%; background-color:#e6e8e4; margin:0 auto 16px; border: 4px solid #d4d7d0;""></div>";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>An update about {dto.BirdName}</title>
</head>
<body style=""margin:0; padding:0; background-color:#faf9f6; font-family: Georgia, 'Times New Roman', serif; color:#2f3a2f;"">

  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#faf9f6; padding:32px 16px;"">
    <tr>
      <td align=""center"">

        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,0.06);"">

          <!-- Header -->
          <tr>
            <td style=""padding:32px 32px 24px; text-align:center; background: linear-gradient(180deg, #f8f9f6 0%, #ffffff 100%);"">
              <img src=""{_wihngoLogoUrl}"" alt=""Wihngo"" width=""40"" height=""40"" style=""display:block; margin:0 auto 16px; opacity:0.8;"" />
              {birdImageSection}
              <h1 style=""margin:0; font-size:24px; font-weight:normal; color:#2f3a2f;"">
                In Loving Memory
              </h1>
              <p style=""margin:8px 0 0; font-size:20px; color:#4a5a4a; font-weight:500;"">
                {dto.BirdName}
              </p>
            </td>
          </tr>

          <!-- Message -->
          <tr>
            <td style=""padding:24px 32px;"">
              <p style=""margin:0 0 20px; font-size:16px; line-height:1.7; color:#3a4a3a;"">
                Dear {dto.SupporterName},
              </p>
              <p style=""margin:0 0 20px; font-size:16px; line-height:1.7; color:#3a4a3a;"">
                We're writing with a gentle update about <strong>{dto.BirdName}</strong>, a bird you kindly supported.
              </p>
              <p style=""margin:0 0 20px; font-size:16px; line-height:1.7; color:#3a4a3a;"">
                Despite love, care, and support from this community, {dto.BirdName}'s journey has come to an end.
              </p>
              <p style=""margin:0 0 24px; font-size:16px; line-height:1.7; color:#3a4a3a; padding:16px; background-color:#f6f7f4; border-radius:8px; border-left:3px solid #8a9a8a;"">
                Your support made comfort, safety, and dignity possible. That kindness mattered.
              </p>
            </td>
          </tr>

          <!-- Memorial Page CTA -->
          <tr>
            <td style=""padding:0 32px 32px; text-align:center;"">
              <a href=""{memorialPageUrl}"" style=""display:inline-block; padding:14px 28px; background-color:#4a5a4a; color:#ffffff; text-decoration:none; border-radius:8px; font-size:15px; font-family:Arial,sans-serif;"">
                Visit Memorial Page
              </a>
              <p style=""margin:16px 0 0; font-size:14px; color:#6b7a6b;"">
                Share a memory or tribute
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding:24px 32px; background-color:#f6f7f4; text-align:center;"">
              <p style=""margin:0 0 12px; font-size:14px; color:#6b7a6b; line-height:1.6;"">
                Wihngo exists to support care, compassion, and dignity for birds.<br />
                Life cannot be guaranteed — care always is.
              </p>
              <p style=""margin:0; font-size:14px; color:#8a9a8a;"">
                With gratitude,<br />
                The Wihngo Community
              </p>
            </td>
          </tr>

        </table>

      </td>
    </tr>
  </table>

</body>
</html>";
        }

        private string BuildArabicEmailHtml(MemorialNotificationDto dto)
        {
            var memorialDate = dto.MemorialDate?.ToString("yyyy/MM/dd") ?? "مؤخراً";
            var memorialPageUrl = $"{_frontendUrl}/birds/{dto.BirdId}/memorial";

            var birdImageSection = !string.IsNullOrWhiteSpace(dto.BirdImageUrl)
                ? $@"<img src=""{dto.BirdImageUrl}"" alt=""{dto.BirdName}"" width=""120"" height=""120"" style=""border-radius:50%; margin-bottom:16px; object-fit:cover; border: 4px solid #e6e8e4;"" />"
                : @"<div style=""width:120px; height:120px; border-radius:50%; background-color:#e6e8e4; margin:0 auto 16px; border: 4px solid #d4d7d0;""></div>";

            return $@"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>تحديث عن {dto.BirdName}</title>
</head>
<body style=""margin:0; padding:0; background-color:#faf9f6; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; color:#2f3a2f; direction:rtl;"">

  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#faf9f6; padding:32px 16px;"">
    <tr>
      <td align=""center"">

        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,0.06);"">

          <!-- Header -->
          <tr>
            <td style=""padding:32px 32px 24px; text-align:center; background: linear-gradient(180deg, #f8f9f6 0%, #ffffff 100%);"">
              <img src=""{_wihngoLogoUrl}"" alt=""وينغو"" width=""40"" height=""40"" style=""display:block; margin:0 auto 16px; opacity:0.8;"" />
              {birdImageSection}
              <h1 style=""margin:0; font-size:24px; font-weight:normal; color:#2f3a2f;"">
                في ذكرى محبة
              </h1>
              <p style=""margin:8px 0 0; font-size:20px; color:#4a5a4a; font-weight:500;"">
                {dto.BirdName}
              </p>
            </td>
          </tr>

          <!-- Message -->
          <tr>
            <td style=""padding:24px 32px;"">
              <p style=""margin:0 0 20px; font-size:16px; line-height:1.9; color:#3a4a3a;"">
                عزيزي/عزيزتي {dto.SupporterName}،
              </p>
              <p style=""margin:0 0 20px; font-size:16px; line-height:1.9; color:#3a4a3a;"">
                نكتب إليك بتحديث لطيف عن <strong>{dto.BirdName}</strong>، الطائر الذي دعمته بلطف.
              </p>
              <p style=""margin:0 0 20px; font-size:16px; line-height:1.9; color:#3a4a3a;"">
                رغم الحب والرعاية والدعم من هذا المجتمع، انتهت رحلة {dto.BirdName}.
              </p>
              <p style=""margin:0 0 24px; font-size:16px; line-height:1.9; color:#3a4a3a; padding:16px; background-color:#f6f7f4; border-radius:8px; border-right:3px solid #8a9a8a;"">
                دعمك وفّر الراحة والأمان والكرامة. هذا اللطف كان مهماً.
              </p>
            </td>
          </tr>

          <!-- Memorial Page CTA -->
          <tr>
            <td style=""padding:0 32px 32px; text-align:center;"">
              <a href=""{memorialPageUrl}"" style=""display:inline-block; padding:14px 28px; background-color:#4a5a4a; color:#ffffff; text-decoration:none; border-radius:8px; font-size:15px;"">
                زيارة صفحة الذكرى
              </a>
              <p style=""margin:16px 0 0; font-size:14px; color:#6b7a6b;"">
                شارك ذكرى أو تحية
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding:24px 32px; background-color:#f6f7f4; text-align:center;"">
              <p style=""margin:0 0 12px; font-size:14px; color:#6b7a6b; line-height:1.8;"">
                وينغو موجود لدعم الرعاية والرحمة والكرامة للطيور.<br />
                الحياة لا يمكن ضمانها — الرعاية دائماً ممكنة.
              </p>
              <p style=""margin:0; font-size:14px; color:#8a9a8a;"">
                مع الامتنان،<br />
                مجتمع وينغو
              </p>
            </td>
          </tr>

        </table>

      </td>
    </tr>
  </table>

</body>
</html>";
        }

        private string BuildEnglishPlainText(MemorialNotificationDto dto)
        {
            var memorialPageUrl = $"{_frontendUrl}/birds/{dto.BirdId}/memorial";

            return $@"
IN LOVING MEMORY
{dto.BirdName}

Dear {dto.SupporterName},

We're writing with a gentle update about {dto.BirdName}, a bird you kindly supported.

Despite love, care, and support from this community, {dto.BirdName}'s journey has come to an end.

Your support made comfort, safety, and dignity possible. That kindness mattered.

Visit the memorial page to share a memory or tribute:
{memorialPageUrl}

---

Wihngo exists to support care, compassion, and dignity for birds.
Life cannot be guaranteed — care always is.

With gratitude,
The Wihngo Community
";
        }

        private string BuildArabicPlainText(MemorialNotificationDto dto)
        {
            var memorialPageUrl = $"{_frontendUrl}/birds/{dto.BirdId}/memorial";

            return $@"
في ذكرى محبة
{dto.BirdName}

عزيزي/عزيزتي {dto.SupporterName}،

نكتب إليك بتحديث لطيف عن {dto.BirdName}، الطائر الذي دعمته بلطف.

رغم الحب والرعاية والدعم من هذا المجتمع، انتهت رحلة {dto.BirdName}.

دعمك وفّر الراحة والأمان والكرامة. هذا اللطف كان مهماً.

زيارة صفحة الذكرى لمشاركة ذكرى أو تحية:
{memorialPageUrl}

---

وينغو موجود لدعم الرعاية والرحمة والكرامة للطيور.
الحياة لا يمكن ضمانها — الرعاية دائماً ممكنة.

مع الامتنان،
مجتمع وينغو
";
        }
    }
}
