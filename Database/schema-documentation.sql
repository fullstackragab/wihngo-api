-- ========================================
-- WIHNGO DATABASE SCHEMA DOCUMENTATION
-- ========================================
-- This file documents the complete database schema that will be
-- automatically created by Entity Framework Core when the application starts.
-- 
-- Connection: Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres
-- ========================================

-- ========================================
-- CORE USER & BIRD MANAGEMENT TABLES
-- ========================================

-- Users table - stores all user accounts with authentication details
CREATE TABLE IF NOT EXISTS users (
    user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    email_confirmation_token VARCHAR(500),
    email_confirmation_token_expiry TIMESTAMP,
    password_reset_token VARCHAR(500),
    password_reset_token_expiry TIMESTAMP,
    refresh_token_hash VARCHAR(500),
    refresh_token_expiry TIMESTAMP,
    failed_login_attempts INTEGER NOT NULL DEFAULT 0,
    is_account_locked BOOLEAN NOT NULL DEFAULT FALSE,
    lockout_end TIMESTAMP,
    last_login_at TIMESTAMP,
    last_password_change_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Birds table - stores individual bird profiles
CREATE TABLE IF NOT EXISTS birds (
    bird_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    species VARCHAR(100) NOT NULL,
    tagline VARCHAR(200),
    description TEXT,
    image_url VARCHAR(1000),
    video_url VARCHAR(1000),
    loved_count INTEGER NOT NULL DEFAULT 0,
    donation_cents BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Stories table - stores updates and stories about birds
CREATE TABLE IF NOT EXISTS stories (
    story_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    author_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    content VARCHAR(5000) NOT NULL,
    image_url VARCHAR(1000),
    is_highlighted BOOLEAN NOT NULL DEFAULT FALSE,
    highlight_order INTEGER,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Support Transactions table - records all donations to birds
CREATE TABLE IF NOT EXISTS support_transactions (
    transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    supporter_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    amount DECIMAL(10,2) NOT NULL,
    message VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Loves table - many-to-many relationship between users and birds
CREATE TABLE IF NOT EXISTS loves (
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, bird_id)
);

-- Support Usage table - tracks how bird owners use donations
CREATE TABLE IF NOT EXISTS support_usage (
    usage_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    amount_cents BIGINT NOT NULL,
    category VARCHAR(50) NOT NULL,
    description VARCHAR(500),
    receipt_url VARCHAR(1000),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- PREMIUM SUBSCRIPTION TABLES
-- ========================================

-- Bird Premium Subscriptions table - tracks premium subscriptions for birds
CREATE TABLE IF NOT EXISTS bird_premium_subscriptions (
    subscription_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    plan VARCHAR(20) NOT NULL,
    amount_cents INTEGER NOT NULL,
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    auto_renew BOOLEAN NOT NULL DEFAULT TRUE,
    payment_method VARCHAR(50),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Premium Styles table - stores custom styling for premium birds
CREATE TABLE IF NOT EXISTS premium_styles (
    style_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL UNIQUE REFERENCES birds(bird_id) ON DELETE CASCADE,
    theme VARCHAR(50) NOT NULL DEFAULT 'default',
    primary_color VARCHAR(7) NOT NULL DEFAULT '#4F46E5',
    accent_color VARCHAR(7) NOT NULL DEFAULT '#F59E0B',
    font_style VARCHAR(50) NOT NULL DEFAULT 'modern',
    layout VARCHAR(50) NOT NULL DEFAULT 'standard',
    custom_css TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- CHARITY & DONATIONS TABLES
-- ========================================

-- Charity Allocations table - tracks how much goes to charity from subscriptions
CREATE TABLE IF NOT EXISTS charity_allocations (
    allocation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID NOT NULL REFERENCES bird_premium_subscriptions(subscription_id) ON DELETE CASCADE,
    amount_cents INTEGER NOT NULL,
    charity_name VARCHAR(200) NOT NULL,
    allocated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Charity Impact Stats table - aggregate statistics about charity contributions
CREATE TABLE IF NOT EXISTS charity_impact_stats (
    stats_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    total_donated_cents BIGINT NOT NULL DEFAULT 0,
    total_subscriptions INTEGER NOT NULL DEFAULT 0,
    period_start TIMESTAMP NOT NULL,
    period_end TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- CRYPTO PAYMENT TABLES
-- ========================================

-- Platform Wallets table - merchant wallet addresses for receiving crypto
CREATE TABLE IF NOT EXISTS platform_wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    address VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (currency, network, address)
);

-- Crypto Payment Requests table - tracks pending and completed crypto payments
CREATE TABLE IF NOT EXISTS crypto_payment_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID REFERENCES birds(bird_id) ON DELETE SET NULL,
    amount_usd DECIMAL(10,2) NOT NULL,
    amount_crypto DECIMAL(20,10) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    exchange_rate DECIMAL(20,2) NOT NULL,
    wallet_address VARCHAR(255) NOT NULL,
    address_index INTEGER,
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
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Crypto Transactions table - records all blockchain transactions
CREATE TABLE IF NOT EXISTS crypto_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_request_id UUID REFERENCES crypto_payment_requests(id) ON DELETE SET NULL,
    transaction_hash VARCHAR(255) NOT NULL UNIQUE,
    from_address VARCHAR(255) NOT NULL,
    to_address VARCHAR(255) NOT NULL,
    amount DECIMAL(30,10) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    block_number BIGINT,
    confirmations INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Crypto Exchange Rates table - stores current USD exchange rates
CREATE TABLE IF NOT EXISTS crypto_exchange_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    currency VARCHAR(10) NOT NULL UNIQUE,
    usd_rate DECIMAL(20,2) NOT NULL,
    source VARCHAR(50) NOT NULL,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Crypto Payment Methods table - stores user's saved wallet addresses
CREATE TABLE IF NOT EXISTS crypto_payment_methods (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    wallet_address VARCHAR(255) NOT NULL,
    wallet_name VARCHAR(100),
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (user_id, wallet_address, currency, network)
);

-- On-Chain Deposits table - tracks deposits made directly to user wallets
CREATE TABLE IF NOT EXISTS on_chain_deposits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    chain VARCHAR(20) NOT NULL,
    token VARCHAR(10) NOT NULL,
    address_or_account VARCHAR(255) NOT NULL,
    amount_crypto DECIMAL(30,10) NOT NULL,
    amount_usd DECIMAL(18,2) NOT NULL,
    tx_hash_or_sig VARCHAR(255) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    confirmations INTEGER NOT NULL DEFAULT 0,
    detected_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    confirmed_at TIMESTAMP,
    credited_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Token Configuration table - defines supported tokens and chains
CREATE TABLE IF NOT EXISTS token_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    token VARCHAR(10) NOT NULL,
    chain VARCHAR(20) NOT NULL,
    contract_address VARCHAR(255),
    decimals INTEGER NOT NULL,
    min_deposit DECIMAL(20,10) NOT NULL,
    required_confirmations INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (token, chain)
);

-- ========================================
-- NOTIFICATION TABLES
-- ========================================

-- Notifications table - stores all system notifications
CREATE TABLE IF NOT EXISTS notifications (
    notification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    data JSONB,
    priority INTEGER NOT NULL DEFAULT 1,
    channels INTEGER NOT NULL DEFAULT 1,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMP,
    group_id UUID,
    related_entity_type VARCHAR(50),
    related_entity_id UUID,
    action_url VARCHAR(500),
    expires_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Notification Preferences table - user preferences for notification types
CREATE TABLE IF NOT EXISTS notification_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    notification_type VARCHAR(50) NOT NULL,
    enabled_channels INTEGER NOT NULL DEFAULT 7,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (user_id, notification_type)
);

-- Notification Settings table - global notification settings per user
CREATE TABLE IF NOT EXISTS notification_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
    in_app_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    push_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    email_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    daily_digest_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    digest_time TIME NOT NULL DEFAULT '09:00:00',
    quiet_hours_start TIME,
    quiet_hours_end TIME,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- User Devices table - stores device tokens for push notifications
CREATE TABLE IF NOT EXISTS user_devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    push_token VARCHAR(500) NOT NULL UNIQUE,
    platform VARCHAR(20) NOT NULL,
    device_name VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_used_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- INVOICE & PAYMENT SYSTEM TABLES
-- ========================================

-- Invoice sequence for invoice numbers
CREATE SEQUENCE IF NOT EXISTS wihngo_invoice_seq START 1;

-- Invoices table - manages payment invoices
CREATE TABLE IF NOT EXISTS invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_number VARCHAR(50) UNIQUE,
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    bird_id UUID REFERENCES birds(bird_id) ON DELETE SET NULL,
    amount_fiat DECIMAL(18,6) NOT NULL,
    fiat_currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    amount_fiat_at_settlement DECIMAL(18,6),
    settlement_currency VARCHAR(10),
    state VARCHAR(50) NOT NULL DEFAULT 'CREATED',
    preferred_payment_methods JSONB,
    metadata JSONB,
    issued_pdf_url VARCHAR(1000),
    issued_at TIMESTAMP,
    receipt_notes TEXT,
    is_tax_deductible BOOLEAN NOT NULL DEFAULT FALSE,
    solana_reference VARCHAR(255),
    base_payment_data JSONB,
    paypal_order_id VARCHAR(255),
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Payments table - records all payment transactions for invoices
CREATE TABLE IF NOT EXISTS payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    provider VARCHAR(50) NOT NULL,
    provider_tx_id VARCHAR(255),
    tx_hash VARCHAR(255) UNIQUE,
    from_address VARCHAR(255),
    to_address VARCHAR(255),
    amount_crypto DECIMAL(30,10),
    crypto_currency VARCHAR(10),
    chain VARCHAR(20),
    amount_fiat DECIMAL(18,6),
    fiat_currency VARCHAR(10),
    exchange_rate_at_payment DECIMAL(20,6),
    state VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    confirmations INTEGER NOT NULL DEFAULT 0,
    detected_at TIMESTAMP,
    confirmed_at TIMESTAMP,
    settled_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Supported Tokens table - defines which crypto tokens are accepted
CREATE TABLE IF NOT EXISTS supported_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    token_symbol VARCHAR(10) NOT NULL,
    chain VARCHAR(20) NOT NULL,
    mint_address VARCHAR(255) NOT NULL,
    decimals INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    tolerance_percent DECIMAL(5,2) NOT NULL DEFAULT 0.5,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (token_symbol, chain)
);

-- Refund Requests table - manages refund requests
CREATE TABLE IF NOT EXISTS refund_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    amount_fiat DECIMAL(18,6) NOT NULL,
    fiat_currency VARCHAR(10) NOT NULL,
    reason TEXT NOT NULL,
    state VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    reviewed_by_admin_id UUID,
    admin_notes TEXT,
    refunded_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Payment Events table - audit log for payment state changes
CREATE TABLE IF NOT EXISTS payment_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    payment_id UUID REFERENCES payments(id) ON DELETE SET NULL,
    event_type VARCHAR(50) NOT NULL,
    from_state VARCHAR(50),
    to_state VARCHAR(50),
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Audit Logs table - system-wide audit trail
CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,
    user_id UUID REFERENCES users(user_id) ON DELETE SET NULL,
    changes JSONB,
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Webhooks Received table - tracks incoming webhooks from payment providers
CREATE TABLE IF NOT EXISTS webhook_received (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider VARCHAR(50) NOT NULL,
    provider_event_id VARCHAR(255) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    processed BOOLEAN NOT NULL DEFAULT FALSE,
    processed_at TIMESTAMP,
    error_message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (provider, provider_event_id)
);

-- Blockchain Cursors table - tracks blockchain scanning progress
CREATE TABLE IF NOT EXISTS blockchain_cursors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    chain VARCHAR(20) NOT NULL,
    cursor_type VARCHAR(50) NOT NULL,
    last_processed_slot_or_block BIGINT NOT NULL,
    last_processed_signature VARCHAR(255),
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (chain, cursor_type)
);

-- ========================================
-- INDEXES FOR PERFORMANCE
-- ========================================

-- User indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_refresh_token_expiry ON users(refresh_token_expiry);

-- Bird indexes
CREATE INDEX IF NOT EXISTS idx_birds_owner_id ON birds(owner_id);
CREATE INDEX IF NOT EXISTS idx_birds_loved_count ON birds(loved_count DESC);
CREATE INDEX IF NOT EXISTS idx_birds_donation_cents ON birds(donation_cents DESC);

-- Story indexes
CREATE INDEX IF NOT EXISTS idx_stories_bird_id ON stories(bird_id);
CREATE INDEX IF NOT EXISTS idx_stories_author_id ON stories(author_id);
CREATE INDEX IF NOT EXISTS idx_stories_created_at ON stories(created_at DESC);

-- Support transaction indexes
CREATE INDEX IF NOT EXISTS idx_support_transactions_bird_id ON support_transactions(bird_id);
CREATE INDEX IF NOT EXISTS idx_support_transactions_supporter_id ON support_transactions(supporter_id);
CREATE INDEX IF NOT EXISTS idx_support_transactions_created_at ON support_transactions(created_at DESC);

-- Crypto payment indexes
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_user_id ON crypto_payment_requests(user_id);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_status ON crypto_payment_requests(status);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_transaction_hash ON crypto_payment_requests(transaction_hash);
CREATE INDEX IF NOT EXISTS idx_crypto_payment_requests_expires_at ON crypto_payment_requests(expires_at);

-- On-chain deposit indexes
CREATE INDEX IF NOT EXISTS idx_on_chain_deposits_user_id ON on_chain_deposits(user_id);
CREATE INDEX IF NOT EXISTS idx_on_chain_deposits_status ON on_chain_deposits(status);
CREATE INDEX IF NOT EXISTS idx_on_chain_deposits_chain_address ON on_chain_deposits(chain, address_or_account);
CREATE INDEX IF NOT EXISTS idx_on_chain_deposits_detected_at ON on_chain_deposits(detected_at DESC);

-- Notification indexes
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_user_id_is_read ON notifications(user_id, is_read);
CREATE INDEX IF NOT EXISTS idx_notifications_group_id ON notifications(group_id);
CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON notifications(created_at DESC);

-- Invoice indexes
CREATE INDEX IF NOT EXISTS idx_invoices_user_id ON invoices(user_id);
CREATE INDEX IF NOT EXISTS idx_invoices_state ON invoices(state);
CREATE INDEX IF NOT EXISTS idx_invoices_expires_at ON invoices(expires_at);

-- Payment indexes
CREATE INDEX IF NOT EXISTS idx_payments_invoice_id ON payments(invoice_id);
CREATE INDEX IF NOT EXISTS idx_payments_provider_tx_id ON payments(provider_tx_id);

-- Audit log indexes
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at DESC);

-- ========================================
-- SAMPLE DATA TO BE SEEDED
-- ========================================
-- The application will automatically seed:
-- 
-- 1. Supported Tokens (4 tokens):
--    - USDC on Solana
--    - EURC on Solana
--    - USDC on Base
--    - EURC on Base
--
-- 2. Platform Wallets (4 wallets):
--    - USDT on Tron
--    - USDT on Ethereum
--    - USDT on BSC
--    - ETH on Sepolia
--
-- 3. Crypto Exchange Rates (7 currencies):
--    - BTC, ETH, USDT, USDC, BNB, SOL, DOGE
--
-- 4. Development Data (when in Development mode):
--    - 5 Users with hashed passwords
--    - 10 Birds with various species
--    - Multiple Stories per bird
--    - Love relationships between users and birds
--    - Support transactions (donations)
--    - Notifications
--    - Sample invoices
--    - Sample crypto payment requests
--
-- ========================================
-- END OF SCHEMA DOCUMENTATION
-- ========================================
