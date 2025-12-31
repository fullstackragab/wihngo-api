# ?? Database Connection String - Environment Variable Setup

## ? Security Improvement Applied

The database connection string has been **removed from `appsettings.json`** and now reads from the `DEFAULT_CONNECTION` environment variable for better security.

---

## ?? Required Environment Variable

```bash
DEFAULT_CONNECTION=Host=your-host;Port=5432;Database=wihngo;Username=user;Password=password;SSL Mode=Require;Trust Server Certificate=True
```

---

## ?? Local Development Setup

### Option 1: PowerShell (Windows)

#### Permanent Setup (Run as Administrator):
```powershell
[System.Environment]::SetEnvironmentVariable('DEFAULT_CONNECTION', 'Host=YOUR_DB_HOST;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=YOUR_DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=True', 'User')
```

**Remember:** Restart Visual Studio after setting!

---

### Option 2: User Secrets (Recommended for Development)

```bash
cd C:\.net\Wihngo

dotnet user-secrets set "DEFAULT_CONNECTION" "Host=your-host;Port=5432;Database=wihngo;Username=user;Password=password;SSL Mode=Require;Trust Server Certificate=True"
```

**Benefits:**
- ? Stored outside project directory
- ? Not committed to Git
- ? Per-user configuration
- ? Most secure for development

---

### Option 3: launchSettings.json

Edit `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DEFAULT_CONNECTION": "Host=your-host;Port=5432;Database=wihngo;Username=user;Password=password;SSL Mode=Require"
      }
    }
  }
}
```

**?? Important:** Add `launchSettings.json` to `.gitignore`!

---

## ?? Production Setup (Render)

### Render Dashboard:

1. Go to https://dashboard.render.com
2. Select your service: `wihngo-api`
3. Click **"Environment"** tab
4. Add/update this variable:

```
DEFAULT_CONNECTION = Host=YOUR_DB_HOST;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=YOUR_DB_PASSWORD;Pooling=true;SSL Mode=Require;Trust Server Certificate=True
```

5. Click **"Save Changes"**
6. Render will automatically redeploy

---

## ?? Verification

After setting the environment variable and restarting your application, check the logs:

```
Database Configuration:
  Connection: ***configured***
  Database: wihngo_kzno
```

**If you see an error:**
```
Database connection string is not configured. Set DEFAULT_CONNECTION environment variable.
```

Then the environment variable is not set correctly!

---

## ?? Connection String Format

### PostgreSQL Connection String:

```
Host=hostname;Port=5432;Database=dbname;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=True;Pooling=true
```

### Parameters Explained:

| Parameter | Description | Example |
|-----------|-------------|---------|
| `Host` | Database server hostname | `dpg-xxx.oregon-postgres.render.com` |
| `Port` | Database port | `5432` |
| `Database` | Database name | `wihngo_kzno` |
| `Username` | Database user | `wihngo` |
| `Password` | Database password | `your-password` |
| `SSL Mode` | SSL/TLS encryption | `Require` |
| `Trust Server Certificate` | Trust self-signed certs | `True` |
| `Pooling` | Enable connection pooling | `true` (optional) |

---

## ?? Testing Database Connection

### Test 1: Check Startup Logs

```
Database Configuration:
  Connection: ***configured***
  Database: wihngo_kzno
```

### Test 2: Test API Endpoint

```bash
curl http://localhost:5000/test
```

**Expected Response:**
```json
{
  "status": "OK",
  "timestamp": "2024-01-15T10:30:00Z",
  "message": "Backend is running"
}
```

### Test 3: Database Operations

Try user registration or any database operation:
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "password": "Test@1234Strong"
  }'
```

---

## ?? Configuration Priority

The application checks in this order:

1. **Environment Variable** (highest priority)
   ```
   DEFAULT_CONNECTION
   ```

2. **appsettings.json** (fallback - now removed for security)
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "..."
   }
   ```

3. **Error** (if none set)
   ```
   InvalidOperationException: Database connection string is not configured
   ```

---

## ?? Security Best Practices

### ? DO:
- Use environment variables for connection strings
- Use User Secrets for development
- Different credentials for dev and production
- Use SSL/TLS encryption (`SSL Mode=Require`)
- Enable connection pooling
- Rotate database passwords regularly

### ? DON'T:
- Commit connection strings to Git
- Share credentials in plain text
- Use same credentials across environments
- Disable SSL in production
- Use weak passwords
- Leave debug logging enabled in production

---

## ?? Troubleshooting

### Issue: "Database connection string is not configured"

**Cause:** Environment variable not set

**Solution:**
1. Set `DEFAULT_CONNECTION` environment variable
2. Restart Visual Studio
3. Verify with: `$env:DEFAULT_CONNECTION` (PowerShell)

### Issue: "Connection refused" or "Could not connect to server"

**Cause:** Invalid host or port

**Solution:**
1. Verify database host is accessible
2. Check firewall rules
3. Confirm port is correct (usually 5432)
4. Test connection with database client

### Issue: "Password authentication failed"

**Cause:** Invalid username or password

**Solution:**
1. Verify credentials are correct
2. Check for extra spaces or special characters
3. Ensure password is properly escaped in connection string

### Issue: "SSL connection error"

**Cause:** SSL configuration mismatch

**Solution:**
1. Add `SSL Mode=Require`
2. Add `Trust Server Certificate=True` if self-signed
3. Update to latest Npgsql package

### Issue: Works in Visual Studio but fails when deployed

**Cause:** Environment variable not set in production

**Solution:**
1. Go to Render Dashboard
2. Add `DEFAULT_CONNECTION` to Environment variables
3. Save and redeploy

---

## ?? Complete Environment Variables

Your application now uses these environment variables:

```bash
# Database
DEFAULT_CONNECTION=Host=...;Database=...

# AWS S3
AWS_ACCESS_KEY_ID=AKIA...
AWS_SECRET_ACCESS_KEY=...

# SendGrid Email
SENDGRID_API_KEY=SG....
EMAIL_PROVIDER=SendGrid

# Application
FrontendUrl=https://wihngo.com
```

---

## ?? Quick Setup Commands

### PowerShell (All Variables):

```powershell
# Run as Administrator

# Database
[System.Environment]::SetEnvironmentVariable('DEFAULT_CONNECTION', 'Host=dpg-xxx.oregon-postgres.render.com;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=True', 'User')

# AWS S3
[System.Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', 'YOUR_KEY', 'User')
[System.Environment]::SetEnvironmentVariable('AWS_SECRET_ACCESS_KEY', 'YOUR_SECRET', 'User')

# SendGrid
[System.Environment]::SetEnvironmentVariable('SENDGRID_API_KEY', 'SG.YOUR_KEY', 'User')
[System.Environment]::SetEnvironmentVariable('EMAIL_PROVIDER', 'SendGrid', 'User')
```

**Restart Visual Studio after setting all variables!**

---

### User Secrets (All Variables):

```bash
cd C:\.net\Wihngo

# Database
dotnet user-secrets set "DEFAULT_CONNECTION" "Host=...;Database=..."

# AWS S3
dotnet user-secrets set "AWS_ACCESS_KEY_ID" "YOUR_KEY"
dotnet user-secrets set "AWS_SECRET_ACCESS_KEY" "YOUR_SECRET"

# SendGrid
dotnet user-secrets set "SENDGRID_API_KEY" "SG.YOUR_KEY"
dotnet user-secrets set "EMAIL_PROVIDER" "SendGrid"
```

---

## ?? Setup Checklist

### Local Development:
- [ ] Set `DEFAULT_CONNECTION` environment variable
- [ ] Restarted Visual Studio
- [ ] Checked logs for "Database Configuration"
- [ ] Tested database connection
- [ ] All other environment variables set (AWS, SendGrid)

### Production (Render):
- [ ] Added `DEFAULT_CONNECTION` to Render environment
- [ ] Saved and redeployed
- [ ] Verified logs show successful connection
- [ ] Tested API endpoints
- [ ] All features working

---

## ?? Migrating from Old Setup

### Before (Insecure):
```json
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Host=...;Password=secret"  // ? Exposed in Git
}
```

### After (Secure):
```bash
# Environment variable
DEFAULT_CONNECTION=Host=...;Password=secret  # ? Not in Git
```

### Migration Steps:

1. **Copy your connection string** from old `appsettings.json`
2. **Set environment variable** with the connection string
3. **Restart** Visual Studio
4. **Test** that application starts and connects
5. **Connection string removed** from appsettings.json (already done!)

---

## ?? Additional Resources

- **Npgsql Documentation:** https://www.npgsql.org/doc/connection-string-parameters.html
- **PostgreSQL Connection Strings:** https://www.connectionstrings.com/postgresql/
- **ASP.NET Configuration:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
- **Render PostgreSQL:** https://render.com/docs/databases

---

## ? What Changed

### Files Modified:

1. **`Program.cs`**
   - ? Now reads from `DEFAULT_CONNECTION` environment variable
   - ? Falls back to `ConnectionStrings:DefaultConnection` if needed
   - ? Throws error if neither is set
   - ? Logs database configuration on startup
   - ? Extracts and logs database name

2. **`appsettings.json`**
   - ? Removed `ConnectionStrings` section
   - ? Connection string no longer in version control
   - ? Improved security

---

**?? Your database connection is now secure and configured via environment variables!** ??

**Setup Guide:** Follow steps above for your environment  
**Quick Start:** Set `DEFAULT_CONNECTION` and restart  
**All Variables:** See `ENVIRONMENT_VARIABLES_COMPLETE.md`
