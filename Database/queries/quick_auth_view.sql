-- ============================================
-- QUICK AUTH VIEW - Essential Information
-- ============================================

-- Show all users with their authentication status
SELECT 
    user_id,
    name,
    email,
    email_confirmed,
    created_at,
    last_login_at,
    failed_login_attempts,
    is_account_locked,
    CASE 
        WHEN lockout_end > NOW() THEN 'LOCKED UNTIL ' || TO_CHAR(lockout_end, 'YYYY-MM-DD HH24:MI')
        WHEN is_account_locked THEN 'Lock Expired'
        ELSE 'Not Locked'
    END as lock_status,
    CASE 
        WHEN email_confirmed THEN '? Confirmed'
        WHEN email_confirmation_token_expiry > NOW() THEN '? Pending (Token Valid)'
        WHEN email_confirmation_token_expiry IS NOT NULL THEN '? Pending (Token Expired)'
        ELSE '? Not Confirmed'
    END as email_status,
    CASE 
        WHEN password_reset_token_expiry > NOW() THEN '?? Reset Requested'
        ELSE 'No Active Reset'
    END as password_reset_status,
    CASE 
        WHEN refresh_token_expiry > NOW() THEN '? Active Session'
        ELSE 'No Active Session'
    END as session_status
FROM users
ORDER BY created_at DESC;
