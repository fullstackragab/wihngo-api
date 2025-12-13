-- ============================================
-- Verify Database Schema
-- ============================================
-- Run this to check if all required columns exist

\echo ''
\echo '========================================'
\echo 'CHECKING DATABASE SCHEMA'
\echo '========================================'
\echo ''

-- Check crypto_payment_requests.confirmations
\echo 'Checking crypto_payment_requests.confirmations...'
SELECT 
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'crypto_payment_requests' 
            AND column_name = 'confirmations'
        ) 
        THEN '? EXISTS'
        ELSE '? MISSING'
    END as status,
    'crypto_payment_requests.confirmations' as column_check;

-- Check token_configurations.token_address
\echo ''
\echo 'Checking token_configurations.token_address...'
SELECT 
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'token_configurations' 
            AND column_name = 'token_address'
        ) 
        THEN '? EXISTS'
        ELSE '? MISSING'
    END as status,
    'token_configurations.token_address' as column_check;

-- Check onchain_deposits.metadata
\echo ''
\echo 'Checking onchain_deposits.metadata...'
SELECT 
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'onchain_deposits' 
            AND column_name = 'metadata'
        ) 
        THEN '? EXISTS'
        ELSE '? MISSING'
    END as status,
    'onchain_deposits.metadata' as column_check;

-- Summary
\echo ''
\echo '========================================'
\echo 'SUMMARY'
\echo '========================================'

SELECT 
    COUNT(*) FILTER (WHERE column_name IN ('confirmations', 'token_address', 'metadata')) as found_columns,
    3 as required_columns,
    CASE 
        WHEN COUNT(*) FILTER (WHERE column_name IN ('confirmations', 'token_address', 'metadata')) = 3 
        THEN '? ALL COLUMNS EXIST'
        ELSE '? SOME COLUMNS MISSING'
    END as status
FROM information_schema.columns 
WHERE (table_name = 'crypto_payment_requests' AND column_name = 'confirmations')
   OR (table_name = 'token_configurations' AND column_name = 'token_address')
   OR (table_name = 'onchain_deposits' AND column_name = 'metadata');

\echo ''
\echo 'If columns are missing, run: .\run-fix-migration.ps1'
\echo ''
