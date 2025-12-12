-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================
-- Run these AFTER the migration to verify success
-- =====================================================

-- 1. Check all active wallets (should return 10 rows)
SELECT 
    currency,
    network,
    address,
    is_active,
    created_at
FROM platform_wallets
WHERE is_active = TRUE
ORDER BY currency, network;

-- 2. Count by currency (should show EURC: 5, USDC: 5)
SELECT 
    currency,
    COUNT(*) as wallet_count
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;

-- 3. Show networks per currency
SELECT 
    currency,
    STRING_AGG(network, ', ' ORDER BY network) as supported_networks
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;

-- 4. Check for any inactive wallets
SELECT 
    currency,
    network,
    address,
    is_active
FROM platform_wallets
WHERE is_active = FALSE
ORDER BY currency, network;

-- Expected Results:
-- Query 1: 10 active wallets
-- Query 2: EURC: 5, USDC: 5
-- Query 3: Both currencies support: base, ethereum, polygon, solana, stellar
-- Query 4: Old wallets (USDT, ETH, BNB, tron, BSC, avalanche) should show as inactive
