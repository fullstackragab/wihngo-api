-- SQL Script to Add Sepolia Wallet to Database
-- Run this in your PostgreSQL database

-- Insert Sepolia Testnet Wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'ETH',
    'sepolia',
    '0x4cc28f4cea7b440858b903b5c46685cb1478cdc4',
    true,
    NOW(),
    NOW()
)
ON CONFLICT (currency, network, address) DO NOTHING;

-- Verify the wallet was inserted
SELECT * FROM platform_wallets WHERE network = 'sepolia';

-- Also ensure ETH exchange rate exists
INSERT INTO crypto_exchange_rates (id, currency, usd_rate, source, last_updated)
VALUES (
    gen_random_uuid(),
    'ETH',
    3000.00,
    'coingecko',
    NOW()
)
ON CONFLICT (currency) DO UPDATE SET 
    usd_rate = 3000.00,
    last_updated = NOW();

-- Verify exchange rate
SELECT * FROM crypto_exchange_rates WHERE currency = 'ETH';
