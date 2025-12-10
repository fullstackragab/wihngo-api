-- =====================================================
-- Wihngo Crypto Payment System - PostgreSQL Migration
-- =====================================================
-- Execute this script on your PostgreSQL database server
-- Database: wihngo
-- =====================================================

-- 1. Create platform_wallets table
CREATE TABLE IF NOT EXISTS platform_wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    address VARCHAR(255) NOT NULL,
    private_key_encrypted TEXT,
    derivation_path VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Unique constraint for currency + network + address combination
    CONSTRAINT uk_platform_wallets_currency_network_address UNIQUE (currency, network, address)
);

-- Create index for active wallets
CREATE INDEX IF NOT EXISTS idx_platform_wallets_active ON platform_wallets(is_active);
CREATE INDEX IF NOT EXISTS idx_platform_wallets_currency_network ON platform_wallets(currency, network);

-- 2. Create crypto_exchange_rates table
CREATE TABLE IF NOT EXISTS crypto_exchange_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    currency VARCHAR(10) NOT NULL UNIQUE,
    usd_rate NUMERIC(20,2) NOT NULL,
    source VARCHAR(50) NOT NULL DEFAULT 'coingecko',
    last_updated TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create index for currency lookup
CREATE INDEX IF NOT EXISTS idx_crypto_exchange_rates_currency ON crypto_exchange_rates(currency);

-- 3. Create crypto_payment_requests table
CREATE TABLE IF NOT EXISTS crypto_payment_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    bird_id UUID,
    amount_usd NUMERIC(10,2) NOT NULL,
    amount_crypto NUMERIC(20,10) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    exchange_rate NUMERIC(20,2) NOT NULL,
    wallet_address VARCHAR(255) NOT NULL,
    user_wallet_address VARCHAR(255),
    qr_code_data TEXT NOT NULL,
    payment_uri TEXT NOT NULL,
    transaction_hash VARCHAR(255),
    confirmations INTEGER NOT NULL DEFAULT 0,
    required_confirmations INTEGER NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    purpose VARCHAR(50) NOT NULL,
    plan VARCHAR(20),
    metadata JSONB,
    expires_at TIMESTAMP NOT NULL,
    confirmed_at TIMESTAMP,
    completed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key to users table (referencing user_id column)
    CONSTRAINT fk_crypto_payment_requests_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    
    -- Foreign key to birds table (referencing bird_id column)
    CONSTRAINT fk_crypto_payment_requests_bird FOREIGN KEY (bird_id) REFERENCES birds(bird_id) ON DELETE SET NULL
);

-- Create indexes for crypto_payment_requests
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_user_id ON crypto_payment_requests(user_id);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_bird_id ON crypto_payment_requests(bird_id);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_status ON crypto_payment_requests(status);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_transaction_hash ON crypto_payment_requests(transaction_hash);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_expires_at ON crypto_payment_requests(expires_at);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_created_at ON crypto_payment_requests(created_at);

-- 4. Create crypto_transactions table
CREATE TABLE IF NOT EXISTS crypto_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_request_id UUID NOT NULL,
    transaction_hash VARCHAR(255) NOT NULL UNIQUE,
    from_address VARCHAR(255) NOT NULL,
    to_address VARCHAR(255) NOT NULL,
    amount NUMERIC(20,10) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    confirmations INTEGER NOT NULL DEFAULT 0,
    block_number BIGINT,
    block_hash VARCHAR(255),
    fee NUMERIC(20,10),
    gas_used BIGINT,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    raw_transaction JSONB,
    detected_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    confirmed_at TIMESTAMP,
    
    -- Foreign key to crypto_payment_requests
    CONSTRAINT fk_crypto_transactions_payment_request FOREIGN KEY (payment_request_id) 
        REFERENCES crypto_payment_requests(id) ON DELETE CASCADE
);

-- Create indexes for crypto_transactions
CREATE INDEX IF NOT EXISTS idx_crypto_transactions_payment_request_id ON crypto_transactions(payment_request_id);
CREATE INDEX IF NOT EXISTS idx_crypto_transactions_transaction_hash ON crypto_transactions(transaction_hash);
CREATE INDEX IF NOT EXISTS idx_crypto_transactions_status ON crypto_transactions(status);

-- 5. Create crypto_payment_methods table
CREATE TABLE IF NOT EXISTS crypto_payment_methods (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    wallet_address VARCHAR(255) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    label VARCHAR(100),
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key to users table (referencing user_id column)
    CONSTRAINT fk_crypto_payment_methods_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    
    -- Unique constraint for user + wallet + currency + network
    CONSTRAINT uk_crypto_payment_methods_user_wallet UNIQUE (user_id, wallet_address, currency, network)
);

-- Create indexes for crypto_payment_methods
CREATE INDEX IF NOT EXISTS idx_crypto_payment_methods_user_id ON crypto_payment_methods(user_id);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_methods_is_default ON crypto_payment_methods(is_default);

-- =====================================================
-- SEED DATA
-- =====================================================

-- Insert platform wallet for TRON USDT
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'USDT',
    'tron',
    'TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA',
    TRUE,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (currency, network, address) DO NOTHING;

-- Insert initial exchange rates
INSERT INTO crypto_exchange_rates (id, currency, usd_rate, source, last_updated)
VALUES
    (gen_random_uuid(), 'BTC', 50000.00, 'coingecko', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'ETH', 3000.00, 'coingecko', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'USDT', 1.00, 'coingecko', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'USDC', 1.00, 'coingecko', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'BNB', 500.00, 'coingecko', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'SOL', 100.00, 'coingecko', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'DOGE', 0.10, 'coingecko', CURRENT_TIMESTAMP)
ON CONFLICT (currency) DO UPDATE SET
    usd_rate = EXCLUDED.usd_rate,
    last_updated = EXCLUDED.last_updated;

-- =====================================================
-- TRIGGERS (Optional - for auto-updating timestamps)
-- =====================================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger for platform_wallets
DROP TRIGGER IF EXISTS update_platform_wallets_updated_at ON platform_wallets;
CREATE TRIGGER update_platform_wallets_updated_at
    BEFORE UPDATE ON platform_wallets
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger for crypto_payment_requests
DROP TRIGGER IF EXISTS update_crypto_payment_requests_updated_at ON crypto_payment_requests;
CREATE TRIGGER update_crypto_payment_requests_updated_at
    BEFORE UPDATE ON crypto_payment_requests
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger for crypto_payment_methods
DROP TRIGGER IF EXISTS update_crypto_payment_methods_updated_at ON crypto_payment_methods;
CREATE TRIGGER update_crypto_payment_methods_updated_at
    BEFORE UPDATE ON crypto_payment_methods
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Check if all tables were created successfully
SELECT 
    tablename,
    schemaname
FROM pg_tables
WHERE tablename IN (
    'platform_wallets',
    'crypto_exchange_rates',
    'crypto_payment_requests',
    'crypto_transactions',
    'crypto_payment_methods'
)
ORDER BY tablename;

-- Verify platform wallet was inserted
SELECT * FROM platform_wallets;

-- Verify exchange rates were inserted
SELECT currency, usd_rate, source FROM crypto_exchange_rates ORDER BY currency;

-- Show table row counts
SELECT 
    'platform_wallets' as table_name, COUNT(*) as row_count FROM platform_wallets
UNION ALL
SELECT 'crypto_exchange_rates', COUNT(*) FROM crypto_exchange_rates
UNION ALL
SELECT 'crypto_payment_requests', COUNT(*) FROM crypto_payment_requests
UNION ALL
SELECT 'crypto_transactions', COUNT(*) FROM crypto_transactions
UNION ALL
SELECT 'crypto_payment_methods', COUNT(*) FROM crypto_payment_methods;
