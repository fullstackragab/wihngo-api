-- =====================================================
-- Add Multi-Network USDT Wallet Support
-- =====================================================
-- This migration adds Ethereum and Binance Smart Chain
-- wallet addresses for USDT payments
-- =====================================================

-- Insert Ethereum USDT wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'USDT',
    'ethereum',
    '0x4cc28f4cea7b440858b903b5c46685cb1478cdc4',
    TRUE,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (currency, network, address) DO NOTHING;

-- Insert Binance Smart Chain USDT wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'USDT',
    'binance-smart-chain',
    '0x83675000ac9915614afff618906421a2baea0020',
    TRUE,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (currency, network, address) DO NOTHING;

-- =====================================================
-- VERIFICATION
-- =====================================================

-- Check all USDT wallets
SELECT 
    currency,
    network,
    address,
    is_active,
    created_at
FROM platform_wallets
WHERE currency = 'USDT'
ORDER BY network;

-- Expected result: 3 rows (tron, ethereum, binance-smart-chain)
