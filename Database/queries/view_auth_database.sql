-- ============================================
-- VIEW ALL AUTHENTICATION DATA
-- ============================================
-- This script displays all authentication-related
-- database tables, columns, and data

\echo ''
\echo '========================================'
\echo 'AUTHENTICATION DATABASE OVERVIEW'
\echo '========================================'
\echo ''

-- ============================================
-- 1. USERS TABLE SCHEMA
-- ============================================
\echo ''
\echo '1. USERS TABLE SCHEMA'
\echo '----------------------------------------'
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;

-- ============================================
-- 2. USERS TABLE DATA
-- ============================================
\echo ''
\echo '2. ALL USERS (Basic Info)'
\echo '----------------------------------------'
SELECT 
    user_id,
    name,
    email,
    email_confirmed,
    created_at,
    last_login_at,
    failed_login_attempts,
    is_account_locked,
    lockout_end
FROM users
ORDER BY created_at DESC;

-- ============================================
-- 3. AUTHENTICATION SECURITY FIELDS
-- ============================================
\echo ''
\echo '3. AUTHENTICATION SECURITY STATUS'
\echo '----------------------------------------'
SELECT 
    user_id,
    email,
    email_confirmed,
    email_confirmation_token IS NOT NULL as has_confirmation_token,
    email_confirmation_token_expiry,
    password_reset_token IS NOT NULL as has_reset_token,
    password_reset_token_expiry,
    refresh_token_hash IS NOT NULL as has_refresh_token,
    refresh_token_expiry,
    last_password_change_at,
    failed_login_attempts,
    is_account_locked,
    lockout_end
FROM users
ORDER BY created_at DESC;

-- ============================================
-- 4. LOCKED ACCOUNTS
-- ============================================
\echo ''
\echo '4. LOCKED ACCOUNTS'
\echo '----------------------------------------'
SELECT 
    user_id,
    email,
    name,
    failed_login_attempts,
    lockout_end,
    CASE 
        WHEN lockout_end > NOW() THEN 'Currently Locked'
        ELSE 'Lock Expired'
    END as lock_status,
    EXTRACT(EPOCH FROM (lockout_end - NOW()))/60 as minutes_until_unlock
FROM users
WHERE is_account_locked = true
ORDER BY lockout_end DESC;

-- ============================================
-- 5. UNCONFIRMED EMAIL ADDRESSES
-- ============================================
\echo ''
\echo '5. UNCONFIRMED EMAIL ADDRESSES'
\echo '----------------------------------------'
SELECT 
    user_id,
    email,
    name,
    created_at,
    email_confirmation_token_expiry,
    CASE 
        WHEN email_confirmation_token_expiry > NOW() THEN 'Token Valid'
        WHEN email_confirmation_token_expiry IS NULL THEN 'No Token'
        ELSE 'Token Expired'
    END as token_status,
    EXTRACT(EPOCH FROM (NOW() - created_at))/3600 as hours_since_registration
FROM users
WHERE email_confirmed = false
ORDER BY created_at DESC;

-- ============================================
-- 6. ACTIVE PASSWORD RESET REQUESTS
-- ============================================
\echo ''
\echo '6. ACTIVE PASSWORD RESET REQUESTS'
\echo '----------------------------------------'
SELECT 
    user_id,
    email,
    name,
    password_reset_token_expiry,
    EXTRACT(EPOCH FROM (password_reset_token_expiry - NOW()))/60 as minutes_until_expiry,
    CASE 
        WHEN password_reset_token_expiry > NOW() THEN 'Valid'
        ELSE 'Expired'
    END as token_status
FROM users
WHERE password_reset_token IS NOT NULL
ORDER BY password_reset_token_expiry DESC;

-- ============================================
-- 7. ACTIVE REFRESH TOKENS
-- ============================================
\echo ''
\echo '7. ACTIVE REFRESH TOKENS'
\echo '----------------------------------------'
SELECT 
    user_id,
    email,
    name,
    last_login_at,
    refresh_token_expiry,
    EXTRACT(EPOCH FROM (refresh_token_expiry - NOW()))/86400 as days_until_expiry,
    CASE 
        WHEN refresh_token_expiry > NOW() THEN 'Valid'
        ELSE 'Expired'
    END as token_status
FROM users
WHERE refresh_token_hash IS NOT NULL
ORDER BY refresh_token_expiry DESC;

-- ============================================
-- 8. RECENT LOGINS
-- ============================================
\echo ''
\echo '8. RECENT LOGINS (Last 7 Days)'
\echo '----------------------------------------'
SELECT 
    user_id,
    email,
    name,
    last_login_at,
    email_confirmed,
    failed_login_attempts,
    EXTRACT(EPOCH FROM (NOW() - last_login_at))/3600 as hours_since_login
FROM users
WHERE last_login_at IS NOT NULL
  AND last_login_at > NOW() - INTERVAL '7 days'
ORDER BY last_login_at DESC;

-- ============================================
-- 9. USER REGISTRATION STATISTICS
-- ============================================
\echo ''
\echo '9. USER REGISTRATION STATISTICS'
\echo '----------------------------------------'
SELECT 
    COUNT(*) as total_users,
    COUNT(*) FILTER (WHERE email_confirmed = true) as confirmed_users,
    COUNT(*) FILTER (WHERE email_confirmed = false) as unconfirmed_users,
    COUNT(*) FILTER (WHERE is_account_locked = true) as locked_accounts,
    COUNT(*) FILTER (WHERE failed_login_attempts > 0) as users_with_failed_attempts,
    COUNT(*) FILTER (WHERE last_login_at IS NOT NULL) as users_who_logged_in,
    COUNT(*) FILTER (WHERE created_at > NOW() - INTERVAL '24 hours') as registered_last_24h,
    COUNT(*) FILTER (WHERE created_at > NOW() - INTERVAL '7 days') as registered_last_7d
FROM users;

-- ============================================
-- 10. AUTHENTICATION HEALTH CHECK
-- ============================================
\echo ''
\echo '10. AUTHENTICATION HEALTH CHECK'
\echo '----------------------------------------'
SELECT 
    'Total Users' as metric,
    COUNT(*)::text as value
FROM users
UNION ALL
SELECT 
    'Confirmed Emails',
    COUNT(*)::text
FROM users WHERE email_confirmed = true
UNION ALL
SELECT 
    'Unconfirmed Emails',
    COUNT(*)::text
FROM users WHERE email_confirmed = false
UNION ALL
SELECT 
    'Expired Confirmation Tokens',
    COUNT(*)::text
FROM users 
WHERE email_confirmed = false 
  AND email_confirmation_token_expiry < NOW()
UNION ALL
SELECT 
    'Active Password Reset Requests',
    COUNT(*)::text
FROM users 
WHERE password_reset_token IS NOT NULL 
  AND password_reset_token_expiry > NOW()
UNION ALL
SELECT 
    'Active Refresh Tokens',
    COUNT(*)::text
FROM users 
WHERE refresh_token_hash IS NOT NULL 
  AND refresh_token_expiry > NOW()
UNION ALL
SELECT 
    'Locked Accounts',
    COUNT(*)::text
FROM users 
WHERE is_account_locked = true 
  AND lockout_end > NOW()
UNION ALL
SELECT 
    'Users with Failed Login Attempts',
    COUNT(*)::text
FROM users 
WHERE failed_login_attempts > 0;

-- ============================================
-- 11. AUTHENTICATION COLUMNS CHECK
-- ============================================
\echo ''
\echo '11. AUTHENTICATION COLUMNS CHECK'
\echo '----------------------------------------'
SELECT 
    column_name,
    CASE 
        WHEN column_name IN (
            'email', 'password_hash', 'email_confirmed', 
            'email_confirmation_token', 'email_confirmation_token_expiry',
            'password_reset_token', 'password_reset_token_expiry',
            'refresh_token_hash', 'refresh_token_expiry',
            'failed_login_attempts', 'is_account_locked', 'lockout_end',
            'last_login_at', 'last_password_change_at'
        ) THEN '? PRESENT'
        ELSE ''
    END as auth_field,
    data_type
FROM information_schema.columns
WHERE table_name = 'users'
  AND column_name IN (
      'email', 'password_hash', 'email_confirmed', 
      'email_confirmation_token', 'email_confirmation_token_expiry',
      'password_reset_token', 'password_reset_token_expiry',
      'refresh_token_hash', 'refresh_token_expiry',
      'failed_login_attempts', 'is_account_locked', 'lockout_end',
      'last_login_at', 'last_password_change_at'
  )
ORDER BY column_name;

-- ============================================
-- 12. SAMPLE USER DETAILS (if any exist)
-- ============================================
\echo ''
\echo '12. SAMPLE USER DETAILS'
\echo '----------------------------------------'
SELECT 
    user_id,
    name,
    email,
    email_confirmed,
    created_at,
    last_login_at,
    last_password_change_at,
    failed_login_attempts,
    is_account_locked,
    CASE 
        WHEN email_confirmation_token IS NOT NULL THEN 'Has Token'
        ELSE 'No Token'
    END as confirmation_token_status,
    CASE 
        WHEN password_reset_token IS NOT NULL THEN 'Has Token'
        ELSE 'No Token'
    END as reset_token_status,
    CASE 
        WHEN refresh_token_hash IS NOT NULL THEN 'Has Token'
        ELSE 'No Token'
    END as refresh_token_status
FROM users
ORDER BY created_at DESC
LIMIT 5;

-- ============================================
-- 13. DATABASE SIZE AND ROW COUNT
-- ============================================
\echo ''
\echo '13. USERS TABLE SIZE'
\echo '----------------------------------------'
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    n_tup_ins AS inserts,
    n_tup_upd AS updates,
    n_tup_del AS deletes,
    n_live_tup AS live_rows
FROM pg_stat_user_tables
WHERE tablename = 'users';

\echo ''
\echo '========================================'
\echo 'END OF AUTHENTICATION DATABASE REPORT'
\echo '========================================'
\echo ''
