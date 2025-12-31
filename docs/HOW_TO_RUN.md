# ? How to Run the Migration

## You Have PostgreSQL at: C:\Program Files\PostgreSQL\18\bin

Perfect! Here are your options:

---

## ?? Option 1: Double-Click (EASIEST!)

**Just double-click this file:**
```
DOUBLE_CLICK_TO_FIX.bat
```

That's it! It will:
1. Connect to your Render database
2. Add the 3 missing columns
3. Show you success/failure message

**Expected output:**
```
============================================
  SUCCESS! COLUMNS ADDED
============================================
```

Then restart your app and test signup!

---

## ?? Option 2: Command Prompt

1. Open **Command Prompt** (not PowerShell)
2. Navigate to your project:
   ```cmd
   cd C:\.net\Wihngo
   ```
3. Run:
   ```cmd
   run-migration.bat
   ```

---

## ?? Option 3: Direct psql Command

Open Command Prompt and run:

```cmd
cd C:\.net\Wihngo
set PGPASSWORD=YOUR_DB_PASSWORD
"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@YOUR_DB_HOST:5432/wihngo_kzno?sslmode=require" -f Database\migrations\simple_fix.sql
```

---

## ?? Option 4: Copy-Paste SQL (If .bat files don't work)

If batch files are blocked by antivirus or security settings:

1. Open Command Prompt
2. Run:
   ```cmd
   cd "C:\Program Files\PostgreSQL\18\bin"
   set PGPASSWORD=YOUR_DB_PASSWORD
   psql "postgresql://wihngo@YOUR_DB_HOST:5432/wihngo_kzno?sslmode=require"
   ```
3. Once connected, copy and paste these 3 lines:
   ```sql
   ALTER TABLE crypto_payment_requests ADD COLUMN IF NOT EXISTS confirmations integer NOT NULL DEFAULT 0;
   ALTER TABLE token_configurations ADD COLUMN IF NOT EXISTS token_address character varying(255) NOT NULL DEFAULT '';
   ALTER TABLE onchain_deposits ADD COLUMN IF NOT EXISTS metadata jsonb;
   ```
4. Press Enter
5. Type `\q` to exit

---

## ? Verification

After running the migration, check if it worked:

### Method 1: Try Signup
Just try registering a user. If it works ? you're done!

### Method 2: Check Database
Run this SQL:
```sql
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'crypto_payment_requests' 
AND column_name = 'confirmations';
```

Should return: `confirmations`

### Method 3: Check App Logs
Look for these changes:
```diff
Before:
- ? column c.confirmations does not exist

After:
+ ? Total non-completed payments found: 0
+ ?? PAYMENT MONITOR JOB COMPLETED
```

---

## ?? Troubleshooting

### "psql.exe not found"
- Check if file exists: `dir "C:\Program Files\PostgreSQL\18\bin\psql.exe"`
- If not, update the path in the .bat files

### "connection timeout"
- Check your internet connection
- Try the Render Dashboard method instead (see EMERGENCY_FIX.md)

### "SSL required"
- Make sure connection string includes `?sslmode=require`
- Already included in all scripts above

### "password authentication failed"
- Double-check the password in the scripts
- Try copying it directly from Render dashboard

---

## ?? Files Created for You

| File | What It Does |
|------|--------------|
| `DOUBLE_CLICK_TO_FIX.bat` | ? Easiest - just double-click |
| `run-migration.bat` | More detailed output |
| `Database/migrations/simple_fix.sql` | The actual SQL |
| `HOW_TO_RUN.md` | This guide |

---

## ?? Quick Start

1. **Double-click:** `DOUBLE_CLICK_TO_FIX.bat`
2. **Press any key** when prompted
3. **Wait 5-10 seconds** for migration to complete
4. **Look for:** "SUCCESS! COLUMNS ADDED"
5. **Restart your application**
6. **Test signup** - should work now!

---

## ?? Tips

- **Run from project directory** (C:\.net\Wihngo)
- **Use Command Prompt**, not PowerShell (for .bat files)
- **Check antivirus** if .bat files won't run
- **Render Dashboard** is always a fallback (see EMERGENCY_FIX.md)

---

## ? Expected Time

- Migration runs: **5-10 seconds**
- Total process: **1 minute**
- Including app restart and test: **2 minutes**

---

## ?? Success Looks Like

```
============================================
  SUCCESS! COLUMNS ADDED
============================================

What to do next:
  1. Restart your application
  2. Try signup again - should work now!
```

Then when you test signup:
- ? User can register
- ? No 503 errors
- ? Background jobs run smoothly
- ? Logs show no "column does not exist" errors

---

**Ready?** Just double-click `DOUBLE_CLICK_TO_FIX.bat` and you're done! ??
