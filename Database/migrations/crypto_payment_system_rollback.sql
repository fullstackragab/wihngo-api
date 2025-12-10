-- =====================================================
-- ROLLBACK SCRIPT - Crypto Payment System
-- =====================================================
-- Use this script to remove all crypto payment tables
-- WARNING: This will delete all crypto payment data!
-- =====================================================

-- Drop triggers first
DROP TRIGGER IF EXISTS update_platform_wallets_updated_at ON platform_wallets;
DROP TRIGGER IF EXISTS update_crypto_payment_requests_updated_at ON crypto_payment_requests;
DROP TRIGGER IF EXISTS update_crypto_payment_methods_updated_at ON crypto_payment_methods;

-- Drop function
DROP FUNCTION IF EXISTS update_updated_at_column();

-- Drop tables in reverse order (respecting foreign key dependencies)
DROP TABLE IF EXISTS crypto_payment_methods CASCADE;
DROP TABLE IF EXISTS crypto_transactions CASCADE;
DROP TABLE IF EXISTS crypto_payment_requests CASCADE;
DROP TABLE IF EXISTS crypto_exchange_rates CASCADE;
DROP TABLE IF EXISTS platform_wallets CASCADE;

-- Verify tables were dropped
SELECT 
    tablename,
    schemaname
FROM pg_tables
WHERE tablename IN (
    'platform_wallets',
    'crypto_exchange_rates',
    'crypto_payment_requests',
    'crypto_transactions',
    'crypto_payment_methods'
)
ORDER BY tablename;

-- Should return 0 rows if all tables were successfully dropped
