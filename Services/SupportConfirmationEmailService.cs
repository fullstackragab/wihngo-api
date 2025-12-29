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

    /// <summary>
    /// Service for sending support confirmation emails after successful transactions.
    /// IMPORTANT: Only call this service after on-chain transaction success with confirmation hash.
    /// </summary>
    public class SupportConfirmationEmailService : ISupportConfirmationEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<SupportConfirmationEmailService> _logger;
        private readonly SendGridClient? _client;
        private readonly string _frontendUrl;
        private readonly string _wihngoLogoUrl;

        // HARD-CODED: This disclaimer must never be changed or made configurable
        private const string EnglishDisclaimer = "This is a confirmation of voluntary support and is not a tax invoice.";
        private const string ArabicDisclaimer = "هذا تأكيد لدعم طوعي ولا يُعد فاتورة ضريبية.";

        // HARD-CODED: Logo hosted on S3 (public read access)
        private const string DefaultLogoUrl = "https://amzn-s3-wihngo-bucket.s3.us-west-2.amazonaws.com/assets/logo.png";

        public SupportConfirmationEmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<SupportConfirmationEmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _frontendUrl = !string.IsNullOrEmpty(_emailSettings.FrontendUrl)
                ? _emailSettings.FrontendUrl
                : "https://wihngo.com";
            _wihngoLogoUrl = !string.IsNullOrEmpty(_emailSettings.LogoUrl)
                ? _emailSettings.LogoUrl
                : DefaultLogoUrl;

            if (!string.IsNullOrEmpty(_emailSettings.ApiKey))
            {
                var options = new SendGridClientOptions { ApiKey = _emailSettings.ApiKey };

                if (_emailSettings.DataResidency?.ToLower() == "eu")
                {
                    options.SetDataResidency("eu");
                }

                _client = new SendGridClient(options);
            }

            _logger.LogInformation("SupportConfirmationEmailService initialized");
        }

        public async Task<bool> SendSupportConfirmationAsync(SupportConfirmationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TransactionHash))
            {
                _logger.LogWarning("Attempted to send support confirmation without transaction hash - blocked");
                return false;
            }

            var isArabic = dto.Language?.ToLower() == "ar";

            // Subject line must stay exactly as specified
            var subject = isArabic
                ? "تأكيد الدعم — شكرًا لدعمك طائرًا"
                : "Support Confirmation — Thank you for supporting a bird";

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
                    _logger.LogWarning("Email not configured. Would send support confirmation to {Email}", toEmail);
                    return false;
                }

                var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                var to = new EmailAddress(toEmail);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextBody, htmlBody);

                // Disable all tracking to preserve original URLs
                msg.SetClickTracking(false, false);
                msg.SetOpenTracking(false);
                msg.SetGoogleAnalytics(false);
                msg.SetSubscriptionTracking(false);

                var response = await _client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Support confirmation email sent to {Email}", toEmail);
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
                _logger.LogError(ex, "Failed to send support confirmation to {Email}", toEmail);
                return false;
            }
        }

        private string BuildEnglishEmailHtml(SupportConfirmationDto dto)
        {
            var wihngoAmountRow = dto.WihngoAmount.HasValue && dto.WihngoAmount.Value > 0
                ? $@"
                <tr>
                  <td style=""font-size:14px; padding-bottom:8px; color:#6b7a6b;"">
                    Optional support for Wihngo
                  </td>
                  <td align=""right"" style=""font-size:14px; color:#6b7a6b;"">
                    {dto.WihngoAmount.Value:F2} USDC
                  </td>
                </tr>"
                : string.Empty;

            var birdImageSection = !string.IsNullOrWhiteSpace(dto.BirdImageUrl)
                ? $@"<img src=""{dto.BirdImageUrl}"" alt=""{dto.BirdName}"" width=""72"" height=""72"" style=""border-radius:50%; margin-bottom:8px; object-fit:cover;"" />"
                : @"<div style=""width:72px; height:72px; border-radius:50%; background-color:#e6e8e4; margin:0 auto 8px; display:flex; align-items:center; justify-content:center;""></div>";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Support Confirmation</title>
</head>
<body style=""margin:0; padding:0; background-color:#faf9f6; font-family: Arial, Helvetica, sans-serif; color:#2f3a2f;"">

  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#faf9f6; padding:24px;"">
    <tr>
      <td align=""center"">

        <!-- Container -->
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 16px rgba(0,0,0,0.04);"">

          <!-- Header -->
          <tr>
            <td style=""padding:24px; text-align:center;"">
              <img src=""{_wihngoLogoUrl}"" alt=""Wihngo"" width=""48"" height=""48"" style=""display:block; margin:0 auto 12px;"" />
              <h1 style=""margin:0; font-size:20px; font-weight:600;"">
                Support Confirmation
              </h1>
            </td>
          </tr>

          <!-- Bird Card -->
          <tr>
            <td style=""padding:0 24px 24px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f6f7f4; border-radius:12px;"">
                <tr>
                  <td style=""text-align:center; padding:16px;"">
                    {birdImageSection}
                    <h2 style=""margin:0; font-size:18px; font-weight:600;"">
                      {dto.BirdName}
                    </h2>
                    <p style=""margin:4px 0 0; font-size:13px; color:#6b7a6b;"">
                      Bird
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Message -->
          <tr>
            <td style=""padding:0 24px 16px;"">
              <p style=""margin:0; font-size:15px; line-height:1.6;"">
                Thank you for your kind support. Your contribution helps provide care, safety, and well-being for <strong>{dto.BirdName}</strong>.
              </p>
            </td>
          </tr>

          <!-- Support Summary -->
          <tr>
            <td style=""padding:0 24px 24px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-top:1px solid #e6e8e4; padding-top:16px;"">
                <tr>
                  <td style=""font-size:14px; padding-bottom:8px; padding-top:16px;"">
                    Support for the bird
                  </td>
                  <td align=""right"" style=""font-size:14px; padding-top:16px;"">
                    {dto.BirdAmount:F2} USDC
                  </td>
                </tr>
                {wihngoAmountRow}
                <tr>
                  <td style=""padding-top:12px; font-size:15px; font-weight:600;"">
                    Total supported
                  </td>
                  <td align=""right"" style=""padding-top:12px; font-size:15px; font-weight:600;"">
                    {dto.TotalAmount:F2} USDC
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Metadata -->
          <tr>
            <td style=""padding:0 24px 24px;"">
              <p style=""margin:0; font-size:13px; color:#6b7a6b; line-height:1.6;"">
                Date: {dto.TransactionDateTime:MMMM d, yyyy 'at' h:mm tt} UTC<br />
                Network: Solana<br />
                Transaction reference: {dto.TransactionHash}
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding:16px 24px; background-color:#f6f7f4; font-size:12px; color:#6b7a6b; line-height:1.6;"">
              <p style=""margin:0;"">
                {EnglishDisclaimer}
              </p>
              <p style=""margin:12px 0 0;"">
                Thank you for being part of a community that helps birds feel safe, fed, and loved.
              </p>
              <p style=""margin:12px 0 0;"">
                — Wihngo
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

        private string BuildArabicEmailHtml(SupportConfirmationDto dto)
        {
            var wihngoAmountRow = dto.WihngoAmount.HasValue && dto.WihngoAmount.Value > 0
                ? $@"
                <tr>
                  <td style=""font-size:14px; padding-bottom:8px; color:#6b7a6b;"">
                    دعم اختياري لمنصة وينغو
                  </td>
                  <td align=""left"" style=""font-size:14px; color:#6b7a6b;"">
                    {dto.WihngoAmount.Value:F2} USDC
                  </td>
                </tr>"
                : string.Empty;

            var birdImageSection = !string.IsNullOrWhiteSpace(dto.BirdImageUrl)
                ? $@"<img src=""{dto.BirdImageUrl}"" alt=""{dto.BirdName}"" width=""72"" height=""72"" style=""border-radius:50%; margin-bottom:8px; object-fit:cover;"" />"
                : @"<div style=""width:72px; height:72px; border-radius:50%; background-color:#e6e8e4; margin:0 auto 8px;""></div>";

            return $@"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>تأكيد الدعم</title>
</head>
<body style=""margin:0; padding:0; background-color:#faf9f6; font-family: Arial, Helvetica, sans-serif; color:#2f3a2f; direction:rtl;"">

  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#faf9f6; padding:24px;"">
    <tr>
      <td align=""center"">

        <!-- Container -->
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 16px rgba(0,0,0,0.04);"">

          <!-- Header -->
          <tr>
            <td style=""padding:24px; text-align:center;"">
              <img src=""{_wihngoLogoUrl}"" alt=""وينغو"" width=""48"" height=""48"" style=""display:block; margin:0 auto 12px;"" />
              <h1 style=""margin:0; font-size:20px; font-weight:600;"">
                تأكيد الدعم
              </h1>
            </td>
          </tr>

          <!-- Bird Card -->
          <tr>
            <td style=""padding:0 24px 24px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f6f7f4; border-radius:12px;"">
                <tr>
                  <td style=""text-align:center; padding:16px;"">
                    {birdImageSection}
                    <h2 style=""margin:0; font-size:18px; font-weight:600;"">
                      {dto.BirdName}
                    </h2>
                    <p style=""margin:4px 0 0; font-size:13px; color:#6b7a6b;"">
                      طائر
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Message -->
          <tr>
            <td style=""padding:0 24px 16px;"">
              <p style=""margin:0; font-size:15px; line-height:1.8;"">
                شكرًا لك على دعمك الطيب. مساهمتك تساعد في توفير الرعاية والأمان والاهتمام للطائر <strong>{dto.BirdName}</strong>.
              </p>
            </td>
          </tr>

          <!-- Support Summary -->
          <tr>
            <td style=""padding:0 24px 24px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-top:1px solid #e6e8e4; padding-top:16px;"">
                <tr>
                  <td style=""font-size:14px; padding-bottom:8px; padding-top:16px;"">
                    الدعم المقدم للطائر
                  </td>
                  <td align=""left"" style=""font-size:14px; padding-top:16px;"">
                    {dto.BirdAmount:F2} USDC
                  </td>
                </tr>
                {wihngoAmountRow}
                <tr>
                  <td style=""padding-top:12px; font-size:15px; font-weight:600;"">
                    إجمالي الدعم
                  </td>
                  <td align=""left"" style=""padding-top:12px; font-size:15px; font-weight:600;"">
                    {dto.TotalAmount:F2} USDC
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Metadata -->
          <tr>
            <td style=""padding:0 24px 24px;"">
              <p style=""margin:0; font-size:13px; color:#6b7a6b; line-height:1.8;"">
                التاريخ: {dto.TransactionDateTime:yyyy/MM/dd - HH:mm} UTC<br />
                الشبكة: سولانا<br />
                مرجع العملية: {dto.TransactionHash}
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding:16px 24px; background-color:#f6f7f4; font-size:12px; color:#6b7a6b; line-height:1.8;"">
              <p style=""margin:0;"">
                {ArabicDisclaimer}
              </p>
              <p style=""margin:12px 0 0;"">
                شكرًا لكونك جزءًا من مجتمع يساعد الطيور على الشعور بالأمان والحب.
              </p>
              <p style=""margin:12px 0 0;"">
                — وينغو
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

        private string BuildEnglishPlainText(SupportConfirmationDto dto)
        {
            var wihngoLine = dto.WihngoAmount.HasValue && dto.WihngoAmount.Value > 0
                ? $"Optional support for Wihngo: {dto.WihngoAmount.Value:F2} USDC\n"
                : string.Empty;

            return $@"
SUPPORT CONFIRMATION

Thank you for your kind support. Your contribution helps provide care, safety, and well-being for {dto.BirdName}.

Bird: {dto.BirdName}

SUPPORT SUMMARY
---------------
Support for the bird: {dto.BirdAmount:F2} USDC
{wihngoLine}Total supported: {dto.TotalAmount:F2} USDC

DETAILS
-------
Date: {dto.TransactionDateTime:MMMM d, yyyy 'at' h:mm tt} UTC
Network: Solana
Transaction reference: {dto.TransactionHash}

---
{EnglishDisclaimer}

Thank you for being part of a community that helps birds feel safe, fed, and loved.

— Wihngo
";
        }

        private string BuildArabicPlainText(SupportConfirmationDto dto)
        {
            var wihngoLine = dto.WihngoAmount.HasValue && dto.WihngoAmount.Value > 0
                ? $"دعم اختياري لمنصة وينغو: {dto.WihngoAmount.Value:F2} USDC\n"
                : string.Empty;

            return $@"
تأكيد الدعم

شكرًا لك على دعمك الطيب. مساهمتك تساعد في توفير الرعاية والأمان والاهتمام للطائر {dto.BirdName}.

الطائر: {dto.BirdName}

ملخص الدعم
---------------
الدعم المقدم للطائر: {dto.BirdAmount:F2} USDC
{wihngoLine}إجمالي الدعم: {dto.TotalAmount:F2} USDC

التفاصيل
-------
التاريخ: {dto.TransactionDateTime:yyyy/MM/dd - HH:mm} UTC
الشبكة: سولانا
مرجع العملية: {dto.TransactionHash}

---
{ArabicDisclaimer}

شكرًا لكونك جزءًا من مجتمع يساعد الطيور على الشعور بالأمان والحب.

— وينغو
";
        }
    }
}
