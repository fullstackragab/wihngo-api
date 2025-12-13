# ?? Fix IAM Permissions: AccessDenied Error

## ? Progress Update

**Great news!** The `SignatureDoesNotMatch` error is fixed. Now we just need to add IAM permissions.

### Current Error:
```xml
<Code>AccessDenied</Code>
<Message>User: arn:aws:iam::127214184914:user/wihngo-media-signer 
is not authorized to perform: s3:PutObject on resource: 
"arn:aws:s3:::amzn-s3-wihngo-bucket/..." 
because no identity-based policy allows the s3:PutObject action</Message>
```

---

## ?? How to Fix (AWS IAM Console)

### Step 1: Go to IAM Console

1. Open AWS Console: https://console.aws.amazon.com
2. Search for: **IAM**
3. Click on **"IAM"** service

### Step 2: Find Your User

1. Click **"Users"** in left sidebar
2. Find and click: **`wihngo-media-signer`**

### Step 3: Add Inline Policy

1. Click **"Permissions"** tab
2. Click **"Add permissions"** dropdown
3. Select **"Create inline policy"**

### Step 4: Use JSON Editor

1. Click **"JSON"** tab
2. **Delete any existing JSON**
3. Paste this policy:

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

### Step 5: Create Policy

1. Click **"Review policy"**
2. Policy name: `WihngoS3MediaAccess`
3. Click **"Create policy"**

---

## ? Verify Permissions Were Added

### In IAM Console:

1. Go back to: IAM ? Users ? `wihngo-media-signer`
2. Click **"Permissions"** tab
3. You should see: **`WihngoS3MediaAccess`** policy listed

### Test the Upload:

1. **Don't restart backend** (not needed for IAM changes)
2. **Test from mobile app** immediately
3. Should now get **200 OK** instead of 403!

---

## ?? What Each Permission Does

| Permission | Purpose |
|------------|---------|
| `s3:PutObject` | Upload files to S3 ? (This fixes your error!) |
| `s3:GetObject` | Download files from S3 |
| `s3:DeleteObject` | Delete files from S3 |
| `s3:GetObjectMetadata` | Check if files exist |
| `s3:ListBucket` | List bucket contents |

---

## ?? Expected Result After Fix

### Before (Current Error):
```
? AccessDenied
? User is not authorized to perform: s3:PutObject
```

### After Adding Policy:
```
? S3 Response Status: 200
? Upload successful!
? Profile image updated!
```

---

## ?? Visual Guide

### Your IAM Console Should Look Like This:

```
IAM ? Users ? wihngo-media-signer
?? Permissions tab
?  ?? Permissions policies
?  ?  ?? WihngoS3MediaAccess (inline policy) ?
?  ?? Permissions boundary: Not set
?? Security credentials tab
?  ?? Access keys
?     ?? YOUR_AWS_ACCESS_KEY_ID (Active) ?
?? Groups tab
```

---

## ?? Troubleshooting

### Issue: Can't find IAM service

**Solution:** 
- Search bar at top of AWS Console
- Type: `IAM`
- Click "IAM" with the key icon

### Issue: Can't find user `wihngo-media-signer`

**Solution:**
- The user might be named differently
- Check Users list in IAM
- Look for the user with Access Key: `YOUR_AWS_ACCESS_KEY_ID`

### Issue: Policy creation fails

**Solution:**
- Make sure you're on the JSON tab
- Copy the entire policy (including curly braces)
- Don't modify the policy text

### Issue: Still getting AccessDenied after adding policy

**Solution:**
1. Wait 30 seconds (IAM propagation time)
2. Verify policy is attached to correct user
3. Check bucket name is correct: `amzn-s3-wihngo-bucket`

---

## ?? Policy Breakdown

### The Resource ARNs:

```json
"Resource": [
  "arn:aws:s3:::amzn-s3-wihngo-bucket",      // Bucket itself (for ListBucket)
  "arn:aws:s3:::amzn-s3-wihngo-bucket/*"     // All objects in bucket
]
```

**Both are required!**
- First line: Permission to list the bucket
- Second line: Permission to upload/download/delete objects

---

## ?? Security Notes

### This Policy:
? Allows access to only ONE bucket  
? Allows only necessary S3 operations  
? Follows principle of least privilege  
? Safe for production use  

### NOT Allowed (and that's good):
? Delete the entire bucket  
? Access other S3 buckets  
? Change bucket permissions  
? Access other AWS services  

---

## ?? Alternative: AWS CLI Method

If you prefer command line:

```bash
# Create policy file
cat > policy.json << 'EOF'
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
EOF

# Attach policy to user
aws iam put-user-policy \
  --user-name wihngo-media-signer \
  --policy-name WihngoS3MediaAccess \
  --policy-document file://policy.json
```

---

## ? Quick Checklist

- [ ] Opened AWS IAM Console
- [ ] Found user: `wihngo-media-signer`
- [ ] Clicked "Add permissions" ? "Create inline policy"
- [ ] Selected JSON tab
- [ ] Pasted the policy above
- [ ] Named it: `WihngoS3MediaAccess`
- [ ] Created the policy
- [ ] Verified policy appears under Permissions tab
- [ ] Tested upload from mobile app
- [ ] Got 200 OK response!

---

## ?? After Adding Permissions

### Test Immediately (No Backend Restart Needed!)

1. **Open mobile app**
2. **Pick an image**
3. **Upload profile image**
4. **Watch for:**

```
?? Uploading to S3...
?? S3 Response Status: 200  ? Success!
? Upload successful!
? Profile image updated!
```

---

## ?? Need More Help?

### Verify Your AWS Account ID:
```
arn:aws:iam::127214184914:user/wihngo-media-signer
                ?
           Your Account ID
```

### Check Existing Policies:
```bash
aws iam list-user-policies --user-name wihngo-media-signer
aws iam get-user-policy --user-name wihngo-media-signer --policy-name WihngoS3MediaAccess
```

---

**This is the LAST step! Add the IAM policy and uploads will work!** ??

**No backend restart needed - IAM changes take effect immediately!**
