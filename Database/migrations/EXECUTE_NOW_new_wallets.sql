-- =====================================================
-- QUICK EXECUTE: Wihngo Crypto Wallet Configuration
-- =====================================================
-- Run this SQL to update to the Wihngo system:
-- - 2 currencies: USDC, EURC
-- - 5 networks: Solana, Ethereum, Polygon, Base, Stellar
-- - Total: 10 active wallets
-- =====================================================

BEGIN;

-- Step 1: Deactivate old wallets
UPDATE platform_wallets
SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP
WHERE currency IN ('USDT', 'ETH', 'BNB')
   OR network IN ('tron', 'binance-smart-chain', 'avalanche');

-- Step 2: Insert USDC wallets (5 networks) - Wihngo Actual Addresses
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES 
  (gen_random_uuid(), 'USDC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'ethereum', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'polygon', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'base', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'USDC', 'stellar', 'GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO UPDATE 
  SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

-- Step 3: Insert EURC wallets (5 networks) - Wihngo Actual Addresses
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES 
  (gen_random_uuid(), 'EURC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'ethereum', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'polygon', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'base', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'EURC', 'stellar', 'GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO UPDATE 
  SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

-- Step 4: Verify results
SELECT 
    '? Active Wallets:' as status,
    currency,
    network,
    address
FROM platform_wallets
WHERE is_active = TRUE
ORDER BY currency, network;

SELECT 
    '?? Summary:' as status,
    currency,
    COUNT(*) as wallet_count
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;

COMMIT;

-- Expected output:
-- EURC: 5 wallets (base, ethereum, polygon, solana, stellar)
-- USDC: 5 wallets (base, ethereum, polygon, solana, stellar)
-- Total: 10 active wallets
