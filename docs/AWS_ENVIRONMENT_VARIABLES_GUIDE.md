# AWS Environment Variables Configuration Guide

## ?? Secure Credential Management

AWS credentials are now read from **environment variables** instead of `appsettings.json`. This is the recommended secure practice.

---

## ?? Required Environment Variables

Set these environment variables in your development and production environments:

```bash
AWS_ACCESS_KEY_ID=YOUR_ACTUAL_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY=YOUR_ACTUAL_SECRET_ACCESS_KEY
AWS_BUCKET_NAME=amzn-s3-wihngo-bucket
AWS_REGION=us-east-1
AWS_PRESIGNED_URL_EXPIRATION_MINUTES=10
```

---

## ?? Local Development Setup

### Option 1: Windows (PowerShell)

#### Temporary (Current Session Only)
```powershell
$env:AWS_ACCESS_KEY_ID="YOUR_ACTUAL_ACCESS_KEY_ID"
$env:AWS_SECRET_ACCESS_KEY="YOUR_ACTUAL_SECRET_ACCESS_KEY"
$env:AWS_BUCKET_NAME="amzn-s3-wihngo-bucket"
$env:AWS_REGION="us-east-1"
$env:AWS_PRESIGNED_URL_EXPIRATION_MINUTES="10"
```

#### Permanent (User Environment Variables)
```powershell
# Run as Administrator
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'YOUR_ACTUAL_ACCESS_KEY_ID', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_SECRET_ACCESS_KEY', 'YOUR_ACTUAL_SECRET_ACCESS_KEY', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_BUCKET_NAME', 'amzn-s3-wihngo-bucket', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_REGION', 'us-east-1', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_PRESIGNED_URL_EXPIRATION_MINUTES', '10', 'User')
```

**Note:** Restart Visual Studio after setting permanent environment variables!

---

### Option 2: Visual Studio launchSettings.json

Edit `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "AWS_ACCESS_KEY_ID": "YOUR_ACTUAL_ACCESS_KEY_ID",
        "AWS_SECRET_ACCESS_KEY": "YOUR_ACTUAL_SECRET_ACCESS_KEY",
        "AWS_BUCKET_NAME": "amzn-s3-wihngo-bucket",
        "AWS_REGION": "us-east-1",
        "AWS_PRESIGNED_URL_EXPIRATION_MINUTES": "10"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "AWS_ACCESS_KEY_ID": "YOUR_ACTUAL_ACCESS_KEY_ID",
        "AWS_SECRET_ACCESS_KEY": "YOUR_ACTUAL_SECRET_ACCESS_KEY",
        "AWS_BUCKET_NAME": "amzn-s3-wihngo-bucket",
        "AWS_REGION": "us-east-1",
        "AWS_PRESIGNED_URL_EXPIRATION_MINUTES": "10"
      }
    }
  }
}
```

**?? Important:** Add `launchSettings.json` to `.gitignore` to prevent committing credentials!

---

### Option 3: .env File (with dotenv)

1. **Install dotenv package:**
```bash
dotnet add package DotNetEnv
```

2. **Create `.env` file in project root:**
```env
AWS_ACCESS_KEY_ID=YOUR_ACTUAL_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY=YOUR_ACTUAL_SECRET_ACCESS_KEY
AWS_BUCKET_NAME=amzn-s3-wihngo-bucket
AWS_REGION=us-east-1
AWS_PRESIGNED_URL_EXPIRATION_MINUTES=10
```

3. **Load in Program.cs (top of file):**
```csharp
DotNetEnv.Env.Load();
```

4. **Add to `.gitignore`:**
```gitignore
.env
.env.local
```

---

### Option 4: User Secrets (Recommended for Development)

```bash
cd C:\.net\Wihngo

dotnet user-secrets set "AWS_ACCESS_KEY_ID" "YOUR_ACTUAL_ACCESS_KEY_ID"
dotnet user-secrets set "AWS_SECRET_ACCESS_KEY" "YOUR_ACTUAL_SECRET_ACCESS_KEY"
dotnet user-secrets set "AWS_BUCKET_NAME" "amzn-s3-wihngo-bucket"
dotnet user-secrets set "AWS_REGION" "us-east-1"
dotnet user-secrets set "AWS_PRESIGNED_URL_EXPIRATION_MINUTES" "10"
```

**Benefits:**
- ? Stored outside project directory
- ? Not committed to Git
- ? Per-user configuration
- ? Works seamlessly in Visual Studio

---

## ?? Production Setup (Render)

### Render Environment Variables

1. Go to **Render Dashboard**
2. Select your service: `wihngo-api`
3. Click **"Environment"** tab
4. Add these variables:

```
AWS_ACCESS_KEY_ID = YOUR_ACTUAL_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY = YOUR_ACTUAL_SECRET_ACCESS_KEY
AWS_BUCKET_NAME = amzn-s3-wihngo-bucket
AWS_REGION = us-east-1
AWS_PRESIGNED_URL_EXPIRATION_MINUTES = 10
```

5. Click **"Save Changes"**
6. Render will automatically redeploy

---

## ?? Other Hosting Platforms

### Heroku
```bash
heroku config:set AWS_ACCESS_KEY_ID=YOUR_ACTUAL_ACCESS_KEY_ID
heroku config:set AWS_SECRET_ACCESS_KEY=YOUR_ACTUAL_SECRET_ACCESS_KEY
heroku config:set AWS_BUCKET_NAME=amzn-s3-wihngo-bucket
heroku config:set AWS_REGION=us-east-1
```

### Azure App Service
```bash
az webapp config appsettings set --resource-group MyResourceGroup --name MyApp --settings AWS_ACCESS_KEY_ID=YOUR_ACTUAL_ACCESS_KEY_ID
az webapp config appsettings set --resource-group MyResourceGroup --name MyApp --settings AWS_SECRET_ACCESS_KEY=YOUR_ACTUAL_SECRET_ACCESS_KEY
```

### AWS Elastic Beanstalk
Add to `.ebextensions/environment.config`:
```yaml
option_settings:
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: AWS_ACCESS_KEY_ID
    value: YOUR_ACTUAL_ACCESS_KEY_ID
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: AWS_SECRET_ACCESS_KEY
    value: YOUR_ACTUAL_SECRET_ACCESS_KEY
```

### Docker
```bash
docker run -e AWS_ACCESS_KEY_ID=YOUR_KEY \
           -e AWS_SECRET_ACCESS_KEY=YOUR_SECRET \
           -e AWS_BUCKET_NAME=amzn-s3-wihngo-bucket \
           -e AWS_REGION=us-east-1 \
           your-image
```

Or use `.env` file:
```bash
docker run --env-file .env your-image
```

---

## ?? Verification

### Check Environment Variables are Loaded

After starting your application, check the logs:

```
AWS Configuration loaded:
  Access Key: ***KEYID (last 4 characters shown)
  Secret Key: ***configured***
  Bucket: amzn-s3-wihngo-bucket
  Region: us-east-1
```

**If you see:**
```
Access Key: NOT SET
Secret Key: NOT SET
```

Then the environment variables are not set correctly!

---

### Test API Endpoint

```bash
curl -X POST http://localhost:5000/api/media/upload-url \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "mediaType": "profile-image",
    "fileExtension": ".jpg"
  }'
```

**Expected Response:**
```json
{
  "uploadUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "s3Key": "users/profile-images/.../uuid.jpg",
  "expiresAt": "2024-01-15T10:40:00Z"
}
```

---

## ?? Security Best Practices

### ? DO:
- Use environment variables for credentials
- Use User Secrets for local development
- Use platform-specific secret managers in production
- Rotate access keys regularly
- Use IAM roles when running on AWS infrastructure

### ? DON'T:
- Commit credentials to Git
- Share credentials in plain text
- Use the same credentials across environments
- Give credentials more permissions than needed
- Hard-code credentials in source code

---

## ?? .gitignore Recommendations

Add these to your `.gitignore`:

```gitignore
# Environment files
.env
.env.local
.env.*.local
*.env

# VS launch settings with secrets
Properties/launchSettings.json

# User-specific files
*.user
*.userosscache
*.suo
```

---

## ?? Troubleshooting

### Issue: "AWS credentials are not configured"

**Cause:** Environment variables are not set

**Solution:**
1. Set environment variables using one of the methods above
2. Restart your application (and Visual Studio if using IDE)
3. Check logs for "AWS Configuration loaded"

### Issue: "InvalidAccessKeyId"

**Cause:** Wrong access key or not set

**Solution:**
1. Verify you copied the correct Access Key ID from AWS
2. Make sure it starts with `AKIA`
3. Check for extra spaces or quotes

### Issue: Environment variables work in terminal but not in Visual Studio

**Cause:** Visual Studio doesn't reload environment variables

**Solution:**
1. Close Visual Studio completely
2. Reopen Visual Studio
3. Or use `launchSettings.json` method instead

### Issue: Works locally but fails in Render

**Cause:** Environment variables not set in Render

**Solution:**
1. Go to Render Dashboard ? Environment
2. Add all AWS_* variables
3. Click "Save Changes" (triggers redeploy)

---

## ?? Quick Start Checklist

- [ ] Get AWS credentials from IAM Console
- [ ] Choose configuration method (User Secrets recommended)
- [ ] Set all required environment variables
- [ ] Restart application
- [ ] Check startup logs for AWS configuration
- [ ] Test upload-url endpoint
- [ ] Add `.env` and `launchSettings.json` to `.gitignore`
- [ ] Set up production environment variables on hosting platform

---

## ?? Need Help?

### Common Commands

**Check if environment variable is set (PowerShell):**
```powershell
$env:AWS_ACCESS_KEY_ID
```

**Check if environment variable is set (CMD):**
```cmd
echo %AWS_ACCESS_KEY_ID%
```

**List all AWS environment variables (PowerShell):**
```powershell
Get-ChildItem Env: | Where-Object { $_.Name -like "AWS*" }
```

**Remove environment variable (PowerShell):**
```powershell
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', $null, 'User')
```

---

## ?? Additional Resources

- **AWS IAM Best Practices:** https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html
- **ASP.NET Configuration:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
- **User Secrets:** https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets

---

**Success! Your AWS credentials are now securely managed via environment variables.** ??
