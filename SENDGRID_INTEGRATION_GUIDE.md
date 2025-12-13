# ?? SendGrid Email Integration Guide

## ?? Overview

Your Wihngo application now supports SendGrid for sending emails including:
- ? User registration confirmation
- ? Password reset emails
- ? Welcome emails
- ? Security alerts
- ? Account unlock notifications
- ? Invoice notifications
- ? Payment confirmations
- ? Notifications and digests

---

## ?? Required Environment Variables

Set these environment variables for SendGrid:

```bash
SENDGRID_API_KEY=YOUR_SENDGRID_API_KEY
EMAIL_PROVIDER=SendGrid
EMAIL_FROM=noreply@wihngo.com
EMAIL_FROM_NAME=Wihngo
```

**Optional (for SMTP fallback):**
```bash
SMTP_HOST=smtp.sendgrid.net
SMTP_PORT=587
SMTP_USE_SSL=true
SMTP_USERNAME=apikey
SMTP_PASSWORD=YOUR_SENDGRID_API_KEY
```

---

## ?? Local Development Setup

### Option 1: PowerShell (Windows)

#### Permanent Setup:
```powershell
# Run as Administrator
[System.Environment]::SetEnvironmentVariable('SENDGRID_API_KEY', 'YOUR_SENDGRID_API_KEY', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_PROVIDER', 'SendGrid', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_FROM', 'noreply@wihngo.com', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_FROM_NAME', 'Wihngo', 'User')
```

**Remember:** Restart Visual Studio after setting environment variables!

---

### Option 2: User Secrets (Recommended for Development)

```bash
cd C:\.net\Wihngo

dotnet user-secrets set "SENDGRID_API_KEY" "YOUR_SENDGRID_API_KEY"
dotnet user-secrets set "EMAIL_PROVIDER" "SendGrid"
dotnet user-secrets set "EMAIL_FROM" "noreply@wihngo.com"
dotnet user-secrets set "EMAIL_FROM_NAME" "Wihngo"
```

---

### Option 3: launchSettings.json

Edit `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "SENDGRID_API_KEY": "YOUR_SENDGRID_API_KEY",
        "EMAIL_PROVIDER": "SendGrid",
        "EMAIL_FROM": "noreply@wihngo.com",
        "EMAIL_FROM_NAME": "Wihngo"
      }
    }
  }
}
```

**?? Add to `.gitignore`!**

---

## ?? Production Setup (Render)

### Render Dashboard:

1. Go to https://dashboard.render.com
2. Select your service: `wihngo-api`
3. Click **"Environment"** tab
4. Add these variables:

```
SENDGRID_API_KEY = YOUR_SENDGRID_API_KEY
EMAIL_PROVIDER = SendGrid
EMAIL_FROM = noreply@wihngo.com
EMAIL_FROM_NAME = Wihngo
```

5. Click **"Save Changes"**
6. Render will automatically redeploy

---

## ?? Getting Your SendGrid API Key

### Step 1: Log in to SendGrid

Go to: https://app.sendgrid.com

### Step 2: Create API Key

1. Click **"Settings"** ? **"API Keys"** in left sidebar
2. Click **"Create API Key"** button
3. **Name:** `Wihngo Production` (or `Wihngo Development`)
4. **API Key Permissions:** Select **"Full Access"** or **"Restricted Access"**

**For Restricted Access, enable:**
- Mail Send: **Full Access**
- Mail Settings: **Read Access**
- Stats: **Read Access**

5. Click **"Create & View"**
6. **Copy the API key immediately** (you won't see it again!)

**Your API key will look like:**
```
SG.xxxxxxxxxxxx.yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy
```

---

## ?? Sender Authentication (Required!)

SendGrid requires you to verify your sender email or domain.

### Option A: Single Sender Verification (Quick)

1. Go to **"Settings"** ? **"Sender Authentication"**
2. Click **"Single Sender Verification"**
3. Fill in details:
   - From Name: `Wihngo`
   - From Email Address: `noreply@wihngo.com`
   - Reply To: `support@wihngo.com`
   - Company Address: Your address
4. Click **"Create"**
5. **Check your email** and click the verification link

### Option B: Domain Authentication (Recommended for Production)

1. Go to **"Settings"** ? **"Sender Authentication"**
2. Click **"Authenticate Your Domain"**
3. Select your DNS host
4. Follow the instructions to add DNS records:
   - CNAME records for authentication
   - DKIM and SPF records
5. Wait for DNS propagation (can take up to 48 hours)
6. Click **"Verify"** in SendGrid dashboard

**Domain authentication improves email deliverability!**

---

## ?? Testing SendGrid Integration

### Test 1: Check Configuration

After restarting your app, check the logs:

```
Email Configuration loaded:
  Provider: SendGrid
  SendGrid API Key: ***configured***
  SMTP Host: smtp.sendgrid.net
  SMTP Port: 587
  From Email: noreply@wihngo.com
```

**If you see:**
```
SendGrid API Key: NOT SET
```

Then environment variables are not configured!

---

### Test 2: Test User Registration

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "your-test-email@example.com",
    "password": "Test@1234Strong"
  }'
```

**Expected:**
- ? User registered successfully
- ? Confirmation email sent to your test email
- ? Check spam folder if not in inbox

---

### Test 3: Test Password Reset

```bash
curl -X POST http://localhost:5000/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "your-test-email@example.com"
  }'
```

**Expected:**
- ? Password reset email sent
- ? Contains reset link

---

## ?? Email Types & Templates

Your application sends these emails:

| Email Type | Trigger | Template |
|------------|---------|----------|
| **Email Confirmation** | User registration | Beautiful HTML template with confirmation button |
| **Welcome Email** | Email confirmed | Welcome message with getting started guide |
| **Password Reset** | Forgot password | Secure reset link (expires in 1 hour) |
| **Password Changed** | Password changed | Security alert notification |
| **Account Unlocked** | Account auto-unlocked | Login instructions |
| **Security Alert** | Security events | Custom alert messages |

All emails include:
- ? Responsive HTML design
- ? Plain text alternative
- ? Mobile-friendly layout
- ? Professional branding

---

## ?? Monitoring & Debugging

### View SendGrid Activity

1. Go to SendGrid Dashboard
2. Click **"Activity"** in left sidebar
3. See all sent emails, delivery status, opens, clicks

### Check Email Logs

Your application logs email sending:

```
[12:34:56] info: Wihngo.Services.AuthEmailService
  Email sent via SendGrid to test@example.com: Confirm Your Wihngo Account
```

**If email fails:**
```
[12:34:56] fail: Wihngo.Services.AuthEmailService
  SendGrid email failed: 403 - Forbidden
  Reason: The provided authorization grant is invalid, expired, or revoked
```

---

## ?? Troubleshooting

### Issue: "SendGrid API Key: NOT SET"

**Cause:** Environment variable not configured

**Solution:**
1. Set `SENDGRID_API_KEY` environment variable
2. Restart Visual Studio/application
3. Verify with: `$env:SENDGRID_API_KEY` (PowerShell)

---

### Issue: "403 Forbidden" from SendGrid

**Cause 1:** Invalid or expired API key

**Solution:**
1. Generate new API key in SendGrid
2. Update environment variable
3. Restart application

**Cause 2:** API key permissions too restrictive

**Solution:**
1. Go to SendGrid ? Settings ? API Keys
2. Edit your API key
3. Grant **"Mail Send"** permission
4. Save changes

---

### Issue: "401 Unauthorized"

**Cause:** API key not correctly set

**Solution:**
1. Check API key starts with `SG.`
2. No extra spaces or quotes
3. Verify environment variable name: `SENDGRID_API_KEY`

---

### Issue: Emails not being delivered

**Cause 1:** Sender not verified

**Solution:**
1. Go to SendGrid ? Settings ? Sender Authentication
2. Verify your sender email or domain
3. Check verification email

**Cause 2:** Emails going to spam

**Solution:**
1. Set up domain authentication (SPF/DKIM)
2. Use a custom domain (not @gmail.com)
3. Warm up your sending reputation gradually

**Cause 3:** SendGrid account suspended

**Solution:**
1. Check SendGrid dashboard for alerts
2. Verify your account is in good standing
3. Contact SendGrid support if needed

---

### Issue: "Invalid template ID"

**Cause:** Using SendGrid templates (not currently implemented)

**Solution:**
The current implementation uses inline HTML templates. If you want to use SendGrid templates:

```csharp
// In AuthEmailService.cs
var msg = new SendGridMessage();
msg.SetTemplateId("YOUR_TEMPLATE_ID");
msg.SetTemplateData(new { name = userName, confirmationUrl = url });
```

---

## ?? Switching Between SMTP and SendGrid

Your application automatically chooses the provider:

### Use SendGrid (Recommended):
```bash
EMAIL_PROVIDER=SendGrid
SENDGRID_API_KEY=SG.xxx
```

### Use SMTP:
```bash
EMAIL_PROVIDER=SMTP
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
```

---

## ?? Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `SENDGRID_API_KEY` | **Yes** | - | Your SendGrid API key |
| `EMAIL_PROVIDER` | No | `SMTP` | Email provider (`SendGrid` or `SMTP`) |
| `EMAIL_FROM` | No | `noreply@wihngo.com` | Sender email address |
| `EMAIL_FROM_NAME` | No | `Wihngo` | Sender display name |
| `SMTP_HOST` | No | `smtp.sendgrid.net` | SMTP server (if using SMTP) |
| `SMTP_PORT` | No | `587` | SMTP port |
| `SMTP_USE_SSL` | No | `true` | Use SSL/TLS |
| `SMTP_USERNAME` | No | `apikey` | SMTP username |
| `SMTP_PASSWORD` | No | - | SMTP password (SendGrid API key) |

---

## ?? Security Best Practices

### ? DO:
- Use environment variables for API key
- Rotate API keys regularly (every 90 days)
- Use restricted API keys (least privilege)
- Set up domain authentication (SPF/DKIM)
- Monitor SendGrid activity dashboard
- Keep API keys out of Git

### ? DON'T:
- Commit API keys to Git
- Share API keys in plain text
- Use same API key for dev and production
- Grant "Full Access" unless necessary
- Ignore SendGrid security alerts

---

## ?? SendGrid Free Tier Limits

SendGrid Free Plan includes:
- ? 100 emails/day
- ? Single sender verification
- ? Email API
- ? SMTP relay
- ? Activity feed (3 days)

For production, consider upgrading:
- **Essentials:** $19.95/mo - 50,000 emails/mo
- **Pro:** $89.95/mo - 100,000 emails/mo

---

## ?? Testing with Real Emails

### Use These Test Email Services:

1. **Mailtrap** (Development)
   - https://mailtrap.io
   - Catches all test emails
   - Free plan: 500 emails/month

2. **SendGrid Sandbox** (Development)
   - Enable sandbox mode in SendGrid
   - Emails logged but not delivered

3. **Temp Email** (Quick Tests)
   - https://temp-mail.org
   - Disposable email addresses

---

## ?? Quick Start Checklist

- [ ] Get SendGrid API key
- [ ] Verify sender email in SendGrid
- [ ] Set `SENDGRID_API_KEY` environment variable
- [ ] Set `EMAIL_PROVIDER=SendGrid`
- [ ] Set `EMAIL_FROM` to your verified email
- [ ] Restart application
- [ ] Check logs for "Email Configuration loaded"
- [ ] Test user registration
- [ ] Check email delivery in SendGrid Activity
- [ ] Verify email received in inbox

---

## ?? Additional Resources

- **SendGrid Documentation:** https://docs.sendgrid.com
- **SendGrid C# Library:** https://github.com/sendgrid/sendgrid-csharp
- **Sender Authentication Guide:** https://docs.sendgrid.com/ui/account-and-settings/how-to-set-up-domain-authentication
- **Email Best Practices:** https://sendgrid.com/resource/email-marketing-best-practices/

---

## ?? Next Steps

1. **Set up domain authentication** for better deliverability
2. **Create SendGrid templates** for consistent branding
3. **Set up webhooks** for tracking opens/clicks
4. **Monitor sending reputation** in SendGrid
5. **Implement unsubscribe handling** (if sending marketing emails)

---

**? SendGrid is now integrated! Set your API key and test!** ??
