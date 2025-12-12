-- ============================================
-- Wihngo Complete Database Setup Script
-- Fresh installation for Render.com PostgreSQL
-- Database: wihngo_kzno
-- ============================================

-- Connect to the database
\c wihngo_kzno

-- ============================================
-- DROP ALL EXISTING TABLES (Clean Start)
-- ============================================
DROP TABLE IF EXISTS refund_requests CASCADE;
DROP TABLE IF EXISTS payment_events CASCADE;
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS webhooks_received CASCADE;
DROP TABLE IF EXISTS blockchain_cursors CASCADE;
DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS invoices CASCADE;
DROP TABLE IF EXISTS supported_tokens CASCADE;
DROP TABLE IF EXISTS user_devices CASCADE;
DROP TABLE IF EXISTS notifications CASCADE;
DROP TABLE IF EXISTS crypto_payments CASCADE;
DROP TABLE IF EXISTS exchange_rates CASCADE;
DROP TABLE IF EXISTS charity_allocations CASCADE;
DROP TABLE IF EXISTS charities CASCADE;
DROP TABLE IF EXISTS premium_subscriptions CASCADE;
DROP TABLE IF EXISTS support_transactions CASCADE;
DROP TABLE IF EXISTS loves CASCADE;
DROP TABLE IF EXISTS stories CASCADE;
DROP TABLE IF EXISTS birds CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS on_chain_deposits CASCADE;
DROP SEQUENCE IF EXISTS wihngo_invoice_seq CASCADE;

-- ============================================
-- CREATE INVOICE SEQUENCE
-- ============================================
CREATE SEQUENCE wihngo_invoice_seq START 1;

-- ============================================
-- CREATE TABLES
-- ============================================

-- Users Table
CREATE TABLE users (
    user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    profile_image TEXT,
    bio TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_email ON users(email);

-- Birds Table
CREATE TABLE birds (
    bird_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    species VARCHAR(200),
    description TEXT,
    image_url TEXT,
    video_url TEXT,
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    donation_cents BIGINT DEFAULT 0,
    is_premium BOOLEAN DEFAULT FALSE,
    premium_expires_at TIMESTAMP
);

CREATE INDEX idx_birds_user_id ON birds(user_id);
CREATE INDEX idx_birds_is_premium ON birds(is_premium);

-- Stories Table
CREATE TABLE stories (
    story_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID REFERENCES birds(bird_id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    media_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    highlight_order INTEGER,
    is_highlighted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_stories_bird_id ON stories(bird_id);
CREATE INDEX idx_stories_user_id ON stories(user_id);
CREATE INDEX idx_stories_created_at ON stories(created_at DESC);

-- Loves Table
CREATE TABLE loves (
    love_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, bird_id)
);

CREATE INDEX idx_loves_user_id ON loves(user_id);
CREATE INDEX idx_loves_bird_id ON loves(bird_id);

-- Support Transactions Table
CREATE TABLE support_transactions (
    transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID REFERENCES birds(bird_id) ON DELETE CASCADE,
    amount_cents BIGINT NOT NULL,
    currency VARCHAR(10) DEFAULT 'USD',
    payment_method VARCHAR(50),
    status VARCHAR(50) DEFAULT 'pending',
    transaction_hash TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_support_transactions_user_id ON support_transactions(user_id);
CREATE INDEX idx_support_transactions_bird_id ON support_transactions(bird_id);

-- Premium Subscriptions Table
CREATE TABLE premium_subscriptions (
    subscription_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID REFERENCES birds(bird_id) ON DELETE SET NULL,
    plan_type VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    amount_cents BIGINT NOT NULL,
    currency VARCHAR(10) DEFAULT 'USD',
    start_date TIMESTAMP NOT NULL DEFAULT NOW(),
    end_date TIMESTAMP,
    auto_renew BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_premium_subscriptions_user_id ON premium_subscriptions(user_id);
CREATE INDEX idx_premium_subscriptions_status ON premium_subscriptions(status);

-- Charities Table
CREATE TABLE charities (
    charity_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    description TEXT,
    ein VARCHAR(20),
    website_url TEXT,
    logo_url TEXT,
    is_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Charity Allocations Table
CREATE TABLE charity_allocations (
    allocation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    charity_id UUID NOT NULL REFERENCES charities(charity_id) ON DELETE CASCADE,
    percentage DECIMAL(5,2) NOT NULL CHECK (percentage >= 0 AND percentage <= 100),
    amount_cents BIGINT DEFAULT 0,
    last_processed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_charity_allocations_user_id ON charity_allocations(user_id);
CREATE INDEX idx_charity_allocations_charity_id ON charity_allocations(charity_id);

-- Exchange Rates Table
CREATE TABLE exchange_rates (
    rate_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    from_currency VARCHAR(10) NOT NULL,
    to_currency VARCHAR(10) NOT NULL,
    rate DECIMAL(18,8) NOT NULL,
    source VARCHAR(50),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(from_currency, to_currency)
);

CREATE INDEX idx_exchange_rates_currencies ON exchange_rates(from_currency, to_currency);

-- Crypto Payments Table
CREATE TABLE crypto_payments (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID REFERENCES birds(bird_id) ON DELETE CASCADE,
    transaction_hash TEXT UNIQUE NOT NULL,
    from_address TEXT NOT NULL,
    to_address TEXT NOT NULL,
    amount_crypto DECIMAL(28,18) NOT NULL,
    token_symbol VARCHAR(20) NOT NULL,
    network VARCHAR(50) NOT NULL,
    amount_usd DECIMAL(18,2),
    status VARCHAR(50) DEFAULT 'pending',
    confirmations INTEGER DEFAULT 0,
    confirmed_at TIMESTAMP,
    expires_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_crypto_payments_user_id ON crypto_payments(user_id);
CREATE INDEX idx_crypto_payments_tx_hash ON crypto_payments(transaction_hash);
CREATE INDEX idx_crypto_payments_status ON crypto_payments(status);

-- Notifications Table
CREATE TABLE notifications (
    notification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    deep_link TEXT,
    is_read BOOLEAN DEFAULT FALSE,
    bird_id UUID REFERENCES birds(bird_id) ON DELETE SET NULL,
    story_id UUID REFERENCES stories(story_id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user_id ON notifications(user_id);
CREATE INDEX idx_notifications_is_read ON notifications(is_read);
CREATE INDEX idx_notifications_created_at ON notifications(created_at DESC);

-- User Devices Table
CREATE TABLE user_devices (
    device_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    push_token VARCHAR(500) NOT NULL,
    device_type VARCHAR(50),
    device_name VARCHAR(200),
    is_active BOOLEAN DEFAULT TRUE,
    last_used_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_user_devices_user_id ON user_devices(user_id);
CREATE INDEX idx_user_devices_push_token ON user_devices(push_token);

-- On-Chain Deposits Table
CREATE TABLE on_chain_deposits (
    deposit_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    deposit_address TEXT NOT NULL,
    chain VARCHAR(50) NOT NULL,
    token_symbol VARCHAR(20) NOT NULL,
    amount_crypto DECIMAL(28,18) NOT NULL,
    amount_usd DECIMAL(18,2),
    transaction_hash TEXT UNIQUE NOT NULL,
    from_address TEXT NOT NULL,
    block_number BIGINT,
    confirmations INTEGER DEFAULT 0,
    status VARCHAR(50) DEFAULT 'pending',
    confirmed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_on_chain_deposits_user_id ON on_chain_deposits(user_id);
CREATE INDEX idx_on_chain_deposits_tx_hash ON on_chain_deposits(transaction_hash);
CREATE INDEX idx_on_chain_deposits_status ON on_chain_deposits(status);

-- ============================================
-- INVOICE & PAYMENT SYSTEM TABLES
-- ============================================

-- Supported Tokens Table
CREATE TABLE supported_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    token_symbol VARCHAR(20) NOT NULL,
    chain VARCHAR(50) NOT NULL,
    mint_address TEXT NOT NULL,
    merchant_receiving_address TEXT,
    decimals INTEGER NOT NULL DEFAULT 6,
    is_active BOOLEAN DEFAULT TRUE,
    tolerance_percent DECIMAL(5,2) DEFAULT 0.5,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(token_symbol, chain)
);

CREATE INDEX idx_supported_tokens_chain ON supported_tokens(chain);
CREATE INDEX idx_supported_tokens_active ON supported_tokens(is_active);

-- Invoices Table
CREATE TABLE invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_number VARCHAR(50) UNIQUE,
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID REFERENCES birds(bird_id) ON DELETE SET NULL,
    amount_fiat DECIMAL(18,2) NOT NULL,
    fiat_currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    amount_fiat_at_settlement DECIMAL(18,2),
    settlement_currency VARCHAR(10),
    state VARCHAR(50) NOT NULL DEFAULT 'CREATED',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP,
    issued_at TIMESTAMP,
    issued_pdf_url TEXT,
    is_tax_deductible BOOLEAN DEFAULT FALSE,
    metadata JSONB,
    solana_reference TEXT,
    base_payment_data JSONB,
    paypal_order_id TEXT UNIQUE
);

CREATE INDEX idx_invoices_user_id ON invoices(user_id);
CREATE INDEX idx_invoices_state ON invoices(state);
CREATE INDEX idx_invoices_invoice_number ON invoices(invoice_number);
CREATE INDEX idx_invoices_created_at ON invoices(created_at DESC);

-- Payments Table
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    payment_method VARCHAR(50) NOT NULL,
    payer_identifier TEXT,
    tx_hash TEXT UNIQUE,
    provider_tx_id TEXT UNIQUE,
    token VARCHAR(20),
    chain VARCHAR(50),
    amount_crypto DECIMAL(28,18),
    fiat_value_at_payment DECIMAL(18,2),
    block_slot BIGINT,
    confirmations INTEGER DEFAULT 0,
    confirmed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_payments_invoice_id ON payments(invoice_id);
CREATE INDEX idx_payments_tx_hash ON payments(tx_hash);
CREATE INDEX idx_payments_provider_tx_id ON payments(provider_tx_id);

-- Refund Requests Table
CREATE TABLE refund_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    payment_id UUID NOT NULL REFERENCES payments(id) ON DELETE CASCADE,
    amount DECIMAL(18,2) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    reason TEXT NOT NULL,
    state VARCHAR(50) NOT NULL DEFAULT 'REQUESTED',
    refund_method VARCHAR(50),
    provider_refund_id TEXT,
    requires_approval BOOLEAN DEFAULT FALSE,
    approved_by UUID REFERENCES users(user_id) ON DELETE SET NULL,
    approved_at TIMESTAMP,
    completed_at TIMESTAMP,
    error_message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_refund_requests_invoice_id ON refund_requests(invoice_id);
CREATE INDEX idx_refund_requests_state ON refund_requests(state);

-- Payment Events Table
CREATE TABLE payment_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    payment_id UUID REFERENCES payments(id) ON DELETE SET NULL,
    event_type VARCHAR(100) NOT NULL,
    old_state VARCHAR(50),
    new_state VARCHAR(50),
    actor_user_id UUID REFERENCES users(user_id) ON DELETE SET NULL,
    description TEXT,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_payment_events_invoice_id ON payment_events(invoice_id);
CREATE INDEX idx_payment_events_created_at ON payment_events(created_at DESC);

-- Audit Logs Table
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(100) NOT NULL,
    actor_user_id UUID REFERENCES users(user_id) ON DELETE SET NULL,
    changes JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at DESC);

-- Webhooks Received Table
CREATE TABLE webhooks_received (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider VARCHAR(50) NOT NULL,
    provider_event_id TEXT NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload TEXT NOT NULL,
    processed BOOLEAN DEFAULT FALSE,
    processed_at TIMESTAMP,
    error_message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(provider, provider_event_id)
);

CREATE INDEX idx_webhooks_received_processed ON webhooks_received(processed);
CREATE INDEX idx_webhooks_received_created_at ON webhooks_received(created_at DESC);

-- Blockchain Cursors Table
CREATE TABLE blockchain_cursors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    chain VARCHAR(50) NOT NULL,
    cursor_type VARCHAR(100) NOT NULL,
    last_processed_value BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(chain, cursor_type)
);

-- ============================================
-- SEED DATA
-- ============================================

-- Seed Test User (password: Test123!)
INSERT INTO users (user_id, name, email, password_hash, created_at)
VALUES (
    gen_random_uuid(),
    'Test User',
    'test@wihngo.com',
    '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou',
    NOW()
);

-- Seed Supported Tokens with your crypto addresses
INSERT INTO supported_tokens (id, token_symbol, chain, mint_address, merchant_receiving_address, decimals, is_active, tolerance_percent, created_at)
VALUES
    (gen_random_uuid(), 'USDC', 'solana', 'EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', 6, true, 0.5, NOW()),
    (gen_random_uuid(), 'EURC', 'solana', 'HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', 6, true, 0.5, NOW()),
    (gen_random_uuid(), 'USDC', 'base', '0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913', '0x2e61b5d2066eAFb86FBD75F59c585468ceE51092', 6, true, 0.5, NOW()),
    (gen_random_uuid(), 'EURC', 'base', '0x60a3E35Cc302bFA44Cb288Bc5a4F316Fdb1adb42', '0x2e61b5d2066eAFb86FBD75F59c585468ceE51092', 6, true, 0.5, NOW());

-- Seed Sample Birds with your image/video URLs
INSERT INTO birds (bird_id, name, species, description, image_url, video_url, user_id, created_at)
SELECT 
    gen_random_uuid(),
    'Robin',
    'American Robin',
    'A beautiful robin spotted in the garden',
    'https://thegraphicsfairy.com/wp-content/uploads/2020/09/Robin-Branch-Vintage-Image-GraphicsFairy.jpg',
    'https://cdn.pixabay.com/video/2019/11/13/29033-373125430_large.mp4',
    user_id,
    NOW()
FROM users WHERE email = 'test@wihngo.com';

-- Seed Exchange Rates
INSERT INTO exchange_rates (rate_id, from_currency, to_currency, rate, source, updated_at)
VALUES
    (gen_random_uuid(), 'USD', 'EUR', 0.92, 'manual', NOW()),
    (gen_random_uuid(), 'EUR', 'USD', 1.09, 'manual', NOW()),
    (gen_random_uuid(), 'USDC', 'USD', 1.00, 'manual', NOW()),
    (gen_random_uuid(), 'EURC', 'EUR', 1.00, 'manual', NOW());

-- Seed Sample Charity
INSERT INTO charities (charity_id, name, description, website_url, is_verified, created_at)
VALUES (
    gen_random_uuid(),
    'Audubon Society',
    'Dedicated to the conservation of birds and their habitats',
    'https://www.audubon.org',
    true,
    NOW()
);

-- ============================================
-- VERIFICATION QUERIES
-- ============================================
SELECT '? Database setup complete!' as status;

SELECT 'Tables Created:' as info;
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

SELECT 'Invoice Payment System Tables:' as info;
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('invoices', 'payments', 'supported_tokens', 'refund_requests', 'payment_events', 'audit_logs', 'webhooks_received', 'blockchain_cursors')
ORDER BY table_name;

SELECT 'Supported Tokens:' as info;
SELECT token_symbol, chain, mint_address, merchant_receiving_address, is_active 
FROM supported_tokens 
ORDER BY chain, token_symbol;

SELECT 'Test User:' as info;
SELECT user_id, name, email, created_at 
FROM users 
WHERE email = 'test@wihngo.com';

SELECT 'Sample Birds:' as info;
SELECT bird_id, name, species, image_url 
FROM birds;

SELECT 'Invoice Sequence:' as info;
SELECT last_value FROM wihngo_invoice_seq;

SELECT '? Setup Complete! Login with: test@wihngo.com / Test123!' as message;
