# ? Quick Setup: SendGrid Email Integration

## ?? 5-Minute Setup

### Step 1: Get SendGrid API Key (2 minutes)

1. **Log in:** https://app.sendgrid.com
2. **Go to:** Settings ? API Keys
3. **Click:** "Create API Key"
4. **Name:** `Wihngo-Production`
5. **Permissions:** Full Access
6. **Click:** "Create & View"
7. **Copy the key** (starts with `SG.`)

?? **Save it now** - you won't see it again!

---

### Step 2: Verify Sender Email (1 minute)

1. **Go to:** Settings ? Sender Authentication
2. **Click:** "Single Sender Verification"
3. **Fill in:**
   - From Email: `noreply@wihngo.com`
   - From Name: `Wihngo`
4. **Click:** "Create"
5. **Check your email** and verify

---

### Step 3: Set Environment Variables (PowerShell)

```powershell
# Open PowerShell as Administrator
[System.Environment]::SetEnvironmentVariable('SENDGRID_API_KEY', 'SG.your-actual-key-here', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_PROVIDER', 'SendGrid', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_FROM', 'noreply@wihngo.com', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_FROM_NAME', 'Wihngo', 'User')
```

**Or use User Secrets:**
```bash
cd C:\.net\Wihngo
dotnet user-secrets set "SENDGRID_API_KEY" "SG.your-actual-key-here"
dotnet user-secrets set "EMAIL_PROVIDER" "SendGrid"
```

---

### Step 4: Restart Application

**Visual Studio:** Close and reopen, then press F5

---

### Step 5: Verify It Works

**Check logs:**
```
Email Configuration loaded:
  Provider: SendGrid
  SendGrid API Key: ***configured***
  From Email: noreply@wihngo.com
```

**Test registration:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "your-email@example.com",
    "password": "Test@1234Strong"
  }'
```

**Check your email** - should receive confirmation email!

---

## ?? For Render (Production)

1. **Go to:** https://dashboard.render.com
2. **Select:** Your service
3. **Click:** Environment tab
4. **Add:**
   ```
   SENDGRID_API_KEY = SG.your-actual-key-here
   EMAIL_PROVIDER = SendGrid
   EMAIL_FROM = noreply@wihngo.com
   EMAIL_FROM_NAME = Wihngo
   ```
5. **Save** (auto-deploys)

---

## ? Success Indicators

- ? Logs show "SendGrid API Key: ***configured***"
- ? Test registration sends confirmation email
- ? Email appears in SendGrid Activity dashboard
- ? Email received in inbox (check spam if not)

---

## ?? Quick Troubleshooting

### "SendGrid API Key: NOT SET"
? Environment variable not set. Restart Visual Studio.

### "403 Forbidden"
? API key invalid or insufficient permissions. Create new key with Full Access.

### "Email not delivered"
? Sender email not verified. Complete sender verification.

### "Still not working?"
? See full guide: `SENDGRID_INTEGRATION_GUIDE.md`

---

## ?? What Emails Are Sent?

- ? Registration confirmation
- ? Password reset
- ? Welcome email
- ? Security alerts
- ? Account notifications

All with beautiful HTML templates! ??

---

**That's it! SendGrid is ready in 5 minutes!** ??

**Full documentation:** `SENDGRID_INTEGRATION_GUIDE.md`
