# ?? Complete Environment Variables Reference

## ?? All Environment Variables for Wihngo

This document lists ALL environment variables your application uses.

---

## ??? Database Connection (PostgreSQL)

**Status:** ? Required  
**Purpose:** Main application database

```bash
DEFAULT_CONNECTION=Host=dpg-xxx.oregon-postgres.render.com;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=True;Pooling=true
```

**Setup Guide:** `DATABASE_CONNECTION_SETUP.md`

**?? IMPORTANT:** This is now **required**! Connection string has been removed from `appsettings.json` for security.

---

## ?? AWS S3 (Media Upload)

**Status:** ? Required  
**Purpose:** Profile images, bird photos, videos

```bash
AWS_ACCESS_KEY_ID=AKIA...
AWS_SECRET_ACCESS_KEY=your-secret-key
AWS_BUCKET_NAME=amzn-s3-wihngo-bucket
AWS_REGION=us-east-1
AWS_PRESIGNED_URL_EXPIRATION_MINUTES=10
```

**Setup Guide:** `QUICK_AWS_SETUP.md`

---

## ?? SendGrid (Email)

**Status:** ? Required  
**Purpose:** Registration, password reset, notifications

```bash
SENDGRID_API_KEY=SG.xxxxx...
EMAIL_PROVIDER=SendGrid
EMAIL_FROM=noreply@wihngo.com
EMAIL_FROM_NAME=Wihngo
```

**Setup Guide:** `SENDGRID_QUICK_SETUP.md`

---

## ?? JWT Authentication

**Status:** ?? Optional (defaults in appsettings.json)  
**Purpose:** User authentication tokens

```bash
# Optional: Can override appsettings.json
Jwt__Key=your-secret-key-min-32-chars
Jwt__Issuer=wihngo-api
Jwt__Audience=wihngo-app
Jwt__ExpiryHours=24
```

**Current:** Already configured in `appsettings.json`

---

## ?? Application Settings

**Status:** ?? Optional  
**Purpose:** Frontend URL for email links

```bash
FrontendUrl=https://wihngo.com
ASPNETCORE_ENVIRONMENT=Production
```

---

## ? Quick Setup Commands

### PowerShell (Windows - Run as Administrator):

```powershell
# Database Connection (REQUIRED)
[System.Environment]::SetEnvironmentVariable('DEFAULT_CONNECTION', 'Host=dpg-xxx.oregon-postgres.render.com;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=True', 'User')

# AWS S3
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'YOUR_KEY', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_SECRET_ACCESS_KEY', 'YOUR_SECRET', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_BUCKET_NAME', 'amzn-s3-wihngo-bucket', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_REGION', 'us-east-1', 'User')

# SendGrid Email
[System.Environment]::SetEnvironmentVariable('SENDGRID_API_KEY', 'SG.YOUR_KEY', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_PROVIDER', 'SendGrid', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_FROM', 'noreply@wihngo.com', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_FROM_NAME', 'Wihngo', 'User')

# Application
[System.Environment]::SetEnvironmentVariable('FrontendUrl', 'https://wihngo.com', 'User')
```

**Remember:** Restart Visual Studio after setting variables!

---

### User Secrets (Recommended for Development):

```bash
cd C:\.net\Wihngo

# Database Connection (REQUIRED)
dotnet user-secrets set "DEFAULT_CONNECTION" "Host=dpg-xxx.oregon-postgres.render.com;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=True"

# AWS S3
dotnet user-secrets set "AWS_ACCESS_KEY_ID" "YOUR_KEY"
dotnet user-secrets set "AWS_SECRET_ACCESS_KEY" "YOUR_SECRET"

# SendGrid
dotnet user-secrets set "SENDGRID_API_KEY" "SG.YOUR_KEY"
dotnet user-secrets set "EMAIL_PROVIDER" "SendGrid"

# Application
dotnet user-secrets set "FrontendUrl" "https://wihngo.com"
```

---

### Render (Production):

Go to https://dashboard.render.com ? Your Service ? Environment

```
DEFAULT_CONNECTION = Host=dpg-xxx.oregon-postgres.render.com;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=True;Pooling=true

AWS_ACCESS_KEY_ID = YOUR_KEY
AWS_SECRET_ACCESS_KEY = YOUR_SECRET
AWS_BUCKET_NAME = amzn-s3-wihngo-bucket
AWS_REGION = us-east-1

SENDGRID_API_KEY = SG.YOUR_KEY
EMAIL_PROVIDER = SendGrid
EMAIL_FROM = noreply@wihngo.com
EMAIL_FROM_NAME = Wihngo

FrontendUrl = https://wihngo.com
ASPNETCORE_ENVIRONMENT = Production
```

---

## ?? Verify Configuration

After setting environment variables, check startup logs:

### Database Configuration:
```
Database Configuration:
  Connection: ***configured***
  Database: wihngo_kzno
```

### AWS Configuration:
```
AWS Configuration loaded:
  Access Key: ***XXXX
  Secret Key: ***configured***
  Bucket: amzn-s3-wihngo-bucket
  Region: us-east-1
```

### Email Configuration:
```
Email Configuration loaded:
  Provider: SendGrid
  SendGrid API Key: ***configured***
  From Email: noreply@wihngo.com
```

---

## ?? Configuration Priority

The application checks in this order:

1. **Environment Variables** (highest priority)
   - `DEFAULT_CONNECTION`
   - `AWS_ACCESS_KEY_ID`
   - `SENDGRID_API_KEY`

2. **User Secrets** (development)
   - Stored in `~/.microsoft/usersecrets/`

3. **appsettings.json** (fallback)
   - Most credentials removed for security
   - Only non-sensitive defaults remain

4. **Default Values** (if nothing set)
   - Bucket: `amzn-s3-wihngo-bucket`
   - Region: `us-east-1`
   - Email Provider: `SMTP`

---

## ?? Check Current Variables

### PowerShell:
```powershell
# Check specific variable
$env:DEFAULT_CONNECTION
$env:AWS_ACCESS_KEY_ID
$env:SENDGRID_API_KEY

# List all database variables
Get-ChildItem Env: | Where-Object { $_.Name -like "*CONNECTION*" }

# List all AWS variables
Get-ChildItem Env: | Where-Object { $_.Name -like "AWS*" }

# List all email variables
Get-ChildItem Env: | Where-Object { $_.Name -like "*MAIL*" -or $_.Name -like "*EMAIL*" }
```

### CMD:
```cmd
echo %DEFAULT_CONNECTION%
echo %AWS_ACCESS_KEY_ID%
echo %SENDGRID_API_KEY%
```

---

## ?? Setup Checklist

### Database:
- [ ] Got database credentials from Render
- [ ] Set `DEFAULT_CONNECTION` environment variable
- [ ] Visual Studio restarted
- [ ] Logs show "Database Configuration: ***configured***"
- [ ] Application connects successfully

### AWS S3:
- [ ] IAM user created: `wihngo-media-signer`
- [ ] Access key generated
- [ ] IAM policy attached (s3:PutObject, s3:GetObject)
- [ ] Environment variables set
- [ ] Visual Studio restarted
- [ ] Logs show "AWS Configuration loaded"

### SendGrid:
- [ ] SendGrid account created
- [ ] API key generated
- [ ] Sender email verified
- [ ] Environment variable set
- [ ] Visual Studio restarted
- [ ] Logs show "Email Configuration loaded"

### Testing:
- [ ] Test database connection (registration)
- [ ] Test S3 upload (from mobile app)
- [ ] Test email (registration flow)
- [ ] Check SendGrid Activity dashboard
- [ ] Verify files in S3 bucket

---

## ?? Security Notes

### ? Good Practices:
- Use environment variables (not appsettings.json)
- Different credentials for dev and production
- Rotate credentials every 90 days
- Use User Secrets for development
- Add `.env` and `launchSettings.json` to `.gitignore`
- Use restricted IAM/API permissions

### ? Never Do:
- Commit credentials to Git
- Share credentials in plain text
- Use production credentials in development
- Give excessive permissions
- Leave debug logging enabled in production

---

## ?? Common Issues

### "Database connection string is not configured"

**Cause:** `DEFAULT_CONNECTION` environment variable not set

**Solution:**
1. Set the variable
2. Restart Visual Studio
3. Verify with: `$env:DEFAULT_CONNECTION`

### "NOT SET" in Logs

**Cause:** Environment variable not configured

**Solution:**
1. Set the variable
2. Restart Visual Studio/application
3. Verify with PowerShell

### Variables Work in Terminal but Not in Visual Studio

**Cause:** Visual Studio doesn't reload environment variables

**Solution:**
1. Close Visual Studio completely
2. Reopen Visual Studio
3. Or use User Secrets instead

### Works Locally but Fails on Render

**Cause:** Environment variables not set on Render

**Solution:**
1. Render Dashboard ? Environment
2. Add all required variables
3. Save (triggers redeploy)

---

## ?? Documentation Index

| Topic | Document |
|-------|----------|
| Database Setup | `DATABASE_CONNECTION_SETUP.md` |
| AWS S3 Setup | `QUICK_AWS_SETUP.md` |
| AWS Troubleshooting | `AWS_ENVIRONMENT_VARIABLES_GUIDE.md` |
| IAM Permissions | `ADD_IAM_POLICY_NOW.md` |
| SendGrid Setup | `SENDGRID_QUICK_SETUP.md` |
| SendGrid Details | `SENDGRID_INTEGRATION_GUIDE.md` |
| Complete Summary | This document |

---

## ?? Minimum Required Variables

To get started, you **must** have:

```bash
# REQUIRED
DEFAULT_CONNECTION=...    # Database connection
AWS_ACCESS_KEY_ID=...     # S3 uploads
AWS_SECRET_ACCESS_KEY=... # S3 uploads
SENDGRID_API_KEY=SG...    # Email sending
```

Optional but recommended:
```bash
EMAIL_PROVIDER=SendGrid
EMAIL_FROM=noreply@wihngo.com
```

Everything else has sensible defaults!

---

## ?? Recent Changes

### v2.0 - Database Security Update:
- ? **Database connection string** moved to environment variable
- ? Removed `ConnectionStrings` from `appsettings.json`
- ? Added database configuration logging
- ? Required: `DEFAULT_CONNECTION` must be set

**Migration:** Set `DEFAULT_CONNECTION` environment variable with your database connection string.

---

**? Set these variables and your application is fully configured!** ??

**Quick Start:**
1. Database: `DATABASE_CONNECTION_SETUP.md`
2. AWS: `QUICK_AWS_SETUP.md`
3. SendGrid: `SENDGRID_QUICK_SETUP.md`
4. Test all features
5. Deploy to production
