-- =====================================================
-- Add Solana Crypto Wallets - WIHNGO PLATFORM
-- =====================================================
-- This migration adds wallet addresses for:
-- - USDC on Solana
-- - EURC on Solana
-- Total: 2 wallet entries (2 currencies on 1 network)
-- =====================================================

-- First, deactivate old wallets (all non-Solana networks)
UPDATE platform_wallets
SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP
WHERE network != 'solana';

-- USDC on Solana - Wihngo Actual Address
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES 
  (gen_random_uuid(), 'USDC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO UPDATE 
  SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

-- EURC on Solana - Wihngo Actual Address
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES 
  (gen_random_uuid(), 'EURC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO UPDATE 
  SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

-- =====================================================
-- VERIFICATION
-- =====================================================

-- Check all active wallets
SELECT 
    currency,
    network,
    address,
    is_active,
    created_at
FROM platform_wallets
WHERE is_active = TRUE
ORDER BY currency, network;

-- Expected result: 2 rows total
--   EURC: solana
--   USDC: solana

-- Count by currency
SELECT currency, COUNT(*) as wallet_count
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;

-- Expected:
--   EURC: 1
--   USDC: 1

-- Verify the supported combinations match new requirements
SELECT 
    currency,
    STRING_AGG(network, ', ' ORDER BY network) as supported_networks
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;

-- Expected output:
--   EURC: solana
--   USDC: solana

