-- =====================================================
-- COPY & PASTE THIS INTO YOUR DATABASE
-- =====================================================

BEGIN;

UPDATE platform_wallets SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP WHERE currency IN ('USDT', 'ETH', 'BNB') OR network IN ('tron', 'binance-smart-chain', 'avalanche');

INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at) VALUES (gen_random_uuid(), 'USDC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'USDC', 'ethereum', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'USDC', 'polygon', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'USDC', 'base', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'USDC', 'stellar', 'GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) ON CONFLICT (currency, network, address) DO UPDATE SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at) VALUES (gen_random_uuid(), 'EURC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'EURC', 'ethereum', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'EURC', 'polygon', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'EURC', 'base', '0xfcc173a7569492439ec3df467d0ec0c05c0f541c', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP), (gen_random_uuid(), 'EURC', 'stellar', 'GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) ON CONFLICT (currency, network, address) DO UPDATE SET is_active = TRUE, updated_at = CURRENT_TIMESTAMP;

COMMIT;

SELECT currency, network, address FROM platform_wallets WHERE is_active = TRUE ORDER BY currency, network;
