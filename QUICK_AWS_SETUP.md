# ? Quick Setup: AWS Environment Variables

## For Windows/PowerShell Users

### Step 1: Get Your AWS Credentials

1. Go to AWS IAM Console: https://console.aws.amazon.com
2. Navigate to: IAM ? Users ? `wihngo-media-signer`
3. Security credentials ? Create access key
4. Copy both values immediately!

### Step 2: Set Environment Variables (PowerShell)

Open PowerShell **as Administrator** and run:

```powershell
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'PASTE_YOUR_ACCESS_KEY_HERE', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_SECRET_ACCESS_KEY', 'PASTE_YOUR_SECRET_KEY_HERE', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_BUCKET_NAME', 'amzn-s3-wihngo-bucket', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_REGION', 'us-east-1', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_PRESIGNED_URL_EXPIRATION_MINUTES', '10', 'User')
```

**Example (with fake credentials):**
```powershell
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'AKIAIOSFODNN7EXAMPLE', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_SECRET_ACCESS_KEY', 'wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_BUCKET_NAME', 'amzn-s3-wihngo-bucket', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_REGION', 'us-east-1', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_PRESIGNED_URL_EXPIRATION_MINUTES', '10', 'User')
```

### Step 3: Restart Visual Studio

**IMPORTANT:** Close and reopen Visual Studio for it to pick up the new environment variables!

### Step 4: Run Your Application

Press F5 in Visual Studio

### Step 5: Verify

Check the console output for:
```
AWS Configuration loaded:
  Access Key: ***KEYID
  Secret Key: ***configured***
  Bucket: amzn-s3-wihngo-bucket
  Region: us-east-1
```

---

## ? Verification Commands

### Check if variables are set:
```powershell
$env:AWS_ACCESS_KEY_ID
$env:AWS_SECRET_ACCESS_KEY
```

### List all AWS variables:
```powershell
Get-ChildItem Env: | Where-Object { $_.Name -like "AWS*" }
```

---

## ?? For Render Deployment

1. Go to Render Dashboard: https://dashboard.render.com
2. Select your service
3. Click "Environment" tab
4. Add these variables:

```
AWS_ACCESS_KEY_ID = YOUR_ACTUAL_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY = YOUR_ACTUAL_SECRET_ACCESS_KEY
AWS_BUCKET_NAME = amzn-s3-wihngo-bucket
AWS_REGION = us-east-1
AWS_PRESIGNED_URL_EXPIRATION_MINUTES = 10
```

5. Click "Save Changes"

---

## ?? Troubleshooting

### Issue: Still showing "NOT SET" in logs

**Solution:** 
1. Make sure you ran PowerShell **as Administrator**
2. Restart Visual Studio completely
3. Try verifying with: `$env:AWS_ACCESS_KEY_ID`

### Issue: "InvalidAccessKeyId"

**Solution:**
1. Make sure Access Key starts with `AKIA`
2. Check for extra spaces or quotes
3. Create a new access key if needed

---

## ?? Full Documentation

See `AWS_ENVIRONMENT_VARIABLES_GUIDE.md` for:
- Alternative setup methods (User Secrets, launchSettings.json)
- Other hosting platforms (Heroku, Azure, etc.)
- Security best practices
- Detailed troubleshooting

---

**That's it! Your AWS credentials are now securely configured.** ??

**Next steps:**
1. Test the upload endpoint
2. Configure CORS on S3 bucket (see `MOBILE_S3_UPLOAD_FIX.md`)
3. Test with mobile app
