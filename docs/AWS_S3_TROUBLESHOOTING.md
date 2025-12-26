# AWS S3 Configuration & Troubleshooting Guide

## ?? Quick Fix for NullReferenceException

The error you're seeing:
```
System.NullReferenceException: Object reference not set to an instance of an object.
at Amazon.S3.AmazonS3Client.GetPreSignedURLInternalAsync
```

This happens because AWS credentials are missing or invalid.

---

## ? Step 1: Add AWS Configuration

### Option A: Using appsettings.json (Development)

I've already added the AWS section to your `appsettings.json`. Now you need to replace the placeholder values:

```json
"AWS": {
  "AccessKeyId": "YOUR_ACTUAL_AWS_ACCESS_KEY_ID",
  "SecretAccessKey": "YOUR_ACTUAL_AWS_SECRET_ACCESS_KEY",
  "BucketName": "amzn-s3-wihngo-bucket",
  "Region": "us-east-1",
  "PresignedUrlExpirationMinutes": 10
}
```

### Option B: Using Environment Variables (Production - Recommended)

Set these environment variables on your server:

```bash
AWS__AccessKeyId=YOUR_ACTUAL_AWS_ACCESS_KEY_ID
AWS__SecretAccessKey=YOUR_ACTUAL_AWS_SECRET_ACCESS_KEY
AWS__BucketName=amzn-s3-wihngo-bucket
AWS__Region=us-east-1
AWS__PresignedUrlExpirationMinutes=10
```

**Note:** Use double underscores `__` for nested configuration in environment variables.

### Option C: Using User Secrets (Development - Most Secure)

In Visual Studio, right-click your project ? **Manage User Secrets**

Add this to `secrets.json`:
```json
{
  "AWS": {
    "AccessKeyId": "YOUR_ACTUAL_AWS_ACCESS_KEY_ID",
    "SecretAccessKey": "YOUR_ACTUAL_AWS_SECRET_ACCESS_KEY"
  }
}
```

Or use the command line:
```bash
cd C:\.net\Wihngo

dotnet user-secrets set "AWS:AccessKeyId" "YOUR_ACTUAL_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AWS:SecretAccessKey" "YOUR_ACTUAL_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AWS:BucketName" "amzn-s3-wihngo-bucket"
dotnet user-secrets set "AWS:Region" "us-east-1"
```

---

## ?? Step 2: Get Your AWS Credentials

### From AWS Console:

1. **Log in to AWS Console:** https://console.aws.amazon.com
2. **Go to IAM:** Search for "IAM" in the top search bar
3. **Click "Users"** in the left sidebar
4. **Find your user:** `wihngo-media-signer`
5. **Click on the user**
6. **Go to "Security credentials" tab**
7. **Click "Create access key"**
8. **Choose "Application running outside AWS"**
9. **Copy both:**
   - Access key ID
   - Secret access key (only shown once!)

?? **IMPORTANT:** Save the secret access key immediately. You can't view it again!

---

## ?? Step 3: Verify S3 Bucket Exists

### Check Bucket Name:
```bash
# Your bucket name from requirements:
amzn-s3-wihngo-bucket
```

### Verify in AWS Console:
1. Go to S3 Console: https://s3.console.aws.amazon.com
2. Search for: `amzn-s3-wihngo-bucket`
3. Verify the bucket exists in region: `us-east-1`

---

## ?? Step 4: Set IAM Permissions

Your IAM user `wihngo-media-signer` needs these permissions:

### Required IAM Policy:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:GetObjectMetadata",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::amzn-s3-wihngo-bucket",
        "arn:aws:s3:::amzn-s3-wihngo-bucket/*"
      ]
    }
  ]
}
```

### How to Attach Policy:
1. Go to IAM ? Users ? `wihngo-media-signer`
2. Click **"Add permissions"** ? **"Create inline policy"**
3. Click **"JSON"** tab
4. Paste the policy above
5. Click **"Review policy"**
6. Name it: `WihngoS3MediaPolicy`
7. Click **"Create policy"**

---

## ?? Step 5: Test Your Configuration

### Test 1: Check Configuration Loading

Add this temporary code to `Program.cs` after `var app = builder.Build();`:

```csharp
// TEST: Verify AWS configuration is loaded
var awsConfig = app.Services.GetRequiredService<IOptions<AwsConfiguration>>().Value;
Console.WriteLine("=== AWS CONFIGURATION TEST ===");
Console.WriteLine($"Access Key ID: {(string.IsNullOrEmpty(awsConfig.AccessKeyId) ? "MISSING" : "***" + awsConfig.AccessKeyId[^4..])}");
Console.WriteLine($"Secret Access Key: {(string.IsNullOrEmpty(awsConfig.SecretAccessKey) ? "MISSING" : "***configured***")}");
Console.WriteLine($"Bucket Name: {awsConfig.BucketName}");
Console.WriteLine($"Region: {awsConfig.Region}");
Console.WriteLine($"URL Expiration: {awsConfig.PresignedUrlExpirationMinutes} minutes");
Console.WriteLine("==============================");
```

### Test 2: Test S3 Connection

Create a test endpoint in `MediaController.cs`:

```csharp
#if DEBUG
[HttpGet("test-s3-connection")]
public async Task<IActionResult> TestS3Connection()
{
    try
    {
        var testKey = "test/connection-test.txt";
        var exists = await _s3Service.FileExistsAsync(testKey);
        
        return Ok(new
        {
            success = true,
            message = "S3 connection successful",
            bucketAccessible = true,
            testKey = testKey,
            fileExists = exists
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "S3 connection failed",
            error = ex.Message,
            stackTrace = ex.StackTrace
        });
    }
}
#endif
```

### Test 3: Make a Test Request

```bash
# After configuring AWS credentials and restarting the app:
curl -X GET http://localhost:5000/api/media/test-s3-connection \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## ?? Troubleshooting Common Issues

### Issue 1: "Access Denied" Error
**Cause:** IAM user doesn't have required permissions  
**Fix:** Attach the IAM policy from Step 4

### Issue 2: "Bucket does not exist"
**Cause:** Bucket name is incorrect or in wrong region  
**Fix:** 
- Verify bucket name: `amzn-s3-wihngo-bucket`
- Verify region: `us-east-1`
- Create bucket if it doesn't exist

### Issue 3: "The AWS Access Key Id you provided does not exist"
**Cause:** Access Key ID is incorrect or was deleted  
**Fix:** Generate new access key in IAM console

### Issue 4: "SignatureDoesNotMatch"
**Cause:** Secret Access Key is incorrect  
**Fix:** Generate new access key pair

### Issue 5: Configuration Not Loading
**Cause:** Wrong configuration path or missing section  
**Fix:** Ensure the AWS section exists in `appsettings.json`

---

## ?? Configuration Checklist

- [ ] AWS section added to `appsettings.json`
- [ ] Access Key ID replaced (not "YOUR_AWS_ACCESS_KEY_ID")
- [ ] Secret Access Key replaced (not "YOUR_AWS_SECRET_ACCESS_KEY")
- [ ] Bucket name is correct: `amzn-s3-wihngo-bucket`
- [ ] Region is correct: `us-east-1`
- [ ] IAM user has required S3 permissions
- [ ] Bucket exists in AWS console
- [ ] Application restarted after configuration changes
- [ ] Test endpoint returns success

---

## ?? For Render Deployment

### Set Environment Variables in Render:

1. Go to your Render dashboard
2. Select your service
3. Go to **"Environment"** tab
4. Add these variables:

```
AWS__AccessKeyId = your-access-key-id
AWS__SecretAccessKey = your-secret-access-key
AWS__BucketName = amzn-s3-wihngo-bucket
AWS__Region = us-east-1
AWS__PresignedUrlExpirationMinutes = 10
```

5. Click **"Save Changes"**
6. Render will automatically redeploy

---

## ?? Quick Start (Local Development)

1. **Get AWS Credentials:**
   ```bash
   # From your AWS IAM user: wihngo-media-signer
   Access Key ID: AKIA...
   Secret Access Key: abc123...
   ```

2. **Update appsettings.json:**
   ```json
   "AWS": {
     "AccessKeyId": "AKIA...",
     "SecretAccessKey": "abc123...",
     "BucketName": "amzn-s3-wihngo-bucket",
     "Region": "us-east-1",
     "PresignedUrlExpirationMinutes": 10
   }
   ```

3. **Restart Application:**
   ```bash
   # Stop the debugger (Shift+F5)
   # Start again (F5)
   ```

4. **Test Upload Endpoint:**
   ```bash
   curl -X POST http://localhost:5000/api/media/upload-url \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "mediaType": "profile-image",
       "fileExtension": ".jpg"
     }'
   ```

---

## ?? Still Having Issues?

If you're still seeing the `NullReferenceException` after following these steps:

1. **Check the startup logs** - Look for the AWS configuration test output
2. **Verify credentials format** - No quotes, spaces, or special characters
3. **Check IAM permissions** - User must have s3:PutObject, s3:GetObject, etc.
4. **Try a different region** - If bucket is in a different region, update config
5. **Create access key again** - Old keys may have been revoked

---

## ?? Additional Resources

- **AWS S3 Documentation:** https://docs.aws.amazon.com/s3/
- **AWS IAM Best Practices:** https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html
- **ASP.NET Configuration:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/

---

## ? Success Indicators

You'll know it's working when:
- ? No `NullReferenceException` in logs
- ? Test endpoint returns `{ "success": true }`
- ? Upload URL generation returns a pre-signed URL
- ? Logs show: "Generated upload URL for user..."

---

**Need Help?** Check the error logs for specific AWS error messages. They usually indicate exactly what's wrong!
