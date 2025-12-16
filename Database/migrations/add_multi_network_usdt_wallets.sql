-- =====================================================
-- Remove Multi-Network Support - Solana Only
-- =====================================================
-- This migration deactivates all non-Solana wallets
-- Only USDC and EURC on Solana network are supported
-- =====================================================

-- Deactivate all USDT wallets (USDT is no longer supported)
UPDATE platform_wallets
SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP
WHERE currency = 'USDT';

-- Deactivate all non-Solana network wallets
UPDATE platform_wallets
SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP
WHERE network != 'solana';

-- =====================================================
-- VERIFICATION
-- =====================================================

-- Check all active wallets (should only be USDC and EURC on Solana)
SELECT 
    currency,
    network,
    address,
    is_active,
    created_at
FROM platform_wallets
WHERE is_active = TRUE
ORDER BY currency, network;

-- Expected result: 2 rows (USDC on solana, EURC on solana)

-- Check deactivated wallets
SELECT 
    currency,
    network,
    COUNT(*) as count
FROM platform_wallets
WHERE is_active = FALSE
GROUP BY currency, network
ORDER BY currency, network;
