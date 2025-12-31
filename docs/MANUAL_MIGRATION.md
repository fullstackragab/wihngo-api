# Manual Migration - Copy and Paste Method

## Problem
The migration script failed to run. This can happen due to:
- Path issues with psql
- Connection timeout
- Firewall blocking the connection
- SSL certificate issues

## Solution: Manual Migration

Follow these steps to manually apply the migration:

---

## Step 1: Copy the SQL Script

Open `Database/migrations/fix_missing_columns.sql` and copy its entire contents.

---

## Step 2: Connect to Database (Choose One Method)

### Method A: Using Command Prompt

1. Open **Command Prompt** (not PowerShell)
2. Navigate to PostgreSQL bin directory:
   ```cmd
   cd "C:\Program Files\PostgreSQL\18\bin"
   ```
3. Set password:
   ```cmd
   set PGPASSWORD=YOUR_DB_PASSWORD
   ```
4. Connect:
   ```cmd
   psql "postgresql://wihngo@YOUR_DB_HOST:5432/wihngo_kzno?sslmode=require"
   ```

### Method B: Using Render Dashboard (Easiest!)

1. Go to https://dashboard.render.com
2. Click on your **wihngo_kzno** database
3. Click the **"Connect"** button (top right)
4. Select **"External Connection"**
5. This will open a web-based psql terminal

### Method C: Using pgAdmin

1. Open pgAdmin
2. Right-click **Servers** ? **Register** ? **Server**
3. General tab:
   - Name: `Wihngo Render DB`
4. Connection tab:
   - Host: `YOUR_DB_HOST`
   - Port: `5432`
   - Database: `wihngo_kzno`
   - Username: `wihngo`
   - Password: `YOUR_DB_PASSWORD`
5. SSL tab:
   - SSL mode: `Require`
6. Click **Save**
7. Right-click the database ? **Query Tool**

### Method D: Using DBeaver (Free)

1. Download DBeaver from https://dbeaver.io/download/
2. Create new connection ? PostgreSQL
3. Fill in connection details (same as above)
4. Enable SSL in Advanced settings
5. Test connection
6. Open SQL Editor

---

## Step 3: Run the Migration

Once connected, paste the entire SQL script and execute it.

You should see:
```
? VERIFIED: crypto_payment_requests.confirmations
? VERIFIED: token_configurations.token_address
? VERIFIED: onchain_deposits.metadata

? ALL COLUMNS VERIFIED SUCCESSFULLY
```

---

## Step 4: Verify Success

Run this query to check if columns exist:

```sql
SELECT 
    table_name, 
    column_name, 
    data_type
FROM information_schema.columns 
WHERE table_name IN ('crypto_payment_requests', 'token_configurations', 'onchain_deposits')
  AND column_name IN ('confirmations', 'token_address', 'metadata')
ORDER BY table_name, column_name;
```

You should see 3 rows:
- `crypto_payment_requests` | `confirmations` | `integer`
- `onchain_deposits` | `metadata` | `jsonb`
- `token_configurations` | `token_address` | `character varying`

---

## Step 5: Restart Application

The app needs to reload the database schema.

---

## Step 6: Test Sign Up

Try registering a new user. It should work now!

---

## Common Connection Issues

### Issue: "psql: error: connection to server failed"
**Solution:** 
- Check your internet connection
- Try Method B (Render Dashboard) - works through browser
- Verify firewall isn't blocking port 5432

### Issue: "SSL connection required"
**Solution:**
- Make sure connection string includes `?sslmode=require`
- In pgAdmin/DBeaver, enable SSL in connection settings

### Issue: "FATAL: password authentication failed"
**Solution:**
- Double-check the password (copy-paste to avoid typos)
- Verify username is exactly `wihngo` (lowercase)

### Issue: Timeout
**Solution:**
- Render databases can be slow to respond
- Try Method B (Render Dashboard) - most reliable
- Wait 30 seconds and try again

---

## Still Stuck?

If manual migration doesn't work, we can try:

1. **Alternative approach:** Add columns via EF Core migration
2. **Direct SQL via Render:** Use Render's SQL editor directly
3. **Temporary workaround:** Comment out the problematic queries in code

Let me know what error you're seeing and I'll help further!

---

## The SQL You Need

In case you need it quickly, here's the exact SQL to run:

```sql
BEGIN;

-- Add confirmations column
ALTER TABLE crypto_payment_requests 
ADD COLUMN IF NOT EXISTS confirmations integer NOT NULL DEFAULT 0;

-- Add token_address column
ALTER TABLE token_configurations 
ADD COLUMN IF NOT EXISTS token_address character varying(255) NOT NULL DEFAULT '';

-- Add metadata column
ALTER TABLE onchain_deposits 
ADD COLUMN IF NOT EXISTS metadata jsonb;

COMMIT;
```

That's it! Just 3 ALTER TABLE statements.
