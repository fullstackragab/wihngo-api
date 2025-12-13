# ?? Authentication Database Schema

## Complete Database Structure for Authentication

---

## ?? Users Table Schema

### Table: `users`

| Column Name | Data Type | Nullable | Default | Purpose |
|------------|-----------|----------|---------|---------|
| `user_id` | uuid | NOT NULL | (generated) | Primary key |
| `name` | varchar(100) | NOT NULL | - | User's full name |
| `email` | varchar(255) | NOT NULL | - | Unique email address |
| `password_hash` | varchar(255) | NOT NULL | - | BCrypt hashed password |
| `email_confirmed` | boolean | NOT NULL | false | Email verification status |
| `email_confirmation_token` | varchar(255) | NULL | - | Token for email confirmation |
| `email_confirmation_token_expiry` | timestamp | NULL | - | Token expiration (24 hours) |
| `password_reset_token` | varchar(255) | NULL | - | Token for password reset |
| `password_reset_token_expiry` | timestamp | NULL | - | Token expiration (1 hour) |
| `refresh_token_hash` | varchar(255) | NULL | - | Hashed refresh token |
| `refresh_token_expiry` | timestamp | NULL | - | Refresh token expiration (30 days) |
| `failed_login_attempts` | integer | NOT NULL | 0 | Count of failed login attempts |
| `is_account_locked` | boolean | NOT NULL | false | Account lock status |
| `lockout_end` | timestamp | NULL | - | When account unlocks (30 min) |
| `last_login_at` | timestamp | NULL | - | Last successful login timestamp |
| `last_password_change_at` | timestamp | NULL | - | Last password change timestamp |
| `created_at` | timestamp | NOT NULL | NOW() | Account creation timestamp |
| `updated_at` | timestamp | NOT NULL | NOW() | Last update timestamp |

### Indexes

```sql
CREATE UNIQUE INDEX users_email_key ON users(email);
CREATE INDEX idx_users_email_confirmed ON users(email_confirmed);
CREATE INDEX idx_users_account_locked ON users(is_account_locked);
CREATE INDEX idx_users_created_at ON users(created_at);
```

---

## ?? Authentication Fields Explained

### Email Confirmation Flow

```
User Registers
    ?
email_confirmation_token: "random-base64-string"
email_confirmation_token_expiry: NOW() + 24 hours
email_confirmed: false
    ?
User Clicks Email Link
    ?
email_confirmation_token: NULL
email_confirmation_token_expiry: NULL
email_confirmed: true
```

### Password Reset Flow

```
User Requests Reset
    ?
password_reset_token: "random-base64-string"
password_reset_token_expiry: NOW() + 1 hour
    ?
User Resets Password
    ?
password_reset_token: NULL
password_reset_token_expiry: NULL
last_password_change_at: NOW()
refresh_token_hash: NULL (all sessions revoked)
```

### Login Flow

```
User Logs In Successfully
    ?
last_login_at: NOW()
failed_login_attempts: 0
refresh_token_hash: "hashed-token"
refresh_token_expiry: NOW() + 30 days
    ?
Generate JWT + Refresh Token
```

### Failed Login Flow

```
User Enters Wrong Password
    ?
failed_login_attempts: +1
    ?
If failed_login_attempts >= 5:
    is_account_locked: true
    lockout_end: NOW() + 30 minutes
```

---

## ?? Authentication States

### User Account States

| State | Conditions | Actions Available |
|-------|-----------|-------------------|
| **New User (Unconfirmed)** | `email_confirmed = false`<br>`email_confirmation_token` exists | - Resend confirmation email<br>- Change email address<br>- Cannot login |
| **Active User** | `email_confirmed = true`<br>`is_account_locked = false` | - Login<br>- Change password<br>- Request password reset |
| **Locked Account** | `is_account_locked = true`<br>`lockout_end > NOW()` | - Wait for unlock<br>- Contact support |
| **Expired Lock** | `is_account_locked = true`<br>`lockout_end < NOW()` | - Auto-unlocked on next login attempt |
| **Password Reset Requested** | `password_reset_token` exists<br>`password_reset_token_expiry > NOW()` | - Reset password with token<br>- Request new token if expired |
| **Active Session** | `refresh_token_hash` exists<br>`refresh_token_expiry > NOW()` | - Use refresh token to get new JWT |

---

## ?? Common Queries

### Get All Unconfirmed Users
```sql
SELECT user_id, name, email, created_at, email_confirmation_token_expiry
FROM users
WHERE email_confirmed = false
ORDER BY created_at DESC;
```

### Get All Locked Accounts
```sql
SELECT user_id, name, email, failed_login_attempts, lockout_end
FROM users
WHERE is_account_locked = true AND lockout_end > NOW()
ORDER BY lockout_end DESC;
```

### Get Active Sessions
```sql
SELECT user_id, name, email, last_login_at, refresh_token_expiry
FROM users
WHERE refresh_token_hash IS NOT NULL 
  AND refresh_token_expiry > NOW()
ORDER BY last_login_at DESC;
```

### Get Users Who Need Email Reminder
```sql
SELECT user_id, name, email, created_at
FROM users
WHERE email_confirmed = false
  AND email_confirmation_token_expiry > NOW()
  AND created_at < NOW() - INTERVAL '3 days'
ORDER BY created_at;
```

### Get Password Reset Requests
```sql
SELECT user_id, name, email, password_reset_token_expiry
FROM users
WHERE password_reset_token IS NOT NULL
  AND password_reset_token_expiry > NOW()
ORDER BY password_reset_token_expiry;
```

---

## ?? Statistics Queries

### User Registration Statistics
```sql
SELECT 
    COUNT(*) as total_users,
    COUNT(*) FILTER (WHERE email_confirmed = true) as confirmed,
    COUNT(*) FILTER (WHERE email_confirmed = false) as unconfirmed,
    COUNT(*) FILTER (WHERE is_account_locked = true) as locked,
    COUNT(*) FILTER (WHERE created_at > NOW() - INTERVAL '24 hours') as last_24h,
    COUNT(*) FILTER (WHERE created_at > NOW() - INTERVAL '7 days') as last_7d,
    COUNT(*) FILTER (WHERE last_login_at > NOW() - INTERVAL '24 hours') as active_24h
FROM users;
```

### Authentication Health Check
```sql
SELECT 
    'Total Users' as metric, COUNT(*)::text as value FROM users
UNION ALL
SELECT 'Confirmed Emails', COUNT(*)::text FROM users WHERE email_confirmed = true
UNION ALL
SELECT 'Pending Confirmation', COUNT(*)::text FROM users WHERE email_confirmed = false
UNION ALL
SELECT 'Expired Confirmation Tokens', COUNT(*)::text 
FROM users WHERE email_confirmed = false AND email_confirmation_token_expiry < NOW()
UNION ALL
SELECT 'Active Password Resets', COUNT(*)::text 
FROM users WHERE password_reset_token IS NOT NULL AND password_reset_token_expiry > NOW()
UNION ALL
SELECT 'Active Sessions', COUNT(*)::text 
FROM users WHERE refresh_token_hash IS NOT NULL AND refresh_token_expiry > NOW()
UNION ALL
SELECT 'Currently Locked Accounts', COUNT(*)::text 
FROM users WHERE is_account_locked = true AND lockout_end > NOW()
UNION ALL
SELECT 'Users with Failed Attempts', COUNT(*)::text 
FROM users WHERE failed_login_attempts > 0;
```

---

## ??? Maintenance Queries

### Cleanup Expired Tokens
```sql
-- Clean up expired email confirmation tokens
UPDATE users
SET email_confirmation_token = NULL,
    email_confirmation_token_expiry = NULL
WHERE email_confirmation_token IS NOT NULL
  AND email_confirmation_token_expiry < NOW();

-- Clean up expired password reset tokens
UPDATE users
SET password_reset_token = NULL,
    password_reset_token_expiry = NULL
WHERE password_reset_token IS NOT NULL
  AND password_reset_token_expiry < NOW();

-- Clean up expired refresh tokens
UPDATE users
SET refresh_token_hash = NULL,
    refresh_token_expiry = NULL
WHERE refresh_token_hash IS NOT NULL
  AND refresh_token_expiry < NOW();
```

### Auto-Unlock Expired Locks
```sql
UPDATE users
SET is_account_locked = false,
    lockout_end = NULL,
    failed_login_attempts = 0
WHERE is_account_locked = true
  AND lockout_end < NOW();
```

### Delete Unverified Old Accounts (7+ days)
```sql
DELETE FROM users
WHERE email_confirmed = false
  AND created_at < NOW() - INTERVAL '7 days';
```

---

## ?? Security Features

### Implemented in Database

? **Password Hashing** - BCrypt with work factor 12  
? **Token Expiration** - Automatic expiration for all tokens  
? **Account Lockout** - 5 failed attempts = 30-minute lock  
? **Email Verification** - Required before full access  
? **Refresh Token** - Hashed storage, 30-day expiration  
? **Single Use Tokens** - Tokens deleted after use  
? **Unique Constraints** - Email must be unique  

### Token Lifetimes

| Token Type | Lifetime | Field |
|------------|----------|-------|
| Email Confirmation | 24 hours | `email_confirmation_token_expiry` |
| Password Reset | 1 hour | `password_reset_token_expiry` |
| Refresh Token | 30 days | `refresh_token_expiry` |
| JWT Access Token | 1 hour | (Not stored in DB) |
| Account Lockout | 30 minutes | `lockout_end` |

---

## ?? View Commands

### Quick View (Essential Info)
```bash
"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@***REMOVED***:5432/wihngo_kzno?sslmode=require" -f Database\queries\quick_auth_view.sql
```

### Full Report (All Details)
```bash
"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@***REMOVED***:5432/wihngo_kzno?sslmode=require" -f Database\queries\view_auth_database.sql
```

### Or Double-Click
```
VIEW_AUTH_DATABASE.bat
```

---

## ?? Mobile Integration Notes

### What Mobile Needs to Know

1. **Email Confirmation**
   - Users cannot login until `email_confirmed = true`
   - Token expires in 24 hours
   - Provide "Resend Email" functionality

2. **Password Reset**
   - Token expires in 1 hour
   - Show countdown timer
   - All sessions revoked on password change

3. **Account Lockout**
   - After 5 failed attempts
   - Locked for 30 minutes
   - Show clear error message

4. **Session Management**
   - Refresh token lasts 30 days
   - Store refresh token securely
   - Use to get new JWT when expired

---

## ?? Example Data

### Sample User Entry
```json
{
  "user_id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe",
  "email": "john@example.com",
  "password_hash": "$2a$12$...", // BCrypt hash
  "email_confirmed": true,
  "email_confirmation_token": null,
  "email_confirmation_token_expiry": null,
  "password_reset_token": null,
  "password_reset_token_expiry": null,
  "refresh_token_hash": "$2a$12$...", // Hashed refresh token
  "refresh_token_expiry": "2025-02-15 10:30:00",
  "failed_login_attempts": 0,
  "is_account_locked": false,
  "lockout_end": null,
  "last_login_at": "2025-01-15 10:30:00",
  "last_password_change_at": "2025-01-10 14:20:00",
  "created_at": "2025-01-01 12:00:00",
  "updated_at": "2025-01-15 10:30:00"
}
```

---

## ?? Visual Database View

### Run These Commands

**Windows (Double-Click):**
```
VIEW_AUTH_DATABASE.bat
```

**Command Line:**
```cmd
cd C:\.net\Wihngo
set PGPASSWORD=***REMOVED***
"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@***REMOVED***:5432/wihngo_kzno?sslmode=require" -f Database\queries\view_auth_database.sql
```

This will display:
- Complete table schema
- All users with status
- Locked accounts
- Unconfirmed emails
- Active tokens
- Recent logins
- Statistics
- Health check

---

**Last Updated:** January 2025  
**Database:** PostgreSQL (Render)  
**Schema Version:** v1.0
