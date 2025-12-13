# ?? View Authentication Database - Quick Guide

## What You Asked For
Display everything related to authentication (signup, register, login, etc.)

## ? Created for You

I've created **4 ways** to view your authentication database:

---

## ?? Option 1: Quick Check (Fastest!)

**Double-click:** `QUICK_AUTH_CHECK.bat`

Shows:
- All users
- Email confirmation status
- Login history
- Account lock status

**Output:** Simple table view

---

## ?? Option 2: Quick View (Essential Info)

**File:** `Database/queries/quick_auth_view.sql`

**Run with:**
```cmd
"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@***REMOVED***:5432/wihngo_kzno?sslmode=require" -f Database\queries\quick_auth_view.sql
```

Shows:
- User status with emojis
- Email confirmation status
- Password reset status
- Session status

---

## ?? Option 3: Full Report (Everything!)

**Double-click:** `VIEW_AUTH_DATABASE.bat`

**Or run:**
```cmd
"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@***REMOVED***:5432/wihngo_kzno?sslmode=require" -f Database\queries\view_auth_database.sql
```

Shows **13 sections:**
1. Users table schema
2. All users (basic info)
3. Authentication security status
4. Locked accounts
5. Unconfirmed emails
6. Active password reset requests
7. Active refresh tokens
8. Recent logins (last 7 days)
9. Registration statistics
10. Authentication health check
11. Authentication columns check
12. Sample user details
13. Database size

---

## ?? Option 4: Documentation

**Read:** `AUTH_DATABASE_SCHEMA.md`

Complete documentation including:
- Full table schema
- All columns explained
- Authentication flows
- User states
- Common queries
- Statistics queries
- Maintenance queries
- Security features
- Token lifetimes
- Mobile integration notes
- Example data

---

## ?? What to Run Right Now

**Quickest way to see your data:**

1. **Double-click:** `QUICK_AUTH_CHECK.bat`
2. See all users instantly

**For full details:**

1. **Double-click:** `VIEW_AUTH_DATABASE.bat`
2. See complete 13-section report

---

## ?? What You'll See

### Users Table Structure

```
user_id              | UUID (primary key)
name                 | User's full name
email                | Email address (unique)
password_hash        | BCrypt hashed password
email_confirmed      | true/false
email_confirmation_token | Token for email verification
email_confirmation_token_expiry | 24 hours from generation
password_reset_token | Token for password reset
password_reset_token_expiry | 1 hour from generation
refresh_token_hash   | Hashed refresh token
refresh_token_expiry | 30 days from generation
failed_login_attempts | Count (max 5 before lock)
is_account_locked    | true/false
lockout_end          | When account unlocks (30 min)
last_login_at        | Last successful login
last_password_change_at | Last password change
created_at           | Registration date
updated_at           | Last update
```

### Example Output (Quick Check)

```
 user_id  |    name    |      email       | email_confirmed |     created_at      | last_login_at | failed_login_attempts | is_account_locked
----------+------------+------------------+-----------------+---------------------+---------------+-----------------------+------------------
 abc-123  | John Doe   | john@example.com |       t         | 2025-01-15 10:00:00 | 2025-01-15... |           0           |        f
 def-456  | Jane Smith | jane@example.com |       f         | 2025-01-15 11:00:00 |      null     |           0           |        f
```

### Example Output (Full Report)

```
========================================
1. USERS TABLE SCHEMA
----------------------------------------
column_name                      | data_type         | is_nullable
---------------------------------+-------------------+------------
user_id                          | uuid              | NO
name                             | character varying | NO
email                            | character varying | NO
...

2. ALL USERS (Basic Info)
----------------------------------------
(Shows all users with key fields)

3. AUTHENTICATION SECURITY STATUS
----------------------------------------
(Shows token status, expiry times, etc.)

...continues with 13 sections...

========================================
END OF AUTHENTICATION DATABASE REPORT
========================================
```

---

## ?? Authentication Fields Summary

| Field | Purpose | Lifetime |
|-------|---------|----------|
| `email_confirmation_token` | Verify email after signup | 24 hours |
| `password_reset_token` | Reset forgotten password | 1 hour |
| `refresh_token_hash` | Keep user logged in | 30 days |
| `failed_login_attempts` | Track failed logins | Reset on success |
| `is_account_locked` | Lock after 5 failures | 30 minutes |

---

## ?? What Mobile Developers Need

All authentication data is in the **`users`** table:

- **Email confirmation**: Check `email_confirmed` field
- **Account lock**: Check `is_account_locked` field
- **Active sessions**: Check `refresh_token_expiry`
- **Failed attempts**: Check `failed_login_attempts`

**API handles all token validation** - mobile just needs to:
1. Call API endpoints
2. Handle responses
3. Store tokens securely

---

## ??? Files Created

| File | Purpose |
|------|---------|
| `QUICK_AUTH_CHECK.bat` | Quick view of all users |
| `VIEW_AUTH_DATABASE.bat` | Full 13-section report |
| `Database/queries/quick_auth_view.sql` | SQL for quick view |
| `Database/queries/view_auth_database.sql` | SQL for full report |
| `AUTH_DATABASE_SCHEMA.md` | Complete documentation |
| `VIEW_AUTH_README.md` | This guide |

---

## ?? Quick Start

**Want to see your auth database right now?**

### Windows:
```
Double-click: QUICK_AUTH_CHECK.bat
```

### Command Line:
```cmd
cd C:\.net\Wihngo
QUICK_AUTH_CHECK.bat
```

### For Full Report:
```
Double-click: VIEW_AUTH_DATABASE.bat
```

---

## ?? What Each View Shows

### QUICK_AUTH_CHECK.bat
- Simple table
- All users
- Basic auth info
- **Best for:** Quick overview

### quick_auth_view.sql
- Enhanced view
- Status with emojis
- Email/session status
- **Best for:** At-a-glance status

### view_auth_database.sql
- 13 detailed sections
- Complete statistics
- Health checks
- **Best for:** Deep analysis

### AUTH_DATABASE_SCHEMA.md
- Full documentation
- Schema details
- Query examples
- **Best for:** Understanding structure

---

## ?? Common Use Cases

### "How many users do I have?"
? Run `QUICK_AUTH_CHECK.bat`

### "Are there any locked accounts?"
? Run `VIEW_AUTH_DATABASE.bat` (see section 4)

### "Who hasn't confirmed their email?"
? Run `VIEW_AUTH_DATABASE.bat` (see section 5)

### "What's the full schema?"
? Read `AUTH_DATABASE_SCHEMA.md`

### "How do I query specific data?"
? See `AUTH_DATABASE_SCHEMA.md` (Common Queries section)

---

## ? Ready to Use

Everything is set up! Just:

1. **Quick view**: Double-click `QUICK_AUTH_CHECK.bat`
2. **Full report**: Double-click `VIEW_AUTH_DATABASE.bat`
3. **Documentation**: Open `AUTH_DATABASE_SCHEMA.md`

No configuration needed - scripts use your existing PostgreSQL installation.

---

**Created:** January 2025  
**Database:** PostgreSQL (Render)  
**Location:** C:\.net\Wihngo
