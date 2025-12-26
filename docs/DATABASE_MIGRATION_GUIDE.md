# ?? Quick Migration: Database Connection String

## ?? Breaking Change

**Your database connection string is no longer in `appsettings.json`!**

This is a **security improvement** - connection strings should never be committed to Git.

---

## ?? Quick Fix (2 Minutes)

### Step 1: Get Your Connection String

**From old `appsettings.json`:**
```
Host=***REMOVED***;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=***REMOVED***;SSL Mode=Require;Trust Server Certificate=True
```

Or **get from Render Dashboard:**
1. Go to: https://dashboard.render.com
2. Click your PostgreSQL database
3. Copy **Internal Database URL** or connection details

---

### Step 2: Set Environment Variable

**PowerShell (Run as Administrator):**
```powershell
[System.Environment]::SetEnvironmentVariable('DEFAULT_CONNECTION', 'Host=***REMOVED***;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=***REMOVED***;SSL Mode=Require;Trust Server Certificate=True', 'User')
```

**Or User Secrets (Easier):**
```bash
cd C:\.net\Wihngo
dotnet user-secrets set "DEFAULT_CONNECTION" "Host=***REMOVED***;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=***REMOVED***;SSL Mode=Require;Trust Server Certificate=True"
```

---

### Step 3: Restart Visual Studio

**Important:** Close and reopen Visual Studio!

---

### Step 4: Verify It Works

Start your application and check logs:

**? Success:**
```
Database Configuration:
  Connection: ***configured***
  Database: wihngo_kzno
```

**? Error:**
```
InvalidOperationException: Database connection string is not configured. Set DEFAULT_CONNECTION environment variable.
```

If you see the error, the environment variable is not set correctly!

---

## ?? For Production (Render)

1. Go to: https://dashboard.render.com
2. Select your service: `wihngo-api`
3. Click **"Environment"** tab
4. Add or update:

```
DEFAULT_CONNECTION = Host=***REMOVED***;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=***REMOVED***;SSL Mode=Require;Trust Server Certificate=True;Pooling=true
```

5. Click **"Save Changes"**
6. Render will automatically redeploy

---

## ?? Troubleshooting

### Issue: Application won't start

**Error:**
```
InvalidOperationException: Database connection string is not configured
```

**Solution:**
1. Set `DEFAULT_CONNECTION` environment variable
2. Restart Visual Studio
3. Verify with: `$env:DEFAULT_CONNECTION` in PowerShell

---

### Issue: Can't connect to database

**Error:**
```
Npgsql.NpgsqlException: Connection refused
```

**Solution:**
1. Verify connection string is correct
2. Check database is running
3. Confirm host, port, username, password are correct

---

### Issue: Still reading from appsettings.json

**Solution:**
The fallback to `appsettings.json` has been removed. You **must** use the environment variable now.

---

## ?? What Changed?

### Before:
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Password=secret"  // ? Exposed
  }
}
```

### After:
```bash
# Environment Variable
DEFAULT_CONNECTION=Host=...;Password=secret  # ? Secure
```

### Benefits:
- ? Connection string not in Git
- ? Easy to rotate credentials
- ? Different credentials per environment
- ? Industry standard practice
- ? Better security

---

## ? Quick Checklist

- [ ] Got connection string from Render or old appsettings.json
- [ ] Set `DEFAULT_CONNECTION` environment variable
- [ ] Restarted Visual Studio
- [ ] Application starts successfully
- [ ] Logs show "Database Configuration: ***configured***"
- [ ] Database operations work (test registration)

---

## ?? Full Documentation

For complete setup guide: `DATABASE_CONNECTION_SETUP.md`

For all environment variables: `ENVIRONMENT_VARIABLES_COMPLETE.md`

---

**? This is a one-time setup! Takes 2 minutes!** ??

**Set `DEFAULT_CONNECTION` and restart - that's it!**
