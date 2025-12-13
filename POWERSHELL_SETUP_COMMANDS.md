# ?? COPY & PASTE: PowerShell Setup Commands

## Step 1: Open PowerShell as Administrator

1. Press `Windows Key`
2. Type: `PowerShell`
3. Right-click "Windows PowerShell"
4. Click "Run as Administrator"

---

## Step 2: Copy & Paste These Commands

**?? REPLACE the values in quotes with your actual AWS credentials!**

```powershell
# Set AWS Access Key ID (replace with your actual key from AWS IAM)
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'AKIA...YOUR_ACTUAL_KEY...', 'User')

# Set AWS Secret Access Key (replace with your actual secret from AWS IAM)
[System.Environment]::SetEnvironmentVariable('AWS_SECRET_ACCESS_KEY', 'your-actual-secret-key-here', 'User')

# Set AWS Bucket Name (this is correct, don't change)
[System.Environment]::SetEnvironmentVariable('AWS_BUCKET_NAME', 'amzn-s3-wihngo-bucket', 'User')

# Set AWS Region (this is correct, don't change)
[System.Environment]::SetEnvironmentVariable('AWS_REGION', 'us-east-1', 'User')

# Set pre-signed URL expiration (this is correct, don't change)
[System.Environment]::SetEnvironmentVariable('AWS_PRESIGNED_URL_EXPIRATION_MINUTES', '10', 'User')
```

---

## Step 3: Verify Variables are Set

Run this command to check:

```powershell
Get-ChildItem Env: | Where-Object { $_.Name -like "AWS*" } | Format-Table Name, Value
```

**You should see:**
```
Name                                    Value
----                                    -----
AWS_ACCESS_KEY_ID                       AKIA...
AWS_SECRET_ACCESS_KEY                   your-secret...
AWS_BUCKET_NAME                         amzn-s3-wihngo-bucket
AWS_REGION                              us-east-1
AWS_PRESIGNED_URL_EXPIRATION_MINUTES    10
```

---

## Step 4: Restart Visual Studio

**CRITICAL:** Close Visual Studio completely and reopen it!

---

## Step 5: Run Your Application

Press `F5` in Visual Studio

---

## Step 6: Check the Logs

Look for this in the console output:

```
AWS Configuration loaded:
  Access Key: ***XXXX  ? Should show last 4 characters of your key
  Secret Key: ***configured***  ? Should say "configured"
  Bucket: amzn-s3-wihngo-bucket
  Region: us-east-1
```

**If you see:**
```
Access Key: NOT SET  ? Something is wrong!
```

Then the environment variables didn't load. Try:
1. Make sure PowerShell was run as Administrator
2. Restart your computer (as a last resort)
3. Use User Secrets instead (see `AWS_ENVIRONMENT_VARIABLES_GUIDE.md`)

---

## ?? Common Mistakes

### ? Wrong: Missing quotes
```powershell
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', AKIAEXAMPLE, 'User')
```

### ? Correct: With quotes
```powershell
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'AKIAEXAMPLE', 'User')
```

### ? Wrong: Extra spaces
```powershell
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', ' AKIAEXAMPLE ', 'User')
```

### ? Correct: No extra spaces
```powershell
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'AKIAEXAMPLE', 'User')
```

---

## ?? To Remove/Update a Variable

```powershell
# Remove
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', $null, 'User')

# Update (just set it again with new value)
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'NEW_VALUE', 'User')
```

---

## ?? Complete Setup Checklist

- [ ] Opened PowerShell as Administrator
- [ ] Got actual AWS credentials from IAM Console
- [ ] Pasted all 5 commands with YOUR credentials
- [ ] Ran verification command - saw all variables
- [ ] Closed Visual Studio completely
- [ ] Reopened Visual Studio
- [ ] Started application (F5)
- [ ] Saw "AWS Configuration loaded" in logs
- [ ] Access Key shows last 4 characters (not "NOT SET")
- [ ] Secret Key shows "***configured***"

---

## ? Success Indicators

**When it's working, you'll see:**

1. **In PowerShell verification:**
   ```
   AWS_ACCESS_KEY_ID          AKIA1234EXAMPLE5678
   AWS_SECRET_ACCESS_KEY      abc123secretkey456
   ```

2. **In application logs:**
   ```
   AWS Configuration loaded:
     Access Key: ***5678
     Secret Key: ***configured***
   ```

3. **In API response:**
   ```json
   {
     "uploadUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
     "s3Key": "users/profile-images/.../uuid.jpg"
   }
   ```

---

## ?? Still Not Working?

### Try User Secrets Instead

In a regular terminal (not PowerShell):
```bash
cd C:\.net\Wihngo
dotnet user-secrets set "AWS_ACCESS_KEY_ID" "YOUR_ACTUAL_KEY"
dotnet user-secrets set "AWS_SECRET_ACCESS_KEY" "YOUR_ACTUAL_SECRET"
```

This works even without Administrator rights!

---

**That's it! Your environment variables are now set.** ??

**Next:** Configure CORS on S3 bucket (see `MOBILE_S3_UPLOAD_FIX.md`)
