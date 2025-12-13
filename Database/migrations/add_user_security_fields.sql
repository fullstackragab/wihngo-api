-- Add security fields to users table for enhanced authentication
-- Run this migration to add email confirmation, account lockout, and refresh token support

-- Add email confirmation fields
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS email_confirmed BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS email_confirmation_token VARCHAR(500),
ADD COLUMN IF NOT EXISTS email_confirmation_token_expiry TIMESTAMP;

-- Add account lockout fields
ALTER TABLE users
ADD COLUMN IF NOT EXISTS is_account_locked BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS failed_login_attempts INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS lockout_end TIMESTAMP;

-- Add refresh token fields
ALTER TABLE users
ADD COLUMN IF NOT EXISTS last_login_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS refresh_token_hash VARCHAR(500),
ADD COLUMN IF NOT EXISTS refresh_token_expiry TIMESTAMP;

-- Add password reset fields
ALTER TABLE users
ADD COLUMN IF NOT EXISTS password_reset_token VARCHAR(500),
ADD COLUMN IF NOT EXISTS password_reset_token_expiry TIMESTAMP,
ADD COLUMN IF NOT EXISTS last_password_change_at TIMESTAMP;

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_users_email_confirmation_token 
ON users(email_confirmation_token) 
WHERE email_confirmation_token IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_users_password_reset_token 
ON users(password_reset_token) 
WHERE password_reset_token IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_users_refresh_token_hash 
ON users(refresh_token_hash) 
WHERE refresh_token_hash IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_users_lockout_end 
ON users(lockout_end) 
WHERE is_account_locked = TRUE AND lockout_end IS NOT NULL;

-- Add comments
COMMENT ON COLUMN users.email_confirmed IS 'Indicates if the user has confirmed their email address';
COMMENT ON COLUMN users.email_confirmation_token IS 'Token used for email confirmation';
COMMENT ON COLUMN users.email_confirmation_token_expiry IS 'Expiry time for email confirmation token';
COMMENT ON COLUMN users.is_account_locked IS 'Indicates if the account is locked due to failed login attempts';
COMMENT ON COLUMN users.failed_login_attempts IS 'Number of consecutive failed login attempts';
COMMENT ON COLUMN users.lockout_end IS 'Time when the account lockout expires';
COMMENT ON COLUMN users.last_login_at IS 'Timestamp of the last successful login';
COMMENT ON COLUMN users.refresh_token_hash IS 'Hashed refresh token for token renewal';
COMMENT ON COLUMN users.refresh_token_expiry IS 'Expiry time for the refresh token';
COMMENT ON COLUMN users.password_reset_token IS 'Token used for password reset';
COMMENT ON COLUMN users.password_reset_token_expiry IS 'Expiry time for password reset token';
COMMENT ON COLUMN users.last_password_change_at IS 'Timestamp of the last password change';

-- Display success message
DO $$
BEGIN
    RAISE NOTICE 'Security fields added to users table successfully';
    RAISE NOTICE 'The following fields were added:';
    RAISE NOTICE '  - Email confirmation: email_confirmed, email_confirmation_token, email_confirmation_token_expiry';
    RAISE NOTICE '  - Account lockout: is_account_locked, failed_login_attempts, lockout_end';
    RAISE NOTICE '  - Refresh tokens: last_login_at, refresh_token_hash, refresh_token_expiry';
    RAISE NOTICE '  - Password reset: password_reset_token, password_reset_token_expiry, last_password_change_at';
    RAISE NOTICE '  - Performance indexes created for token lookups';
END $$;
