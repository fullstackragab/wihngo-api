# Migration Failed - Troubleshooting Guide

## What Happened?

The automated migration script failed. This is actually common and easy to fix!

---

## Quick Diagnosis

**What error did you see?** (Check which one applies)

### ? "psql: command not found" or "not recognized"
**Problem:** PowerShell can't find psql
**Solution:** Use the manual method (see below)

### ? "connection timeout" or "could not connect"
**Problem:** Network/firewall issue
**Solution:** Use Render Dashboard method (easiest!)

### ? "SSL connection required"
**Problem:** SSL not configured
**Solution:** Connection string needs `?sslmode=require`

### ? "password authentication failed"
**Problem:** Wrong credentials
**Solution:** Double-check username/password

### ? "file not found"
**Problem:** Script can't find SQL file
**Solution:** Run from correct directory or use manual method

---

## Solution: Use Render Dashboard (EASIEST!)

This bypasses all local issues and works through your browser:

### Step 1: Go to Render Dashboard
1. Open browser ? https://dashboard.render.com
2. Log in
3. Click on **wihngo_kzno** database

### Step 2: Open SQL Console
1. Click **"Connect"** button (top right)
2. Select **"External Connection"**
3. Wait for terminal to load (may take 10-20 seconds)

### Step 3: Run the Fix
Copy and paste this SQL (all 3 commands at once):

```sql
ALTER TABLE crypto_payment_requests 
ADD COLUMN IF NOT EXISTS confirmations integer NOT NULL DEFAULT 0;

ALTER TABLE token_configurations 
ADD COLUMN IF NOT EXISTS token_address character varying(255) NOT NULL DEFAULT '';

ALTER TABLE onchain_deposits 
ADD COLUMN IF NOT EXISTS metadata jsonb;
```

Press Enter. You should see:
```
ALTER TABLE
ALTER TABLE
ALTER TABLE
```

### Step 4: Verify
Run this to check:
```sql
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'crypto_payment_requests' 
  AND column_name = 'confirmations';
```

Should return: `confirmations`

### Step 5: Done!
- Close the terminal
- Restart your application
- Test signup

---

## Alternative: Use pgAdmin (GUI Tool)

If you prefer a visual tool:

1. Download pgAdmin: https://www.pgadmin.org/download/
2. Install and open it
3. Right-click **Servers** ? **Register** ? **Server**
4. Fill in connection details:
   ```
   Name: Wihngo Render
   Host: ***REMOVED***
   Port: 5432
   Database: wihngo_kzno
   Username: wihngo
   Password: ***REMOVED***
   SSL Mode: Require (in SSL tab)
   ```
5. Click **Save**
6. Expand: Servers ? Wihngo Render ? Databases ? wihngo_kzno
7. Right-click database ? **Query Tool**
8. Paste the 3 ALTER TABLE commands
9. Click **Execute** (? button)

---

## Alternative: Use Command Prompt (Not PowerShell)

PowerShell can have path issues. Command Prompt is more reliable:

1. Open **Command Prompt** (search for `cmd`)
2. Navigate to PostgreSQL:
   ```cmd
   cd "C:\Program Files\PostgreSQL\18\bin"
   ```
3. Set password:
   ```cmd
   set PGPASSWORD=***REMOVED***
   ```
4. Connect:
   ```cmd
   psql "postgresql://wihngo@***REMOVED***:5432/wihngo_kzno?sslmode=require"
   ```
5. Once connected, paste the 3 ALTER TABLE commands
6. Type `\q` to exit

---

## The Absolute Simplest Method

**Just copy these 3 lines and paste into ANY PostgreSQL client:**

```sql
ALTER TABLE crypto_payment_requests ADD COLUMN IF NOT EXISTS confirmations integer NOT NULL DEFAULT 0;
ALTER TABLE token_configurations ADD COLUMN IF NOT EXISTS token_address character varying(255) NOT NULL DEFAULT '';
ALTER TABLE onchain_deposits ADD COLUMN IF NOT EXISTS metadata jsonb;
```

That's it. Three lines. Copy, paste, done.

---

## After Successfully Running Migration

1. **Restart your application** (if running)
2. **Try signing up a user** - should work now
3. **Check application logs** - no more column errors

Expected log change:
```diff
- ? column c.confirmations does not exist
- ? column t.token_address does not exist
- ? column o.metadata does not exist
+ ? Total non-completed payments found: 0
+ ?? PAYMENT MONITOR JOB COMPLETED
```

---

## Still Having Issues?

Tell me:
1. Which method did you try?
2. What exact error message did you see?
3. Did any of the SQL commands work?

I'll help you get it working! ??

---

## Files to Use

Choose the simplest one for your situation:

| File | Use When |
|------|----------|
| `Database/migrations/simple_fix.sql` | You want the shortest SQL |
| `Database/migrations/fix_missing_columns.sql` | You want verification included |
| `MANUAL_MIGRATION.md` | Scripts aren't working |
| `run-migration-with-full-path.ps1` | You want to retry automation |

---

**Remember:** You only need to run 3 ALTER TABLE commands. That's the whole fix!
