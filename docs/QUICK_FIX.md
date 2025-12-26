# Quick Fix Summary - Database Schema Issue

## Problem

**Sign up not working** - Users cannot register due to database schema errors.

### Error Messages:
```
? column c.confirmations does not exist
? column t.token_address does not exist
? column o.metadata does not exist
```

## Root Cause

Database migrations were never run. The C# models have properties that don't exist as columns in the database.

## Fix (3 Easy Steps)

### Step 1: Run the Migration

**Option A: PowerShell (Easiest)**
```powershell
.\run-fix-migration.ps1
```

**Option B: Command Line**
```bash
psql "postgresql://wihngo@***REMOVED***:5432/wihngo_kzno?sslmode=require" -f Database/migrations/fix_missing_columns.sql
```

### Step 2: Restart Application

Stop and start your application to reload the schema.

### Step 3: Test

Try registering a new user - it should now work!

## What Was Added

The migration adds three missing columns:

1. `crypto_payment_requests.confirmations` (integer)
2. `token_configurations.token_address` (varchar)
3. `onchain_deposits.metadata` (jsonb)

## Expected Results

**Before:**
- Sign up fails with 503 errors
- Background jobs crash continuously
- Logs show "column does not exist" errors

**After:**
- Sign up works normally
- Background jobs run successfully
- No more database schema errors

## Files Created

| File | Purpose |
|------|---------|
| `Database/migrations/fix_missing_columns.sql` | SQL migration script |
| `run-fix-migration.ps1` | PowerShell runner |
| `DATABASE_FIX_README.md` | Detailed documentation |
| `QUICK_FIX.md` | This summary (you are here) |

## Need Help?

See `DATABASE_FIX_README.md` for:
- Detailed instructions
- Troubleshooting guide
- Alternative methods
- Verification steps

---

**? Estimated Time:** 2-3 minutes  
**?? Difficulty:** Easy  
**? Safe:** Yes (idempotent, can run multiple times)
