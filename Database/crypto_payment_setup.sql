-- =====================================================
-- Wihngo Crypto Payment Database Setup Script
-- PostgreSQL 16+
-- =====================================================

-- Create database (if not exists)
-- Run this separately if needed:
-- CREATE DATABASE wihngo;

-- Connect to wihngo database before running rest of script

-- =====================================================
-- 1. CREATE CRYPTO PAYMENT TABLES
-- =====================================================

-- Platform Wallets (Wihngo's receiving addresses)
CREATE TABLE IF NOT EXISTS platform_wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    address VARCHAR(255) NOT NULL,
    private_key_encrypted TEXT,
    derivation_path VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(currency, network, address)
);

-- Crypto Payment Requests
CREATE TABLE IF NOT EXISTS crypto_payment_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    bird_id UUID,
    amount_usd DECIMAL(10,2) NOT NULL,
    amount_crypto DECIMAL(20,10) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    exchange_rate DECIMAL(20,2) NOT NULL,
    wallet_address VARCHAR(255) NOT NULL,
    user_wallet_address VARCHAR(255),
    qr_code_data TEXT NOT NULL,
    payment_uri TEXT NOT NULL,
    transaction_hash VARCHAR(255),
    confirmations INT DEFAULT 0,
    required_confirmations INT NOT NULL,
    status VARCHAR(20) DEFAULT 'pending',
    purpose VARCHAR(50) NOT NULL,
    plan VARCHAR(20),
    metadata JSONB,
    expires_at TIMESTAMP NOT NULL,
    confirmed_at TIMESTAMP,
    completed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Crypto Transactions (blockchain transaction records)
CREATE TABLE IF NOT EXISTS crypto_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_request_id UUID REFERENCES crypto_payment_requests(id),
    transaction_hash VARCHAR(255) UNIQUE NOT NULL,
    from_address VARCHAR(255) NOT NULL,
    to_address VARCHAR(255) NOT NULL,
    amount DECIMAL(20,10) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    confirmations INT DEFAULT 0,
    block_number BIGINT,
    block_hash VARCHAR(255),
    fee DECIMAL(20,10),
    status VARCHAR(20) DEFAULT 'pending',
    detected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    confirmed_at TIMESTAMP
);

-- Exchange Rates (cached rates from CoinGecko)
CREATE TABLE IF NOT EXISTS crypto_exchange_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    currency VARCHAR(10) UNIQUE NOT NULL,
    usd_rate DECIMAL(20,2) NOT NULL,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    source VARCHAR(50) NOT NULL
);

-- User Saved Wallet Addresses (optional feature)
CREATE TABLE IF NOT EXISTS crypto_payment_methods (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    wallet_address VARCHAR(255) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    label VARCHAR(100),
    is_default BOOLEAN DEFAULT false,
    verified BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, wallet_address, currency, network)
);

-- =====================================================
-- 2. CREATE INDEXES FOR PERFORMANCE
-- =====================================================

-- Payment Requests Indexes
CREATE INDEX IF NOT EXISTS idx_crypto_payments_user ON crypto_payment_requests(user_id);
CREATE INDEX IF NOT EXISTS idx_crypto_payments_status ON crypto_payment_requests(status);
CREATE INDEX IF NOT EXISTS idx_crypto_payments_tx_hash ON crypto_payment_requests(transaction_hash);
CREATE INDEX IF NOT EXISTS idx_crypto_payments_expires ON crypto_payment_requests(expires_at);
CREATE INDEX IF NOT EXISTS idx_crypto_payments_created ON crypto_payment_requests(created_at);

-- Transactions Indexes
CREATE INDEX IF NOT EXISTS idx_crypto_tx_payment ON crypto_transactions(payment_request_id);
CREATE INDEX IF NOT EXISTS idx_crypto_tx_hash ON crypto_transactions(transaction_hash);
CREATE INDEX IF NOT EXISTS idx_crypto_tx_status ON crypto_transactions(status);

-- Payment Methods Index
CREATE INDEX IF NOT EXISTS idx_crypto_methods_user ON crypto_payment_methods(user_id);

-- =====================================================
-- 3. INSERT SEED DATA
-- =====================================================

-- Insert Primary TRON USDT Wallet
INSERT INTO platform_wallets (currency, network, address, is_active, created_at, updated_at)
VALUES ('USDT', 'tron', 'TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA', true, NOW(), NOW())
ON CONFLICT (currency, network, address) DO NOTHING;

-- Insert Exchange Rates (initial values - will be updated by background job)
INSERT INTO crypto_exchange_rates (currency, usd_rate, source, last_updated) VALUES
    ('BTC', 50000.00, 'coingecko', NOW()),
    ('ETH', 3000.00, 'coingecko', NOW()),
    ('USDT', 1.00, 'coingecko', NOW()),
    ('USDC', 1.00, 'coingecko', NOW()),
    ('BNB', 500.00, 'coingecko', NOW()),
    ('SOL', 100.00, 'coingecko', NOW()),
    ('DOGE', 0.10, 'coingecko', NOW())
ON CONFLICT (currency) DO UPDATE SET
    usd_rate = EXCLUDED.usd_rate,
    last_updated = NOW();

-- =====================================================
-- 4. VERIFICATION QUERIES
-- =====================================================

-- Check if tables were created
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name LIKE '%crypto%'
ORDER BY table_name;

-- Check platform wallet
SELECT * FROM platform_wallets;

-- Check exchange rates
SELECT * FROM crypto_exchange_rates ORDER BY currency;

-- =====================================================
-- 5. OPTIONAL: ADDITIONAL WALLETS
-- =====================================================

-- Uncomment and modify these if you want to add more wallets:

/*
-- Bitcoin Wallet
INSERT INTO platform_wallets (currency, network, address, is_active)
VALUES ('BTC', 'bitcoin', 'YOUR_BTC_ADDRESS_HERE', true)
ON CONFLICT DO NOTHING;

-- Ethereum Wallet (for ETH, USDT ERC-20, USDC)
INSERT INTO platform_wallets (currency, network, address, is_active)
VALUES 
    ('ETH', 'ethereum', 'YOUR_ETH_ADDRESS_HERE', true),
    ('USDT', 'ethereum', 'YOUR_ETH_ADDRESS_HERE', true),
    ('USDC', 'ethereum', 'YOUR_ETH_ADDRESS_HERE', true)
ON CONFLICT DO NOTHING;

-- Binance Smart Chain Wallet
INSERT INTO platform_wallets (currency, network, address, is_active)
VALUES 
    ('BNB', 'binance-smart-chain', 'YOUR_BSC_ADDRESS_HERE', true),
    ('USDT', 'binance-smart-chain', 'YOUR_BSC_ADDRESS_HERE', true)
ON CONFLICT DO NOTHING;

-- Polygon Wallet
INSERT INTO platform_wallets (currency, network, address, is_active)
VALUES 
    ('USDT', 'polygon', 'YOUR_POLYGON_ADDRESS_HERE', true),
    ('USDC', 'polygon', 'YOUR_POLYGON_ADDRESS_HERE', true)
ON CONFLICT DO NOTHING;

-- Solana Wallet
INSERT INTO platform_wallets (currency, network, address, is_active)
VALUES ('SOL', 'solana', 'YOUR_SOLANA_ADDRESS_HERE', true)
ON CONFLICT DO NOTHING;
*/

-- =====================================================
-- 6. USEFUL MANAGEMENT QUERIES
-- =====================================================

-- View all pending payments
-- SELECT * FROM crypto_payment_requests WHERE status = 'pending' ORDER BY created_at DESC;

-- View all completed payments
-- SELECT * FROM crypto_payment_requests WHERE status = 'completed' ORDER BY completed_at DESC;

-- View payment statistics
-- SELECT 
--     status,
--     COUNT(*) as count,
--     SUM(amount_usd) as total_usd,
--     currency
-- FROM crypto_payment_requests
-- GROUP BY status, currency
-- ORDER BY status, currency;

-- View recent transactions
-- SELECT * FROM crypto_transactions ORDER BY detected_at DESC LIMIT 10;

-- Update exchange rate manually (if needed)
-- UPDATE crypto_exchange_rates SET usd_rate = 50000, last_updated = NOW() WHERE currency = 'BTC';

-- Expire old pending payments manually (backup for the job)
-- UPDATE crypto_payment_requests 
-- SET status = 'expired', updated_at = NOW() 
-- WHERE status = 'pending' AND expires_at < NOW();

-- =====================================================
-- SCRIPT COMPLETE
-- =====================================================

-- Summary
SELECT 'Crypto payment tables setup complete!' as status;
SELECT COUNT(*) as platform_wallets_count FROM platform_wallets;
SELECT COUNT(*) as exchange_rates_count FROM crypto_exchange_rates;
