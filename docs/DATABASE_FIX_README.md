# Database Schema Fix - Missing Columns

## Problem

The application is failing with database errors because several columns are missing from the database schema:

```
? 42703: column c.confirmations does not exist
? 42703: column t.token_address does not exist  
? 42703: column o.metadata does not exist
```

### Impact

- **Sign up is failing** - Background jobs crash and prevent user registration
- **Payment monitoring fails** - PaymentMonitorJob cannot query payments
- **Deposit scanning fails** - OnChainDepositBackgroundService crashes
- **Blockchain listeners fail** - SolanaListenerService and EvmListenerService error out

### Root Cause

The C# models (entity classes) were updated with new properties, but the corresponding database migrations were never executed on the production database. This creates a mismatch between the application code and the database schema.

## Missing Columns

| Table | Column | Type | Purpose |
|-------|--------|------|---------|
| `crypto_payment_requests` | `confirmations` | `integer` | Tracks blockchain confirmations for crypto payments |
| `token_configurations` | `token_address` | `varchar(255)` | Stores token contract addresses |
| `onchain_deposits` | `metadata` | `jsonb` | Additional deposit metadata in JSON format |

## Solution

Run the provided migration script to add the missing columns to your database.

### Quick Fix (Recommended)

**Using PowerShell:**

```powershell
.\run-fix-migration.ps1
```

This script will:
1. Connect to your Render PostgreSQL database
2. Execute the migration to add missing columns
3. Verify all columns were created successfully
4. Report the status

### Manual Fix (Alternative)

If you prefer to run the migration manually:

**Option 1: Using psql command line**

```bash
# Set password environment variable
export PGPASSWORD="YOUR_DB_PASSWORD"

# Run migration
psql "postgresql://wihngo@YOUR_DB_HOST:5432/wihngo_kzno?sslmode=require" \
  -f Database/migrations/fix_missing_columns.sql

# Clear password
unset PGPASSWORD
```

**Option 2: Using Render Dashboard**

1. Go to your Render database dashboard
2. Click "Connect" ? "External Connection"
3. Copy the provided psql command
4. Open the SQL file `Database/migrations/fix_missing_columns.sql`
5. Copy its contents
6. Paste into the psql terminal

**Option 3: Copy/Paste SQL**

1. Open `Database/migrations/fix_missing_columns.sql`
2. Copy the entire contents
3. Connect to your database using any PostgreSQL client
4. Paste and execute the SQL

## Migration Script Details

The migration script (`fix_missing_columns.sql`) does the following:

1. **Adds `confirmations` column** to `crypto_payment_requests` table
   - Type: `integer`
   - Default: `0`
   - Required: `NOT NULL`

2. **Adds `token_address` column** to `token_configurations` table
   - Type: `varchar(255)`
   - Default: `''` (empty string)
   - Required: `NOT NULL`

3. **Adds `metadata` column** to `onchain_deposits` table
   - Type: `jsonb`
   - Optional: `NULL`

4. **Verifies** all columns were created successfully

The script is **idempotent** - it's safe to run multiple times. If a column already exists, it will skip adding it.

## Verification

After running the migration, you should see:

```
? VERIFIED: crypto_payment_requests.confirmations
? VERIFIED: token_configurations.token_address
? VERIFIED: onchain_deposits.metadata

========================================
? ALL COLUMNS VERIFIED SUCCESSFULLY
========================================
```

## Testing After Fix

1. **Restart your application** (if running)
2. **Try registering a new user** - should now work without errors
3. **Check application logs** - background jobs should run without database errors
4. **Monitor Hangfire dashboard** - jobs should complete successfully

## Expected Log Changes

**Before fix (errors):**
```
? MONITOR JOB ERROR: 42703: column c.confirmations does not exist
? ERROR: column t.token_address does not exist
? ERROR: column o.metadata does not exist
```

**After fix (working):**
```
? Total non-completed payments found: 0
?? Payments with transaction hash: 0
?? No payments with transaction hash to monitor
?? PAYMENT MONITOR JOB COMPLETED
```

## Troubleshooting

### "psql: command not found"

Install PostgreSQL client tools:

**Windows:**
- Download from https://www.postgresql.org/download/windows/
- Or install via Chocolatey: `choco install postgresql`

**macOS:**
```bash
brew install postgresql
```

**Linux:**
```bash
# Debian/Ubuntu
sudo apt-get install postgresql-client

# Fedora/RHEL
sudo dnf install postgresql
```

### Connection timeout or refused

- Check your internet connection
- Verify the database is running (check Render dashboard)
- Ensure SSL mode is set to "require"
- Try the connection string from Render's "External Connection" tab

### Permission denied

- Verify you're using the correct username and password
- Check if the database user has ALTER TABLE permissions

### Still getting errors after migration

1. **Restart the application** - EF Core may cache the old schema
2. **Check the migration ran successfully** - look for the success message
3. **Verify columns exist** - run this SQL:
   ```sql
   SELECT column_name, data_type 
   FROM information_schema.columns 
   WHERE table_name IN ('crypto_payment_requests', 'token_configurations', 'onchain_deposits')
   ORDER BY table_name, ordinal_position;
   ```

## Prevention

To prevent this issue in the future:

1. **Always run migrations** after updating entity models
2. **Test locally first** before deploying to production
3. **Use EF Core migrations** for schema changes:
   ```bash
   dotnet ef migrations add AddMissingColumns
   dotnet ef database update
   ```
4. **Document schema changes** in migration files
5. **Version control migrations** - commit migration files to Git

## Files in This Fix

```
Database/migrations/fix_missing_columns.sql    # Migration script
run-fix-migration.ps1                          # PowerShell runner
DATABASE_FIX_README.md                         # This documentation
```

## Related Issues

- Sign up not working (503 errors)
- Background jobs failing continuously
- Hangfire retries exhausting
- Payment monitoring not working
- Deposit scanning errors

## Support

If you continue to experience issues after running this fix:

1. Check application logs for new error messages
2. Verify the migration completed successfully
3. Ensure the application was restarted after migration
4. Check if there are other missing columns or schema mismatches

---

**Created:** January 2025  
**Last Updated:** January 2025  
**Maintained By:** Wihngo Backend Team
