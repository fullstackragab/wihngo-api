-- =====================================================
-- Clear/Reset Old Crypto Payment Data
-- =====================================================
-- This script clears old payment data from the crypto payment system
-- Use with caution - this will delete payment records!
-- =====================================================

-- Option 1: Clear ALL payment data (USE WITH EXTREME CAUTION!)
-- Uncomment the following lines to delete ALL payment records

/*
BEGIN;

-- Delete all crypto transactions (child records first)
DELETE FROM crypto_transactions;

-- Delete all crypto payment requests
DELETE FROM crypto_payment_requests;

-- Delete all user payment methods
DELETE FROM crypto_payment_methods;

COMMIT;

SELECT 'All payment data cleared successfully!' as status;
*/

-- =====================================================
-- Option 2: Clear only old/expired payments (RECOMMENDED)
-- =====================================================
-- This removes payments older than 30 days and expired payments

BEGIN;

-- Delete transactions associated with old payment requests
DELETE FROM crypto_transactions
WHERE payment_request_id IN (
    SELECT id FROM crypto_payment_requests
    WHERE created_at < NOW() - INTERVAL '30 days'
    OR status = 'expired'
);

-- Delete old and expired payment requests
DELETE FROM crypto_payment_requests
WHERE created_at < NOW() - INTERVAL '30 days'
OR status = 'expired';

COMMIT;

SELECT 'Old and expired payment data cleared successfully!' as status;

-- =====================================================
-- Option 3: Clear only failed/cancelled payments
-- =====================================================
-- Uncomment to clear failed and cancelled payments only

/*
BEGIN;

-- Delete transactions for failed/cancelled payments
DELETE FROM crypto_transactions
WHERE payment_request_id IN (
    SELECT id FROM crypto_payment_requests
    WHERE status IN ('failed', 'cancelled', 'expired')
);

-- Delete failed/cancelled payment requests
DELETE FROM crypto_payment_requests
WHERE status IN ('failed', 'cancelled', 'expired');

COMMIT;

SELECT 'Failed and cancelled payment data cleared successfully!' as status;
*/

-- =====================================================
-- Option 4: Update old pending payments to expired
-- =====================================================
-- This marks old pending payments as expired instead of deleting them

/*
BEGIN;

UPDATE crypto_payment_requests
SET status = 'expired',
    updated_at = NOW()
WHERE status = 'pending'
AND expires_at < NOW();

COMMIT;

SELECT 'Old pending payments marked as expired!' as status;
*/

-- =====================================================
-- Verification Queries
-- =====================================================

-- Check remaining payment counts by status
SELECT 
    status,
    COUNT(*) as count,
    MIN(created_at) as oldest,
    MAX(created_at) as newest
FROM crypto_payment_requests
GROUP BY status
ORDER BY status;

-- Check total counts
SELECT 
    'crypto_payment_requests' as table_name, 
    COUNT(*) as row_count 
FROM crypto_payment_requests
UNION ALL
SELECT 
    'crypto_transactions', 
    COUNT(*) 
FROM crypto_transactions
UNION ALL
SELECT 
    'crypto_payment_methods', 
    COUNT(*) 
FROM crypto_payment_methods;

-- Show recent payment activity (last 7 days)
SELECT 
    DATE(created_at) as date,
    status,
    COUNT(*) as count
FROM crypto_payment_requests
WHERE created_at >= NOW() - INTERVAL '7 days'
GROUP BY DATE(created_at), status
ORDER BY date DESC, status;

-- Show payments that will be affected by cleanup (preview)
SELECT 
    id,
    user_id,
    amount_usd,
    currency,
    status,
    created_at,
    expires_at,
    CASE 
        WHEN status = 'expired' THEN 'Already expired'
        WHEN created_at < NOW() - INTERVAL '30 days' THEN 'Older than 30 days'
        WHEN status = 'pending' AND expires_at < NOW() THEN 'Pending but expired'
        ELSE 'Will be kept'
    END as cleanup_reason
FROM crypto_payment_requests
WHERE status = 'expired'
   OR created_at < NOW() - INTERVAL '30 days'
   OR (status = 'pending' AND expires_at < NOW())
ORDER BY created_at DESC
LIMIT 20;
