# ?? COPY & PASTE: IAM Policy for S3 Access

## ? Quick Fix - 5 Minutes

Your IAM user `wihngo-media-signer` needs permission to upload files to S3.

---

## ?? Step-by-Step Instructions

### 1. Open AWS IAM Console

**URL:** https://console.aws.amazon.com/iam

Or:
- Go to: https://console.aws.amazon.com
- Search: `IAM`
- Click: IAM service

---

### 2. Navigate to Your User

1. Left sidebar ? Click **"Users"**
2. Find in list ? Click **`wihngo-media-signer`**

---

### 3. Add Inline Policy

1. Click **"Permissions"** tab
2. Click **"Add permissions"** button (dropdown)
3. Select **"Create inline policy"**

---

### 4. Paste Policy JSON

1. Click **"JSON"** tab at the top
2. **Select all existing text and delete it**
3. **Paste this:**

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "WihngoS3MediaAccess",
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

4. Click **"Next"** or **"Review policy"**

---

### 5. Name and Create

1. Policy name: `WihngoS3MediaAccess`
2. Click **"Create policy"**

---

## ? Verify It Worked

### In IAM Console:

Go back to: **IAM ? Users ? wihngo-media-signer ? Permissions**

You should see:
```
Permissions policies (1)
?? WihngoS3MediaAccess (inline policy) ?
```

---

### Test Upload:

1. **Open mobile app** (no need to restart anything!)
2. **Pick an image**
3. **Upload**
4. Should now get: **? 200 OK**

---

## ?? What This Policy Does

```
? Allows: Upload files to amzn-s3-wihngo-bucket
? Allows: Download files from amzn-s3-wihngo-bucket
? Allows: Delete files from amzn-s3-wihngo-bucket
? Allows: Check if files exist
? Allows: List bucket contents

? Blocks: Access to other buckets
? Blocks: Delete the entire bucket
? Blocks: Change bucket settings
```

---

## ?? Common Issues

### Can't Find User

**Symptom:** User `wihngo-media-signer` not in list

**Solution:**
1. Make sure you're in the correct AWS account (ID: 127214184914)
2. Check if user has a different name
3. Search by Access Key: `YOUR_AWS_ACCESS_KEY_ID`

---

### Policy Creation Failed

**Symptom:** Error when clicking "Create policy"

**Solution:**
1. Make sure you copied the **entire** JSON (including `{` and `}`)
2. Check bucket name is exactly: `amzn-s3-wihngo-bucket`
3. Don't modify the policy - paste exactly as shown

---

### Still Getting 403 After Adding Policy

**Symptom:** AccessDenied error continues

**Solution:**
1. **Wait 30 seconds** (IAM changes take a moment to propagate)
2. **Refresh mobile app** and try again
3. **Verify policy is attached:**
   - IAM ? Users ? wihngo-media-signer ? Permissions
   - Should see `WihngoS3MediaAccess` listed

---

## ?? Before & After

### Before (Current):
```xml
<Code>AccessDenied</Code>
<Message>User is not authorized to perform: s3:PutObject</Message>
```

### After (Success):
```
? S3 Response Status: 200
? Upload successful!
```

---

## ?? Visual Checklist

When done, your IAM user should have:

```
wihngo-media-signer
?? Access Keys
?  ?? YOUR_AWS_ACCESS_KEY_ID (Active) ?
?? Permissions
   ?? WihngoS3MediaAccess (inline) ?
      ?? s3:PutObject ?
      ?? s3:GetObject ?
      ?? s3:DeleteObject ?
      ?? s3:GetObjectMetadata ?
      ?? s3:ListBucket ?
```

---

## ? Quick Test Command

After adding policy, test with AWS CLI:

```bash
# This should now work without errors
aws s3 ls s3://amzn-s3-wihngo-bucket --profile wihngo

# Try to upload a test file
echo "test" > test.txt
aws s3 cp test.txt s3://amzn-s3-wihngo-bucket/test.txt --profile wihngo
```

---

## ?? Success Indicators

When it's working:

1. **IAM Console** shows policy attached
2. **Mobile app logs** show: `?? S3 Response Status: 200`
3. **Backend logs** show: `Generated upload URL`
4. **Profile image** updates successfully
5. **No 403 errors** in mobile app

---

**Add this policy and you're done! Takes 2 minutes!** ??

**Remember:** No backend restart needed - test immediately!
