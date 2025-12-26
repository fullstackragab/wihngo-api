-- P2P Payment System Migration
-- Fresh start - drops old payment tables and creates new P2P system

-- =============================================
-- DROP OLD TABLES (if they exist)
-- =============================================
DROP TABLE IF EXISTS crypto_transactions CASCADE;
DROP TABLE IF EXISTS crypto_payment_requests CASCADE;
DROP TABLE IF EXISTS crypto_payment_methods CASCADE;
DROP TABLE IF EXISTS crypto_exchange_rates CASCADE;
DROP TABLE IF EXISTS payment_events CASCADE;
DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS invoices CASCADE;
DROP TABLE IF EXISTS platform_wallets CASCADE;

-- =============================================
-- CREATE NEW P2P PAYMENT TABLES
-- =============================================

-- 1. wallets - User wallet linking (Phantom)
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

-- 2. p2p_payments - P2P Payment records
CREATE TABLE IF NOT EXISTS p2p_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Parties
    sender_user_id UUID NOT NULL REFERENCES users(user_id),
    recipient_user_id UUID NOT NULL REFERENCES users(user_id),
    sender_wallet_pubkey VARCHAR(44),
    recipient_wallet_pubkey VARCHAR(44),

    -- Amounts (USDC has 6 decimals)
    amount_usdc NUMERIC(20, 6) NOT NULL,
    fee_usdc NUMERIC(20, 6) NOT NULL DEFAULT 0,

    -- Status: pending, awaiting_signature, submitted, confirming, confirmed, completed, failed, expired, cancelled
    status VARCHAR(20) NOT NULL DEFAULT 'pending',

    -- Blockchain details
    solana_signature VARCHAR(88),
    block_slot BIGINT,
    confirmations INT NOT NULL DEFAULT 0,

    -- Gas sponsorship
    gas_sponsored BOOLEAN NOT NULL DEFAULT FALSE,

    -- Transaction data
    serialized_transaction TEXT,

    -- Metadata
    memo VARCHAR(255),

    -- Timestamps
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    submitted_at TIMESTAMP WITH TIME ZONE,
    confirmed_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_sender_not_recipient CHECK (sender_user_id != recipient_user_id),
    CONSTRAINT chk_amount_positive CHECK (amount_usdc > 0),
    CONSTRAINT chk_fee_non_negative CHECK (fee_usdc >= 0)
);

CREATE INDEX IF NOT EXISTS idx_p2p_payments_sender ON p2p_payments(sender_user_id);
CREATE INDEX IF NOT EXISTS idx_p2p_payments_recipient ON p2p_payments(recipient_user_id);
CREATE INDEX IF NOT EXISTS idx_p2p_payments_status ON p2p_payments(status);
CREATE INDEX IF NOT EXISTS idx_p2p_payments_signature ON p2p_payments(solana_signature);
CREATE INDEX IF NOT EXISTS idx_p2p_payments_created ON p2p_payments(created_at DESC);

-- 3. gas_sponsorships - Track gas sponsorship events
CREATE TABLE IF NOT EXISTS gas_sponsorships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id UUID NOT NULL REFERENCES p2p_payments(id) ON DELETE CASCADE,

    -- SOL sponsored for transaction fee
    sponsored_sol_amount NUMERIC(20, 9) NOT NULL,

    -- Fee recovery (flat $0.01 USDC)
    fee_usdc_charged NUMERIC(20, 6) NOT NULL DEFAULT 0.01,

    -- Platform wallet used for sponsorship
    sponsor_wallet_pubkey VARCHAR(44) NOT NULL,

    -- ATA creation if needed
    ata_created BOOLEAN NOT NULL DEFAULT FALSE,
    ata_address VARCHAR(44),

    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uk_gas_sponsorships_payment UNIQUE (payment_id)
);

CREATE INDEX IF NOT EXISTS idx_gas_sponsorships_payment ON gas_sponsorships(payment_id);

-- 4. ledger_entries - Double-entry ledger for balance tracking
CREATE TABLE IF NOT EXISTS ledger_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id),

    -- Amount (positive = credit, negative = debit)
    amount_usdc NUMERIC(20, 6) NOT NULL,

    -- Entry type: Payment, PaymentReceived, Fee, Refund, Adjustment, Deposit, Withdrawal
    entry_type VARCHAR(30) NOT NULL,

    -- Reference to source transaction
    reference_type VARCHAR(30) NOT NULL, -- P2PPayment, GasSponsorship, Manual
    reference_id UUID NOT NULL,

    -- Running balance after this entry (denormalized for query performance)
    balance_after NUMERIC(20, 6) NOT NULL,

    -- Description
    description VARCHAR(255),

    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_ledger_entries_user ON ledger_entries(user_id);
CREATE INDEX IF NOT EXISTS idx_ledger_entries_user_created ON ledger_entries(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_ledger_entries_reference ON ledger_entries(reference_type, reference_id);

-- 5. platform_hot_wallets - Platform wallets for gas sponsorship and fee collection
CREATE TABLE IF NOT EXISTS platform_hot_wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Purpose: gas_sponsor, fee_collection
    wallet_type VARCHAR(30) NOT NULL,

    -- Wallet details
    public_key VARCHAR(44) NOT NULL,
    private_key_encrypted TEXT NOT NULL,

    -- Status
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uk_platform_hot_wallets_pubkey UNIQUE (public_key)
);

CREATE INDEX IF NOT EXISTS idx_platform_hot_wallets_type ON platform_hot_wallets(wallet_type);

-- =============================================
-- FUNCTIONS FOR BALANCE CALCULATION
-- =============================================

-- Function to get user's current USDC balance from ledger
CREATE OR REPLACE FUNCTION get_user_balance(p_user_id UUID)
RETURNS NUMERIC(20, 6) AS $$
DECLARE
    v_balance NUMERIC(20, 6);
BEGIN
    SELECT COALESCE(
        (SELECT balance_after
         FROM ledger_entries
         WHERE user_id = p_user_id
         ORDER BY created_at DESC
         LIMIT 1),
        0.000000
    ) INTO v_balance;

    RETURN v_balance;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- TRIGGER FOR UPDATED_AT
-- =============================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to tables with updated_at
DROP TRIGGER IF EXISTS wallets_updated_at ON wallets;
CREATE TRIGGER wallets_updated_at
    BEFORE UPDATE ON wallets
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS p2p_payments_updated_at ON p2p_payments;
CREATE TRIGGER p2p_payments_updated_at
    BEFORE UPDATE ON p2p_payments
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS platform_hot_wallets_updated_at ON platform_hot_wallets;
CREATE TRIGGER platform_hot_wallets_updated_at
    BEFORE UPDATE ON platform_hot_wallets
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
