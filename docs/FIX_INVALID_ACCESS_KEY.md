# ?? URGENT: Invalid AWS Access Key Error

## ? Current Error

```
InvalidAccessKeyId: The AWS Access Key Id you provided does not exist in our records.
AWSAccessKeyId: YOUR_AWS_ACCESS_KEY_ID
```

**This means you haven't replaced the placeholder credentials with your actual AWS credentials!**

---

## ? Step-by-Step Fix

### Step 1: Get Your AWS Credentials

#### Option A: AWS Console (Recommended)

1. **Log in to AWS Console**
   - Go to: https://console.aws.amazon.com
   - Sign in with your AWS account

2. **Navigate to IAM**
   - In the search bar at the top, type: `IAM`
   - Click on **"IAM"** service

3. **Go to Users**
   - Click **"Users"** in the left sidebar
   - Find and click on: **`wihngo-media-signer`**

4. **Create Access Key**
   - Click on **"Security credentials"** tab
   - Scroll down to **"Access keys"** section
   - Click **"Create access key"**

5. **Choose Use Case**
   - Select: **"Application running outside AWS"**
   - Click **"Next"**

6. **Add Description (Optional)**
   - Description: `Wihngo Media Upload Service`
   - Click **"Create access key"**

7. **?? SAVE YOUR CREDENTIALS IMMEDIATELY!**
   ```
   Access key ID: AKIA... (starts with AKIA)
   Secret access key: abc123... (long random string)
   ```
   
   **CRITICAL:** Copy both values NOW! The secret key is only shown once!

---

### Step 2: Update Your appsettings.json

**Current (WRONG):**
```json
"AWS": {
  "AccessKeyId": "YOUR_AWS_ACCESS_KEY_ID",  // ?
  "SecretAccessKey": "YOUR_AWS_SECRET_ACCESS_KEY",  // ?
  "BucketName": "amzn-s3-wihngo-bucket",
  "Region": "us-east-1",
  "PresignedUrlExpirationMinutes": 10
}
```

**Updated (CORRECT):**
```json
"AWS": {
  "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",  // ? Your actual key
  "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",  // ? Your actual secret
  "BucketName": "amzn-s3-wihngo-bucket",
  "Region": "us-east-1",
  "PresignedUrlExpirationMinutes": 10
}
```

**Replace with YOUR actual credentials from Step 1!**

---

### Step 3: Verify IAM User Has Correct Permissions

1. **Go back to IAM ? Users ? `wihngo-media-signer`**
2. **Click "Permissions" tab**
3. **Verify the user has this policy:**

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

**If the policy doesn't exist:**
1. Click **"Add permissions"** ? **"Create inline policy"**
2. Click **"JSON"** tab
3. Paste the policy above
4. Name it: `WihngoS3MediaAccess`
5. Click **"Create policy"**

---

### Step 4: Restart Your Application

After updating `appsettings.json`:

```bash
# Stop the application (Ctrl+C or Shift+F5 in VS)
# Then restart it (F5 in Visual Studio or dotnet run)
```

---

## ?? Verification Steps

### Test 1: Check Startup Logs

After restarting, you should see:
```
? S3Service initialized for bucket amzn-s3-wihngo-bucket in region us-east-1
```

**If you see:**
```
? AWS credentials are not configured
```
Then your credentials are still wrong!

---

### Test 2: Test API Endpoint

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

**If you still get 403:** Your credentials are invalid or have wrong permissions.

---

## ?? Security Best Practices

### For Development (Local)

Use **User Secrets** instead of `appsettings.json`:

```bash
cd C:\.net\Wihngo

dotnet user-secrets set "AWS:AccessKeyId" "YOUR_ACTUAL_ACCESS_KEY_ID"
dotnet user-secrets set "AWS:SecretAccessKey" "YOUR_ACTUAL_SECRET_ACCESS_KEY"
```

Then **remove the AWS section from appsettings.json**!

---

### For Production (Render/Heroku)

**Use Environment Variables:**

In Render Dashboard:
1. Go to your service
2. Click **"Environment"** tab
3. Add:
   ```
   AWS__AccessKeyId = YOUR_ACTUAL_ACCESS_KEY_ID
   AWS__SecretAccessKey = YOUR_ACTUAL_SECRET_ACCESS_KEY
   AWS__BucketName = amzn-s3-wihngo-bucket
   AWS__Region = us-east-1
   ```

**Never commit real credentials to Git!**

---

## ?? Quick Checklist

- [ ] Created/Retrieved access key from AWS IAM Console
- [ ] Copied both Access Key ID and Secret Access Key
- [ ] Updated `appsettings.json` with real credentials
- [ ] Verified IAM user has S3 permissions
- [ ] Restarted the application
- [ ] Checked startup logs for S3Service initialization
- [ ] Tested the upload-url endpoint
- [ ] Configured CORS on S3 bucket (from previous guide)

---

## ?? Common Mistakes

### ? Mistake 1: Still Using Placeholders
```json
"AccessKeyId": "YOUR_AWS_ACCESS_KEY_ID"  // This is NOT a real key!
```

### ? Mistake 2: Wrong Key Format
- Access Key ID should start with `AKIA`
- If yours doesn't, you copied the wrong value

### ? Mistake 3: Spaces or Quotes in Credentials
```json
"AccessKeyId": " AKIA123... "  // ? Extra spaces
"AccessKeyId": "\"AKIA123...\""  // ? Extra quotes
```

### ? Correct Format
```json
"AccessKeyId": "AKIA123..."  // ? No spaces, no extra quotes
```

---

## ?? Troubleshooting

### Issue: "Access Key ID does not exist"
**Solution:** You need to create a NEW access key in IAM Console

### Issue: "Invalid access key format"
**Solution:** Make sure you copied the full key without spaces

### Issue: "Access Denied"
**Solution:** Check IAM permissions (Step 3 above)

### Issue: "Bucket does not exist"
**Solution:** 
1. Verify bucket name: `amzn-s3-wihngo-bucket`
2. Verify region: `us-east-1`
3. Create the bucket if it doesn't exist

---

## ?? Still Having Issues?

### Check This:

1. **Credentials are correct:**
   ```bash
   # Test with AWS CLI
   aws configure
   # Enter your credentials
   
   aws s3 ls s3://amzn-s3-wihngo-bucket
   # Should list bucket contents (or empty)
   ```

2. **IAM User exists:**
   - Username: `wihngo-media-signer`
   - Has S3 permissions
   - Access key is active (not deleted)

3. **Bucket exists:**
   - Name: `amzn-s3-wihngo-bucket`
   - Region: `us-east-1`
   - Your IAM user has access

---

## ? Quick Commands

### Get AWS Account ID
```bash
aws sts get-caller-identity
```

### Test S3 Access
```bash
aws s3 ls s3://amzn-s3-wihngo-bucket --region us-east-1
```

### Create Bucket (if doesn't exist)
```bash
aws s3 mb s3://amzn-s3-wihngo-bucket --region us-east-1
```

---

## ?? Expected Result

After fixing:

**Before:**
```
? InvalidAccessKeyId: YOUR_AWS_ACCESS_KEY_ID does not exist
```

**After:**
```
? S3Service initialized for bucket amzn-s3-wihngo-bucket
? Generated upload URL for user...
? Upload successful!
```

---

**DO THIS NOW:**
1. Stop reading
2. Go to AWS Console
3. Create/Get access key
4. Update appsettings.json
5. Restart app
6. Test again

Good luck! ??
