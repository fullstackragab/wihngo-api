# Email Service Configuration Guide

## Overview
The authentication system now includes a fully integrated email service for sending:
- Email confirmation messages
- Password reset links
- Welcome emails
- Security alerts
- Account unlock notifications

## Email Service Features

### ?? **Supported Email Types**
1. **Email Confirmation** - Sent after registration with 24-hour token
2. **Password Reset** - Sent when user requests password reset with 1-hour token
3. **Welcome Email** - Sent after email confirmation
4. **Security Alerts** - Sent after password changes
5. **Account Unlocked** - Sent when lockout period expires

### ?? **Professional HTML Templates**
All emails include:
- Responsive mobile-friendly design
- Beautiful gradient headers
- Clear call-to-action buttons
- Security warnings
- Both HTML and plain text versions
- Branded footer with support links

## Configuration

### Development Mode (Logging Only)
By default, in development mode, emails are logged to the console instead of being sent. You'll see:
```
[12:34:56] EMAIL: To=user@example.com, Subject=Confirm Your Email, Body=...
```

### Production Mode (SMTP Configuration)

#### Option 1: Gmail SMTP
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@wihngo.com",
    "FromName": "Wihngo"
  }
}
```

**Gmail App Password Setup:**
1. Enable 2-Factor Authentication on your Google account
2. Go to Google Account ? Security ? 2-Step Verification
3. Scroll to bottom and click "App passwords"
4. Generate a new app password for "Mail"
5. Use this 16-character password in the configuration

#### Option 2: SendGrid (Recommended for Production)
SendGrid provides better deliverability and analytics.

**Update `Services/AuthEmailService.cs`:**
```csharp
// Install NuGet package: SendGrid
using SendGrid;
using SendGrid.Helpers.Mail;

private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
{
    var apiKey = _configuration["SendGrid:ApiKey"];
    var client = new SendGridClient(apiKey);
    
    var from = new EmailAddress(_smtpConfig.FromEmail, _smtpConfig.FromName);
    var to = new EmailAddress(toEmail);
    
    var msg = MailHelper.CreateSingleEmail(
        from, 
        to, 
        subject, 
        plainTextBody ?? htmlBody, 
        htmlBody
    );
    
    var response = await client.SendEmailAsync(msg);
    
    if (!response.IsSuccessStatusCode)
    {
        _logger.LogError("SendGrid error: {StatusCode}", response.StatusCode);
    }
}
```

**Configuration:**
```json
{
  "SendGrid": {
    "ApiKey": "SG.your-sendgrid-api-key",
    "FromEmail": "noreply@wihngo.com",
    "FromName": "Wihngo"
  }
}
```

**SendGrid Setup:**
1. Sign up at https://sendgrid.com
2. Verify your sender email/domain
3. Create an API Key with "Mail Send" permissions
4. Add the API key to your configuration

#### Option 3: AWS SES
AWS Simple Email Service is cost-effective for high volumes.

```csharp
// Install NuGet package: AWSSDK.SimpleEmail
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
{
    using var client = new AmazonSimpleEmailServiceClient(
        _configuration["AWS:AccessKey"],
        _configuration["AWS:SecretKey"],
        Amazon.RegionEndpoint.USEast1
    );

    var request = new SendEmailRequest
    {
        Source = $"{_smtpConfig.FromName} <{_smtpConfig.FromEmail}>",
        Destination = new Destination { ToAddresses = new List<string> { toEmail } },
        Message = new Message
        {
            Subject = new Content(subject),
            Body = new Body
            {
                Html = new Content { Charset = "UTF-8", Data = htmlBody },
                Text = new Content { Charset = "UTF-8", Data = plainTextBody }
            }
        }
    };

    await client.SendEmailAsync(request);
}
```

**Configuration:**
```json
{
  "AWS": {
    "AccessKey": "your-aws-access-key",
    "SecretKey": "your-aws-secret-key",
    "Region": "us-east-1"
  }
}
```

## Environment Variables

For production deployments (Render, AWS, etc.), set these as environment variables:

```bash
# SMTP Configuration
SMTP__HOST=smtp.gmail.com
SMTP__PORT=587
SMTP__USESSL=true
SMTP__USERNAME=your-email@gmail.com
SMTP__PASSWORD=your-app-password
SMTP__FROMEMAIL=noreply@wihngo.com
SMTP__FROMNAME=Wihngo

# Frontend URL (for email links)
FRONTENDURL=https://wihngo.com
```

## Testing Email Delivery

### 1. Test Email Configuration
Create a test endpoint in `AuthController.cs`:

```csharp
[HttpPost("test-email")]
[Authorize]
public async Task<IActionResult> TestEmail([FromQuery] string email)
{
    try
    {
        await _authEmailService.SendWelcomeEmailAsync(email, "Test User");
        return Ok(new { message = "Test email sent successfully" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = ex.Message });
    }
}
```

Call it:
```bash
POST /api/auth/test-email?email=your-test@email.com
Authorization: Bearer your-jwt-token
```

### 2. Check Logs
Monitor application logs for email sending:
```
[12:34:56] Email sent successfully to user@example.com: Welcome to Wihngo!
```

Or errors:
```
[12:34:56] Failed to send email to user@example.com: SMTP connection failed
```

### 3. Verify Email Delivery
- Check inbox (and spam folder)
- Verify email formatting on desktop and mobile
- Test all links in the email
- Confirm unsubscribe links work

## Email Templates Customization

All email templates are in `Services/AuthEmailService.cs`. You can customize:

### Colors and Branding
```csharp
// Email confirmation gradient
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);

// Password reset gradient
background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);

// Welcome email gradient
background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
```

### Logo
Add your logo to the header:
```html
<div class='header'>
    <img src='https://wihngo.com/logo.png' alt='Wihngo' style='max-width: 200px;'>
    <h1>Confirm Your Email</h1>
</div>
```

### Footer Links
Update footer links in each template method.

## Security Best Practices

### 1. Token Security
- ? Tokens are single-use
- ? Tokens expire (24h for email, 1h for password reset)
- ? Tokens are base64 encoded GUIDs
- ? Tokens are invalidated after use

### 2. Email Security
- Never include sensitive information in emails
- Use HTTPS links only
- Include warnings about phishing
- Add "If you didn't request this" messages

### 3. Rate Limiting
- Email sending is protected by rate limiting middleware
- Maximum 5 email-related requests per 15 minutes per IP

### 4. Spam Prevention
```csharp
// Don't reveal if email exists
return Ok(new { message = "If the email exists, a link has been sent." });
```

## Troubleshooting

### Emails Not Sending

**Check 1: SMTP Configuration**
```bash
# Test SMTP connection
telnet smtp.gmail.com 587
```

**Check 2: Credentials**
- Verify username and password
- For Gmail, use app password (not regular password)
- Check if 2FA is enabled

**Check 3: Firewall/Network**
- Ensure port 587 (or 465 for SSL) is open
- Check firewall rules
- Verify SSL/TLS settings

**Check 4: Email Service Limits**
- Gmail: 500 emails per day for free accounts
- SendGrid: Check your plan limits
- AWS SES: May be in sandbox mode (needs verification)

### Emails Going to Spam

**Solutions:**
1. **Verify Domain** - Add SPF, DKIM, and DMARC records
2. **Warm Up** - Start with low volume and gradually increase
3. **Content** - Avoid spam trigger words
4. **Engagement** - Monitor bounce rates and unsubscribes
5. **Use Dedicated IP** - For high volumes

### Template Not Rendering

**Check:**
- Email client compatibility (some don't support CSS)
- Use tables for layout (more compatible)
- Inline CSS (external stylesheets often blocked)
- Test with Email on Acid or Litmus

## Monitoring and Analytics

### Log Important Events
```csharp
_logger.LogInformation("Email sent: Type={Type}, To={Email}, Status={Status}", 
    "confirmation", email, "success");
```

### Track Metrics
- Email delivery rate
- Open rate (if using analytics service)
- Click-through rate on links
- Bounce rate
- Unsubscribe rate

### Alerts
Set up alerts for:
- High failure rates
- Unusual sending patterns
- Quota approaching limits

## Production Checklist

- [ ] SMTP credentials configured
- [ ] FrontendUrl set correctly
- [ ] Email templates tested on multiple clients
- [ ] Links verified (confirm email, password reset)
- [ ] Spam score checked
- [ ] Domain verified (SPF, DKIM, DMARC)
- [ ] Rate limits appropriate
- [ ] Logging and monitoring enabled
- [ ] Backup email service configured
- [ ] Unsubscribe functionality implemented
- [ ] Privacy policy and terms linked
- [ ] Support email monitored

## Support

For issues or questions:
- Email: support@wihngo.com
- Documentation: https://wihngo.com/docs
- GitHub Issues: https://github.com/wihngo/api/issues

---

**Last Updated:** 2024
**Version:** 1.0
