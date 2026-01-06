-- ============================================
-- PRODUCTION FULL MIGRATION: 2025-01-06
-- Run this on production to sync all schema changes
-- This is idempotent - safe to run multiple times
-- ============================================

-- =============================================
-- HELPER FUNCTIONS
-- =============================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_user_balance(p_user_id UUID)
RETURNS NUMERIC(20, 6) AS $$
DECLARE
    v_balance NUMERIC(20, 6);
BEGIN
    SELECT COALESCE(
        (SELECT balance_after FROM ledger_entries WHERE user_id = p_user_id ORDER BY created_at DESC LIMIT 1),
        0.000000
    ) INTO v_balance;
    RETURN v_balance;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- 1. WALLETS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    public_key VARCHAR(44) NOT NULL,
    wallet_provider VARCHAR(50) NOT NULL DEFAULT 'phantom',
    is_primary BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_wallets_public_key UNIQUE (public_key)
);
CREATE INDEX IF NOT EXISTS idx_wallets_user_id ON wallets(user_id);
CREATE INDEX IF NOT EXISTS idx_wallets_public_key ON wallets(public_key);

DROP TRIGGER IF EXISTS wallets_updated_at ON wallets;
CREATE TRIGGER wallets_updated_at BEFORE UPDATE ON wallets FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- 2. SUPPORT_INTENTS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS support_intents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supporter_user_id UUID NOT NULL REFERENCES users(user_id),
    bird_id UUID REFERENCES birds(bird_id) ON DELETE SET NULL,
    recipient_user_id UUID NOT NULL REFERENCES users(user_id),
    support_amount NUMERIC(20, 6) NOT NULL,
    platform_support_amount NUMERIC(20, 6) NOT NULL DEFAULT 0,
    total_amount NUMERIC(20, 6) NOT NULL,
    currency VARCHAR(10) NOT NULL DEFAULT 'USDC',
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    sender_wallet_pubkey VARCHAR(44),
    recipient_wallet_pubkey VARCHAR(44),
    payment_method VARCHAR(20) NOT NULL DEFAULT 'pending',
    solana_signature VARCHAR(88),
    confirmations INT NOT NULL DEFAULT 0,
    serialized_transaction TEXT,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    paid_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Add columns that may not exist
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS bird_amount NUMERIC(20, 6) DEFAULT 0;
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS wihngo_support_amount NUMERIC(20, 6) DEFAULT 0;
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS wihngo_wallet_pubkey VARCHAR(44);
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS wihngo_solana_signature VARCHAR(88);
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS platform_fee_percent NUMERIC(5,2) NOT NULL DEFAULT 5;
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS invoice_number VARCHAR(50);
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(255);
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS error_message TEXT;

-- Make bird_id nullable (for Wihngo-only support)
ALTER TABLE support_intents ALTER COLUMN bird_id DROP NOT NULL;

-- Fix FK constraint for bird_id
ALTER TABLE support_intents DROP CONSTRAINT IF EXISTS support_intents_bird_id_fkey;
ALTER TABLE support_intents ADD CONSTRAINT support_intents_bird_id_fkey
    FOREIGN KEY (bird_id) REFERENCES birds(bird_id) ON DELETE SET NULL;

-- Indexes
CREATE INDEX IF NOT EXISTS idx_support_intents_supporter ON support_intents(supporter_user_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_bird ON support_intents(bird_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_recipient ON support_intents(recipient_user_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_status ON support_intents(status);
CREATE INDEX IF NOT EXISTS idx_support_intents_created ON support_intents(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_support_intents_wihngo_amount ON support_intents(wihngo_support_amount) WHERE wihngo_support_amount > 0;
CREATE INDEX IF NOT EXISTS ix_support_intents_invoice_number ON support_intents(invoice_number) WHERE invoice_number IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_support_intents_idempotency ON support_intents(idempotency_key) WHERE idempotency_key IS NOT NULL;

DROP TRIGGER IF EXISTS support_intents_updated_at ON support_intents;
CREATE TRIGGER support_intents_updated_at BEFORE UPDATE ON support_intents FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- 3. BIRDS TABLE UPDATES
-- =============================================
ALTER TABLE birds ADD COLUMN IF NOT EXISTS support_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE birds ADD COLUMN IF NOT EXISTS location VARCHAR(200);
ALTER TABLE birds ADD COLUMN IF NOT EXISTS age VARCHAR(100);
ALTER TABLE birds ADD COLUMN IF NOT EXISTS needs_support BOOLEAN NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS idx_birds_support_enabled ON birds(support_enabled) WHERE support_enabled = true;
CREATE INDEX IF NOT EXISTS idx_birds_needs_support ON birds(needs_support) WHERE needs_support = true;

-- =============================================
-- 4. WEEKLY BIRD SUPPORT ROUNDS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS weekly_bird_support_rounds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    week_start_date DATE NOT NULL,
    times_supported INTEGER NOT NULL DEFAULT 0,
    last_supported_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_bird_week UNIQUE (bird_id, week_start_date)
);

CREATE INDEX IF NOT EXISTS idx_weekly_support_week ON weekly_bird_support_rounds(week_start_date);
CREATE INDEX IF NOT EXISTS idx_weekly_support_bird ON weekly_bird_support_rounds(bird_id);

-- =============================================
-- 5. LEDGER_ENTRIES TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS ledger_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id),
    amount_usdc NUMERIC(20, 6) NOT NULL,
    entry_type VARCHAR(30) NOT NULL,
    reference_type VARCHAR(30) NOT NULL,
    reference_id UUID NOT NULL,
    balance_after NUMERIC(20, 6) NOT NULL,
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_ledger_entries_user ON ledger_entries(user_id);
CREATE INDEX IF NOT EXISTS idx_ledger_entries_user_created ON ledger_entries(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_ledger_entries_reference ON ledger_entries(reference_type, reference_id);

-- =============================================
-- 6. P2P_PAYMENTS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS p2p_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sender_user_id UUID NOT NULL REFERENCES users(user_id),
    recipient_user_id UUID NOT NULL REFERENCES users(user_id),
    sender_wallet_pubkey VARCHAR(44),
    recipient_wallet_pubkey VARCHAR(44),
    amount_usdc NUMERIC(20, 6) NOT NULL,
    fee_usdc NUMERIC(20, 6) NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    solana_signature VARCHAR(88),
    block_slot BIGINT,
    confirmations INT NOT NULL DEFAULT 0,
    gas_sponsored BOOLEAN NOT NULL DEFAULT FALSE,
    serialized_transaction TEXT,
    memo VARCHAR(255),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    submitted_at TIMESTAMP WITH TIME ZONE,
    confirmed_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE p2p_payments ADD COLUMN IF NOT EXISTS invoice_number VARCHAR(50);
CREATE INDEX IF NOT EXISTS ix_p2p_payments_invoice_number ON p2p_payments(invoice_number) WHERE invoice_number IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_p2p_payments_sender ON p2p_payments(sender_user_id);
CREATE INDEX IF NOT EXISTS idx_p2p_payments_recipient ON p2p_payments(recipient_user_id);
CREATE INDEX IF NOT EXISTS idx_p2p_payments_status ON p2p_payments(status);

DROP TRIGGER IF EXISTS p2p_payments_updated_at ON p2p_payments;
CREATE TRIGGER p2p_payments_updated_at BEFORE UPDATE ON p2p_payments FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- 7. GAS_SPONSORSHIPS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS gas_sponsorships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id UUID NOT NULL REFERENCES p2p_payments(id) ON DELETE CASCADE,
    sponsored_sol_amount NUMERIC(20, 9) NOT NULL,
    fee_usdc_charged NUMERIC(20, 6) NOT NULL DEFAULT 0.01,
    sponsor_wallet_pubkey VARCHAR(44) NOT NULL,
    ata_created BOOLEAN NOT NULL DEFAULT FALSE,
    ata_address VARCHAR(44),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_gas_sponsorships_payment UNIQUE (payment_id)
);

-- =============================================
-- 8. PLATFORM_HOT_WALLETS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS platform_hot_wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    wallet_type VARCHAR(30) NOT NULL,
    public_key VARCHAR(44) NOT NULL,
    private_key_encrypted TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_platform_hot_wallets_pubkey UNIQUE (public_key)
);

DROP TRIGGER IF EXISTS platform_hot_wallets_updated_at ON platform_hot_wallets;
CREATE TRIGGER platform_hot_wallets_updated_at BEFORE UPDATE ON platform_hot_wallets FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- 9. WALLET_CONNECT_INTENTS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS wallet_connect_intents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(user_id) ON DELETE SET NULL,
    state VARCHAR(64) NOT NULL,
    nonce VARCHAR(128) NOT NULL,
    purpose VARCHAR(50) NOT NULL DEFAULT 'connect',
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    public_key VARCHAR(44),
    wallet_provider VARCHAR(50) NOT NULL DEFAULT 'phantom',
    signature VARCHAR(128),
    redirect_url VARCHAR(500),
    metadata JSONB,
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_wallet_connect_intents_state UNIQUE (state)
);

CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_state ON wallet_connect_intents(state);
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_user_status ON wallet_connect_intents(user_id, status);
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_expires ON wallet_connect_intents(expires_at) WHERE status = 'pending' OR status = 'awaiting_callback';
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_created ON wallet_connect_intents(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_pubkey ON wallet_connect_intents(public_key) WHERE status = 'completed';

DROP TRIGGER IF EXISTS wallet_connect_intents_updated_at ON wallet_connect_intents;
CREATE TRIGGER wallet_connect_intents_updated_at BEFORE UPDATE ON wallet_connect_intents FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE OR REPLACE FUNCTION expire_wallet_connect_intents()
RETURNS INTEGER AS $$
DECLARE
    expired_count INTEGER;
BEGIN
    UPDATE wallet_connect_intents
    SET status = 'expired', updated_at = CURRENT_TIMESTAMP
    WHERE status IN ('pending', 'awaiting_callback') AND expires_at < CURRENT_TIMESTAMP;
    GET DIAGNOSTICS expired_count = ROW_COUNT;
    RETURN expired_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- VERIFICATION
-- =============================================
DO $$
BEGIN
    RAISE NOTICE '✅ Migration PRODUCTION_FULL_2025_01_06 complete!';
    RAISE NOTICE '✅ Tables: wallets, support_intents, ledger_entries, p2p_payments, gas_sponsorships, platform_hot_wallets, wallet_connect_intents, weekly_bird_support_rounds';
    RAISE NOTICE '✅ Birds columns: support_enabled, location, age, needs_support';
    RAISE NOTICE '✅ support_intents: bird_id is now nullable for Wihngo-only support';
END $$;
