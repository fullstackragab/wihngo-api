-- =====================================================
-- Add All Supported Crypto Wallets - WIHNGO PLATFORM
-- =====================================================
-- This migration adds wallet addresses for all supported
-- currency and network combinations:
-- - USDC: Solana, Ethereum, Polygon, Base, Stellar
-- - EURC: Solana, Ethereum, Polygon, Base, Stellar
-- Total: 10 wallet entries (2 currencies × 5 networks)
-- =====================================================

-- First, deactivate old wallets (USDT, ETH, BNB, Tron, BSC, Avalanche)
UPDATE platform_wallets
SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP
WHERE currency IN ('USDT', 'ETH', 'BNB')
   OR network IN ('tron', 'binance-smart-chain', 'avalanche');

-- USDC Wallets (5 networks) - Wihngo Actual Addresses
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES 
  (gen_random_uuid(), 'USDC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'ethereum', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'polygon', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'base', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'stellar', 'GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO UPDATE 
  SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

-- EURC Wallets (5 networks) - Wihngo Actual Addresses
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES 
  (gen_random_uuid(), 'EURC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'ethereum', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'polygon', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'base', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'stellar', 'GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO UPDATE 
  SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

-- =====================================================
-- VERIFICATION
-- =====================================================

-- Check all active wallets by currency
SELECT 
    currency,
    network,
    address,
    is_active,
    created_at
FROM platform_wallets
WHERE is_active = TRUE
ORDER BY currency, network;

-- Expected result: 10 rows total
--   EURC: base, ethereum, polygon, solana, stellar (5)
--   USDC: base, ethereum, polygon, solana, stellar (5)

-- Count by currency
SELECT currency, COUNT(*) as wallet_count
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;

-- Expected:
--   EURC: 5
--   USDC: 5

-- Verify the supported combinations match new requirements
SELECT 
    currency,
    STRING_AGG(network, ', ' ORDER BY network) as supported_networks
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;

-- Expected output:
--   EURC: base, ethereum, polygon, solana, stellar
--   USDC: base, ethereum, polygon, solana, stellar

