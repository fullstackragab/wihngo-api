# Mobile Team - Email Service Integration Guide

## Overview

The Wihngo API includes a comprehensive email service for authentication-related notifications. This guide explains how the email service works and what to expect when integrating with it from your mobile application.

---

## ?? Email Service Features

The email service (`IAuthEmailService`) automatically sends professional HTML emails for:

1. **Email Confirmation** - When users register
2. **Password Reset** - When users request password reset
3. **Welcome Email** - After successful email confirmation
4. **Security Alerts** - For security events (password changes, account lockouts)
5. **Account Unlocked** - When locked accounts are automatically unlocked

---

## ?? Supported Email Providers

The backend supports two email providers:

### 1. **SMTP (Default)**
- Works with any SMTP server (Gmail, Outlook, Mailtrap, etc.)
- Standard email delivery
- Configuration via `appsettings.json`

### 2. **SendGrid**
- Enterprise-grade email delivery
- Better deliverability and analytics
- Requires SendGrid API key

---

## ?? How It Works

### Email Confirmation Flow

```
[Mobile App] ? Register User ? [API]
                                  ?
                         Generate Confirmation Token
                                  ?
                         Send Confirmation Email
                                  ?
[User Email] ? Beautiful HTML Email with Link
                                  ?
                         User Clicks Link
                                  ?
[Mobile App] ? Confirm Email ? [API]
                                  ?
                         Send Welcome Email
```

### Password Reset Flow

```
[Mobile App] ? Forgot Password ? [API]
                                    ?
                          Generate Reset Token
                                    ?
                          Send Reset Email
                                    ?
[User Email] ? HTML Email with Reset Link
                                    ?
                          User Clicks Link
                                    ?
[Mobile App] ? Reset Password ? [API]
                                    ?
                          Send Security Alert
```

---

## ?? Mobile Integration Points

### 1. **Email Confirmation Links**

The backend sends emails with links in this format:

```
https://wihngo.com/auth/confirm-email?email={email}&token={token}
```

**Important for Mobile:**

- Configure deep links to handle these URLs
- Extract `email` and `token` parameters
- Call the confirmation API endpoint

**Example Deep Link Configuration:**

**iOS (Universal Links):**
```json
{
  "applinks": {
    "apps": [],
    "details": [{
      "appID": "TEAMID.com.wihngo.app",
      "paths": ["/auth/*"]
    }]
  }
}
```

**Android (App Links):**
```xml
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="https"
          android:host="wihngo.com"
          android:pathPrefix="/auth" />
</intent-filter>
```

### 2. **Password Reset Links**

Format:
```
https://wihngo.com/auth/reset-password?email={email}&token={token}
```

Handle similarly to email confirmation links.

---

## ?? API Endpoints Reference

### Email Confirmation
```http
POST /api/auth/confirm-email
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "confirmation-token-from-email"
}
```

### Resend Confirmation Email
```http
POST /api/auth/resend-confirmation
Content-Type: application/json

{
  "email": "user@example.com"
}
```

### Request Password Reset
```http
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```

### Reset Password
```http
POST /api/auth/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "reset-token-from-email",
  "newPassword": "NewSecurePassword123!"
}
```

---

## ?? Email Templates

All emails include:

- ? Responsive HTML design
- ? Professional branding with gradients
- ? Mobile-friendly layout
- ? Plain text fallback
- ? Security warnings
- ? Expiration times clearly stated
- ? Call-to-action buttons
- ? Fallback URLs (for copy/paste)

### Example Email Features

**Email Confirmation:**
- 24-hour expiration
- Branded header with bird emoji ??
- Purple gradient design
- Clear CTA button
- Security notice

**Password Reset:**
- 1-hour expiration
- Red/pink gradient for urgency
- Security warnings
- "Didn't request this?" notice

**Welcome Email:**
- Green gradient for positivity
- Feature highlights
- Getting started links
- Community information

---

## ?? Backend Configuration

The mobile team doesn't need to configure emails, but it's helpful to know:

### Configuration Location
`appsettings.json`:

```json
{
  "Smtp": {
    "Provider": "SMTP",
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@wihngo.com",
    "FromName": "Wihngo",
    "SendGridApiKey": null
  },
  "FrontendUrl": "https://wihngo.com"
}
```

### Environment Variables (Production)
```bash
Smtp__Host=smtp.sendgrid.net
Smtp__Port=587
Smtp__Username=apikey
Smtp__Password=SG.xxxxxxxxxxxxxxxxxxxxx
Smtp__Provider=SendGrid
Smtp__SendGridApiKey=SG.xxxxxxxxxxxxxxxxxxxxx
FrontendUrl=https://wihngo.com
```

---

## ?? Testing Email Integration

### Development Mode

When SMTP is not configured, the backend will:
1. Log email content to console
2. Continue without error
3. Allow you to test flows without email server

### Using Mailtrap (Recommended for Testing)

```json
{
  "Smtp": {
    "Host": "sandbox.smtp.mailtrap.io",
    "Port": 2525,
    "UseSsl": true,
    "Username": "your-mailtrap-username",
    "Password": "your-mailtrap-password"
  }
}
```

**Benefits:**
- Catches all test emails
- View emails in web interface
- Test HTML rendering
- No risk of sending to real users

---

## ?? Important Notes for Mobile Developers

### 1. **Handle Deep Links Properly**

```typescript
// React Native Example
import { Linking } from 'react-native';

Linking.addEventListener('url', (event) => {
  const url = new URL(event.url);
  
  if (url.pathname === '/auth/confirm-email') {
    const email = url.searchParams.get('email');
    const token = url.searchParams.get('token');
    
    // Navigate to confirmation screen
    navigation.navigate('ConfirmEmail', { email, token });
  }
  
  if (url.pathname === '/auth/reset-password') {
    const email = url.searchParams.get('email');
    const token = url.searchParams.get('token');
    
    // Navigate to reset password screen
    navigation.navigate('ResetPassword', { email, token });
  }
});
```

### 2. **Token Expiration**

| Email Type | Expiration | Field in Database |
|------------|------------|-------------------|
| Email Confirmation | 24 hours | `email_confirmation_token_expiry` |
| Password Reset | 1 hour | `password_reset_token_expiry` |

Always check token expiration on the API side. The backend will return appropriate errors:

```json
{
  "message": "Confirmation token has expired",
  "code": "TOKEN_EXPIRED"
}
```

### 3. **Error Handling**

Handle these common email-related errors:

```typescript
try {
  await confirmEmail(email, token);
} catch (error) {
  if (error.code === 'TOKEN_EXPIRED') {
    // Show "resend confirmation" option
  } else if (error.code === 'INVALID_TOKEN') {
    // Show error message
  } else if (error.code === 'EMAIL_ALREADY_CONFIRMED') {
    // Redirect to login
  }
}
```

### 4. **User Experience Tips**

**After Registration:**
```
? Show: "Check your email for confirmation link"
? Provide: "Resend email" button
? Allow: User to change email if typo
```

**Email Confirmation:**
```
? Automatic: Handle deep link automatically
? Manual: Allow manual token entry
? Success: Redirect to login or auto-login
```

**Password Reset:**
```
? Clear: "Check your email" message
? Timer: Show "Resend in X seconds"
? Help: Link to support if no email received
```

---

## ?? Security Considerations

### What the Backend Handles

? **Token Generation** - Cryptographically secure random tokens  
? **Token Expiration** - Automatic expiration checking  
? **Rate Limiting** - Prevents email bombing  
? **Token Hashing** - Tokens stored as hashes in database  
? **Single Use** - Tokens deleted after use  

### What Mobile Should Handle

? **Deep Link Validation** - Verify URL format  
? **HTTPS Only** - Use HTTPS for all API calls  
? **Token Storage** - Don't persist tokens longer than needed  
? **User Feedback** - Clear messages about security events  

---

## ?? Troubleshooting

### User Reports "Didn't Receive Email"

**Checklist:**

1. ? Check spam/junk folder (tell users)
2. ? Verify email address is correct
3. ? Use "Resend email" feature
4. ? Check backend logs for email errors
5. ? Verify SMTP configuration on server

### "Token Invalid or Expired"

**Causes:**

1. Token expired (normal behavior)
2. Token already used
3. User clicked old link
4. Email/token mismatch

**Solution:**

- Offer to resend fresh confirmation email
- Clear messaging about expiration

### Deep Links Not Working

**Check:**

1. Universal Links / App Links configured
2. Domain verification completed
3. AASA / assetlinks.json file served
4. URL scheme matches exactly
5. App installed vs browser fallback

---

## ?? Email Service Code Structure

### Backend Architecture

```
Wihngo/
??? Services/
?   ??? AuthEmailService.cs          # Main email service
?   ??? Interfaces/
?       ??? IAuthEmailService.cs     # Service interface
??? Configuration/
?   ??? SmtpConfiguration.cs         # Email config model
??? Controllers/
    ??? AuthController.cs            # Triggers emails
```

### Service Methods

```csharp
public interface IAuthEmailService
{
    Task SendEmailConfirmationAsync(string email, string name, string confirmationToken);
    Task SendPasswordResetAsync(string email, string name, string resetToken);
    Task SendWelcomeEmailAsync(string email, string name);
    Task SendSecurityAlertAsync(string email, string name, string alertType, string details);
    Task SendAccountUnlockedAsync(string email, string name);
}
```

---

## ?? Email Branding

### Colors Used

| Email Type | Gradient | Purpose |
|------------|----------|---------|
| Confirmation | Purple (#667eea ? #764ba2) | Trust, verification |
| Password Reset | Red/Pink (#f093fb ? #f5576c) | Urgency, security |
| Welcome | Green (#43e97b ? #38f9d7) | Growth, positivity |
| Security Alert | Red (#ff6b6b ? #ee5a6f) | Warning, attention |
| Account Unlocked | Purple (#667eea ? #764ba2) | Resolution, access |

### Emojis Used

- ?? Email Confirmation (bird theme)
- ?? Password Reset (security)
- ?? Welcome (celebration)
- ?? Security Alert (warning)
- ?? Account Unlocked (access restored)

---

## ?? Support Contact

If users have email delivery issues:

- **Support Email:** support@wihngo.com
- **API Issues:** Check backend logs
- **SMTP Issues:** Contact DevOps team

---

## ? Mobile Checklist

Before launching, ensure:

- [ ] Deep links configured for `/auth/confirm-email`
- [ ] Deep links configured for `/auth/reset-password`
- [ ] Handle expired tokens gracefully
- [ ] "Resend email" functionality implemented
- [ ] Clear user messaging for email steps
- [ ] Error handling for all email-related APIs
- [ ] Tested with real email provider
- [ ] Spam folder mentioned in UI
- [ ] Support contact info visible
- [ ] HTTPS enforced for all API calls

---

## ?? Quick Reference

### Key URLs

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/auth/register` | POST | Triggers confirmation email |
| `/api/auth/confirm-email` | POST | Confirms email with token |
| `/api/auth/resend-confirmation` | POST | Resends confirmation |
| `/api/auth/forgot-password` | POST | Triggers reset email |
| `/api/auth/reset-password` | POST | Resets password with token |

### Deep Link Patterns

```
https://wihngo.com/auth/confirm-email?email={email}&token={token}
https://wihngo.com/auth/reset-password?email={email}&token={token}
```

### Token Lifetimes

- **Email Confirmation:** 24 hours
- **Password Reset:** 1 hour

---

## ?? Additional Resources

- [Authentication Security Guide](./AUTHENTICATION_SECURITY.md)
- [Email Service Implementation](./EMAIL_SERVICE_GUIDE.md)
- [Quick Reference](./QUICK_REFERENCE.md)
- [API Documentation](./API_DOCUMENTATION.md)

---

**Last Updated:** January 2025  
**Backend Version:** .NET 10  
**Maintained By:** Wihngo Backend Team

For questions or issues, contact the backend team or open an issue in the repository.
