-- ====================================================
-- SEPOLIA PAYMENT DIAGNOSTIC QUERIES
-- ====================================================
-- Use these queries to check your Sepolia payment status

-- ====================================================
-- 1. CHECK LATEST SEPOLIA PAYMENTS
-- ====================================================
SELECT 
    id,
    user_id,
    bird_id,
    status,
    currency,
    network,
    amount_crypto,
    amount_usd,
    transaction_hash,
    confirmations,
    required_confirmations,
    created_at,
    updated_at,
    confirmed_at,
    completed_at,
    expires_at,
    CASE 
        WHEN expires_at < NOW() THEN '? EXPIRED'
        WHEN status = 'completed' THEN '? COMPLETED'
        WHEN status = 'confirmed' THEN '?? CONFIRMED (needs completion)'
        WHEN status = 'confirming' THEN '? CONFIRMING'
        WHEN status = 'pending' THEN '?? PENDING'
        ELSE '? UNKNOWN'
    END AS status_icon
FROM crypto_payment_requests
WHERE network = 'sepolia'
ORDER BY created_at DESC
LIMIT 10;

-- ====================================================
-- 2. CHECK SPECIFIC PAYMENT (replace with your payment ID)
-- ====================================================
-- SELECT * FROM crypto_payment_requests
-- WHERE id = 'your-payment-id-here';

-- ====================================================
-- 3. CHECK PAYMENTS STUCK IN CONFIRMED STATUS
-- ====================================================
SELECT 
    id,
    status,
    confirmations,
    required_confirmations,
    transaction_hash,
    confirmed_at,
    completed_at,
    created_at,
    (NOW() - confirmed_at) AS time_since_confirmed
FROM crypto_payment_requests
WHERE network = 'sepolia'
  AND status = 'confirmed'
  AND completed_at IS NULL
ORDER BY confirmed_at DESC;

-- ====================================================
-- 4. CHECK PAYMENT CONFIRMATION PROGRESS
-- ====================================================
SELECT 
    id,
    status,
    transaction_hash,
    confirmations,
    required_confirmations,
    ROUND((confirmations::NUMERIC / required_confirmations::NUMERIC) * 100, 2) AS completion_percentage,
    CASE 
        WHEN confirmations >= required_confirmations THEN '? Ready'
        ELSE '? Waiting'
    END AS ready_to_complete,
    created_at,
    updated_at
FROM crypto_payment_requests
WHERE network = 'sepolia'
  AND status IN ('confirming', 'confirmed')
  AND expires_at > NOW()
ORDER BY created_at DESC;

-- ====================================================
-- 5. CHECK IF PREMIUM SUBSCRIPTION WAS CREATED
-- ====================================================
SELECT 
    pr.id AS payment_id,
    pr.status AS payment_status,
    pr.bird_id,
    pr.completed_at AS payment_completed_at,
    ps.id AS subscription_id,
    ps.plan,
    ps.status AS subscription_status,
    ps.start_date,
    ps.end_date,
    ps.is_active
FROM crypto_payment_requests pr
LEFT JOIN bird_premium_subscriptions ps ON pr.bird_id = ps.bird_id
WHERE pr.network = 'sepolia'
  AND pr.status = 'completed'
ORDER BY pr.completed_at DESC
LIMIT 5;

-- ====================================================
-- 6. CHECK PAYMENTS WITHOUT TRANSACTION HASH
-- ====================================================
SELECT 
    id,
    status,
    currency,
    network,
    wallet_address,
    amount_crypto,
    created_at,
    expires_at,
    (expires_at - NOW()) AS time_remaining
FROM crypto_payment_requests
WHERE network = 'sepolia'
  AND status = 'pending'
  AND transaction_hash IS NULL
  AND expires_at > NOW()
ORDER BY created_at DESC;

-- ====================================================
-- 7. CHECK ALL PAYMENT STATUSES (SUMMARY)
-- ====================================================
SELECT 
    status,
    COUNT(*) AS count,
    SUM(amount_usd) AS total_usd
FROM crypto_payment_requests
WHERE network = 'sepolia'
GROUP BY status
ORDER BY count DESC;

-- ====================================================
-- 8. CHECK RECENT PAYMENT STATUS CHANGES
-- ====================================================
SELECT 
    id,
    status,
    transaction_hash,
    confirmations,
    required_confirmations,
    created_at,
    updated_at,
    confirmed_at,
    completed_at,
    (updated_at - created_at) AS time_to_update,
    CASE 
        WHEN completed_at IS NOT NULL THEN (completed_at - created_at)
        ELSE NULL
    END AS time_to_complete
FROM crypto_payment_requests
WHERE network = 'sepolia'
  AND created_at > NOW() - INTERVAL '1 day'
ORDER BY created_at DESC;

-- ====================================================
-- 9. FORCE CHECK: Get Payment Info for Manual API Call
-- ====================================================
-- Copy this output to use in manual API call
SELECT 
    id AS payment_id,
    'POST /api/payments/crypto/' || id || '/check-status' AS api_endpoint,
    status AS current_status,
    transaction_hash,
    confirmations || '/' || required_confirmations AS confirmations_progress
FROM crypto_payment_requests
WHERE network = 'sepolia'
  AND status IN ('confirming', 'confirmed')
ORDER BY created_at DESC
LIMIT 1;

-- ====================================================
-- 10. CHECK SEPOLIA WALLET CONFIGURATION
-- ====================================================
SELECT 
    id,
    currency,
    network,
    address,
    is_active,
    created_at
FROM platform_wallets
WHERE network = 'sepolia';

-- ====================================================
-- NOTES:
-- ====================================================
-- - Payments stuck in "confirmed" status need backend job to run
-- - Backend job (PaymentMonitorJob) runs every 30 seconds
-- - Required confirmations for Sepolia: 6 blocks (~1.2 minutes)
-- - Payments expire after 30 minutes
-- - Check Sepolia Etherscan for transaction confirmations:
--   https://sepolia.etherscan.io/tx/{your-transaction-hash}
