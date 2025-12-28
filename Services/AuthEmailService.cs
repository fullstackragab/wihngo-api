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
    /// Service for sending authentication-related emails via SendGrid
    /// </summary>
    public class AuthEmailService : IAuthEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<AuthEmailService> _logger;
        private readonly SendGridClient? _client;
        private readonly string _frontendUrl;

        public AuthEmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<AuthEmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _frontendUrl = !string.IsNullOrEmpty(_emailSettings.FrontendUrl)
                ? _emailSettings.FrontendUrl
                : "https://wihngo.com";

            _logger.LogInformation("AuthEmailService initialized: FrontendUrl={FrontendUrl}, FromEmail={FromEmail}, HasApiKey={HasApiKey}",
                _frontendUrl, _emailSettings.FromEmail, !string.IsNullOrEmpty(_emailSettings.ApiKey));

            if (!string.IsNullOrEmpty(_emailSettings.ApiKey))
            {
                var options = new SendGridClientOptions { ApiKey = _emailSettings.ApiKey };

                // Set EU data residency if configured
                if (_emailSettings.DataResidency?.ToLower() == "eu")
                {
                    options.SetDataResidency("eu");
                }

                _client = new SendGridClient(options);
            }
        }

        public async Task SendEmailConfirmationAsync(string email, string name, string confirmationToken)
        {
            var confirmationUrl = $"{_frontendUrl}/auth/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(confirmationToken)}";
            
            var subject = "Confirm Your Wihngo Account";
            var htmlBody = BuildEmailConfirmationHtml(name, confirmationUrl);
            var plainTextBody = $@"
Hello {name},

Welcome to Wihngo! Please confirm your email address by clicking the link below:

{confirmationUrl}

This link will expire in 24 hours.

If you didn't create a Wihngo account, please ignore this email.

Best regards,
The Wihngo Team
";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
        }

        public async Task SendPasswordResetAsync(string email, string name, string resetToken)
        {
            _logger.LogInformation("SendPasswordResetAsync called for {Email}", email);
            var resetUrl = $"{_frontendUrl}/auth/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";

            // Debug logging
            Console.WriteLine("========== PASSWORD RESET EMAIL DEBUG ==========");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"Reset Token: {resetToken}");
            Console.WriteLine($"Reset URL: {resetUrl}");
            Console.WriteLine($"Frontend URL: {_frontendUrl}");
            Console.WriteLine($"From Email: {_emailSettings.FromEmail}");
            Console.WriteLine($"Has API Key: {!string.IsNullOrEmpty(_emailSettings.ApiKey)}");
            Console.WriteLine($"Has Client: {_client != null}");
            Console.WriteLine("================================================");
            
            var subject = "Reset Your Wihngo Password";
            var htmlBody = BuildPasswordResetHtml(name, resetUrl);
            var plainTextBody = $@"
Hello {name},

You requested to reset your password for your Wihngo account. Click the link below to set a new password:

{resetUrl}

This link will expire in 1 hour.

If you didn't request a password reset, please ignore this email and your password will remain unchanged.

Best regards,
The Wihngo Team
";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
        }

        public async Task SendWelcomeEmailAsync(string email, string name)
        {
            var subject = "Welcome to Wihngo!";
            var htmlBody = BuildWelcomeEmailHtml(name);
            var plainTextBody = $@"
Hello {name},

Welcome to Wihngo - where bird lovers connect and support our feathered friends!

Here's what you can do:
- Create profiles for your birds
- Share their stories and photos
- Connect with other bird enthusiasts
- Support bird conservation efforts

Get started: {_frontendUrl}/birds/create

Best regards,
The Wihngo Team
";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
        }

        public async Task SendSecurityAlertAsync(string email, string name, string alertType, string details)
        {
            var subject = $"Security Alert: {alertType}";
            var htmlBody = BuildSecurityAlertHtml(name, alertType, details);
            var plainTextBody = $@"
Hello {name},

Security Alert: {alertType}

{details}

If this wasn't you, please contact support immediately at support@wihngo.com or reset your password.

Best regards,
The Wihngo Team
";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
        }

        public async Task SendAccountUnlockedAsync(string email, string name)
        {
            var subject = "Your Wihngo Account Has Been Unlocked";
            var htmlBody = BuildAccountUnlockedHtml(name);
            var plainTextBody = $@"
Hello {name},

Your Wihngo account has been unlocked. You can now log in again.

If you continue to experience login issues or believe your account was compromised, please contact support at support@wihngo.com.

Best regards,
The Wihngo Team
";

            await SendEmailAsync(email, subject, htmlBody, plainTextBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
        {
            _logger.LogInformation("SendEmailAsync called: To={Email}, Subject={Subject}, HasClient={HasClient}, HasApiKey={HasApiKey}",
                toEmail, subject, _client != null, !string.IsNullOrEmpty(_emailSettings.ApiKey));

            try
            {
                if (_client == null || string.IsNullOrEmpty(_emailSettings.ApiKey))
                {
                    _logger.LogWarning("Email not configured. Would send to {Email}: {Subject}", toEmail, subject);
                    Console.WriteLine($"WARNING: Email not configured! Client null: {_client == null}, API key empty: {string.IsNullOrEmpty(_emailSettings.ApiKey)}");
                    return;
                }

                var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                var to = new EmailAddress(toEmail);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextBody, htmlBody);

                // Disable all tracking to preserve original URLs (important for localhost in development)
                msg.SetClickTracking(false, false);
                msg.SetOpenTracking(false);
                msg.SetGoogleAnalytics(false);
                msg.SetSubscriptionTracking(false);

                Console.WriteLine($"Sending email via SendGrid to: {toEmail}");
                var response = await _client.SendEmailAsync(msg);

                Console.WriteLine($"SendGrid Response Status: {response.StatusCode}");
                var responseBody = await response.Body.ReadAsStringAsync();
                Console.WriteLine($"SendGrid Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
                    Console.WriteLine($"SUCCESS: Email sent to {toEmail}");
                }
                else
                {
                    _logger.LogError("SendGrid email failed: {StatusCode} - {responseBody}", response.StatusCode, responseBody);
                    Console.WriteLine($"FAILED: SendGrid returned {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
                Console.WriteLine($"EXCEPTION: Failed to send email - {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private string BuildEmailConfirmationHtml(string name, string confirmationUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .content p {{ margin: 0 0 20px 0; font-size: 16px; }}
        .button {{ display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; font-size: 16px; }}
        .button:hover {{ opacity: 0.9; }}
        .footer {{ background-color: #f8f9fa; padding: 30px; text-align: center; font-size: 14px; color: #666; }}
        .footer a {{ color: #667eea; text-decoration: none; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; font-size: 14px; }}
        .icon {{ font-size: 48px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>??</div>
            <h1>Confirm Your Email</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{name}</strong>,</p>
            <p>Thank you for joining Wihngo! We're excited to have you as part of our community of bird lovers.</p>
            <p>Please confirm your email address to activate your account and start connecting with other bird enthusiasts:</p>
            <div style='text-align: center;'>
                <a href='{confirmationUrl}' class='button'>Confirm Email Address</a>
            </div>
            <div class='warning'>
                ?? This link will expire in 24 hours for security reasons.
            </div>
            <p style='font-size: 14px; color: #666; margin-top: 30px;'>If the button doesn't work, copy and paste this link into your browser:</p>
            <p style='font-size: 12px; word-break: break-all; color: #999;'>{confirmationUrl}</p>
        </div>
        <div class='footer'>
            <p>If you didn't create a Wihngo account, please ignore this email.</p>
            <p style='margin-top: 15px;'>
                <a href='{_frontendUrl}'>Visit Wihngo</a> � 
                <a href='mailto:support@wihngo.com'>Contact Support</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPasswordResetHtml(string name, string resetUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .content p {{ margin: 0 0 20px 0; font-size: 16px; }}
        .button {{ display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; font-size: 16px; }}
        .button:hover {{ opacity: 0.9; }}
        .footer {{ background-color: #f8f9fa; padding: 30px; text-align: center; font-size: 14px; color: #666; }}
        .footer a {{ color: #f5576c; text-decoration: none; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; font-size: 14px; }}
        .security {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; font-size: 14px; }}
        .icon {{ font-size: 48px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Reset Your Password</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{name}</strong>,</p>
            <p>We received a request to reset your password for your Wihngo account.</p>
            <p>Click the button below to choose a new password:</p>
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </div>
            <div class='warning'>
                <strong>Note:</strong> This link will expire in 1 hour for security reasons.
            </div>
            <div class='security'>
                <strong>Security Notice:</strong> If you didn't request this password reset, please ignore this email. Your password will remain unchanged.
            </div>
            <p style='font-size: 14px; color: #666; margin-top: 30px;'>If the button doesn't work, copy and paste this link into your browser:</p>
            <p style='font-size: 12px; word-break: break-all; color: #999;'>{resetUrl}</p>
        </div>
        <div class='footer'>
            <p>Need help? <a href='mailto:support@wihngo.com'>Contact our support team</a></p>
            <p style='margin-top: 15px;'>
                <a href='{_frontendUrl}'>Visit Wihngo</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildWelcomeEmailHtml(string name)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .content p {{ margin: 0 0 20px 0; font-size: 16px; }}
        .button {{ display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; font-size: 16px; }}
        .button:hover {{ opacity: 0.9; }}
        .feature-list {{ list-style: none; padding: 0; margin: 20px 0; }}
        .feature-list li {{ padding: 12px 0; border-bottom: 1px solid #eee; }}
        .feature-list li:last-child {{ border-bottom: none; }}
        .feature-icon {{ font-size: 24px; margin-right: 10px; }}
        .footer {{ background-color: #f8f9fa; padding: 30px; text-align: center; font-size: 14px; color: #666; }}
        .footer a {{ color: #43e97b; text-decoration: none; }}
        .icon {{ font-size: 64px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>??</div>
            <h1>Welcome to Wihngo!</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{name}</strong>,</p>
            <p>Welcome to Wihngo - the vibrant community where bird lovers connect, share, and support our feathered friends!</p>
            <p>Here's what you can do:</p>
            <ul class='feature-list'>
                <li><span class='feature-icon'>??</span> <strong>Create Bird Profiles</strong> - Showcase your birds with photos and stories</li>
                <li><span class='feature-icon'>??</span> <strong>Share Moments</strong> - Post updates and special moments</li>
                <li><span class='feature-icon'>??</span> <strong>Connect</strong> - Find and follow other bird enthusiasts</li>
                <li><span class='feature-icon'>??</span> <strong>Support Conservation</strong> - Help protect birds and their habitats</li>
            </ul>
            <div style='text-align: center;'>
                <a href='{_frontendUrl}/birds/create' class='button'>Create Your First Bird Profile</a>
            </div>
        </div>
        <div class='footer'>
            <p>Need help getting started? Check out our <a href='{_frontendUrl}/help'>Help Center</a></p>
            <p style='margin-top: 15px;'>
                <a href='{_frontendUrl}'>Explore Wihngo</a> � 
                <a href='mailto:support@wihngo.com'>Contact Support</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildSecurityAlertHtml(string name, string alertType, string details)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .content p {{ margin: 0 0 20px 0; font-size: 16px; }}
        .alert-box {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .button {{ display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; font-size: 16px; }}
        .button:hover {{ opacity: 0.9; }}
        .footer {{ background-color: #f8f9fa; padding: 30px; text-align: center; font-size: 14px; color: #666; }}
        .footer a {{ color: #ee5a6f; text-decoration: none; }}
        .icon {{ font-size: 48px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>??</div>
            <h1>Security Alert</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{name}</strong>,</p>
            <div class='alert-box'>
                <strong>{alertType}</strong>
                <p style='margin: 10px 0 0 0;'>{details}</p>
            </div>
            <p><strong>What you should do:</strong></p>
            <ul>
                <li>If this was you, no action is needed</li>
                <li>If this wasn't you, reset your password immediately</li>
                <li>Contact support if you have concerns</li>
            </ul>
            <div style='text-align: center;'>
                <a href='{_frontendUrl}/auth/forgot-password' class='button'>Reset Password</a>
            </div>
        </div>
        <div class='footer'>
            <p><strong>Important:</strong> This is an automated security notification. Please do not reply to this email.</p>
            <p style='margin-top: 15px;'>
                <a href='mailto:support@wihngo.com'>Contact Support</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildAccountUnlockedHtml(string name)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .content p {{ margin: 0 0 20px 0; font-size: 16px; }}
        .button {{ display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; font-size: 16px; }}
        .button:hover {{ opacity: 0.9; }}
        .footer {{ background-color: #f8f9fa; padding: 30px; text-align: center; font-size: 14px; color: #666; }}
        .footer a {{ color: #667eea; text-decoration: none; }}
        .icon {{ font-size: 48px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>??</div>
            <h1>Account Unlocked</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{name}</strong>,</p>
            <p>Your Wihngo account has been automatically unlocked and you can now log in again.</p>
            <p>Your account was temporarily locked due to multiple failed login attempts as a security precaution.</p>
            <div style='text-align: center;'>
                <a href='{_frontendUrl}/auth/login' class='button'>Log In to Wihngo</a>
            </div>
            <p><strong>Security Tips:</strong></p>
            <ul>
                <li>Use a strong, unique password</li>
                <li>Enable two-factor authentication (coming soon)</li>
                <li>Never share your password with anyone</li>
            </ul>
        </div>
        <div class='footer'>
            <p>If you continue to experience issues or believe your account was compromised, please contact support immediately.</p>
            <p style='margin-top: 15px;'>
                <a href='mailto:support@wihngo.com'>Contact Support</a>
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
