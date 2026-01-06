-- ============================================
-- PRODUCTION MIGRATION: Full Support Intents Setup
-- Run this on a fresh server that doesn't have support_intents table
-- Date: 2025-01-06
-- ============================================

-- Ensure update_updated_at_column function exists
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- 1. CREATE SUPPORT_INTENTS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS support_intents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- The supporter
    supporter_user_id UUID NOT NULL REFERENCES users(user_id),

    -- The bird being supported (NULL for Wihngo-only support)
    bird_id UUID REFERENCES birds(bird_id) ON DELETE SET NULL,

    -- The bird owner (recipient of support)
    recipient_user_id UUID NOT NULL REFERENCES users(user_id),

    -- Amounts (all in selected currency)
    support_amount NUMERIC(20, 6) NOT NULL,
    platform_support_amount NUMERIC(20, 6) NOT NULL DEFAULT 0,
    total_amount NUMERIC(20, 6) NOT NULL,

    -- Bird-first model columns
    bird_amount NUMERIC(20, 6) DEFAULT 0,
    wihngo_support_amount NUMERIC(20, 6) DEFAULT 0,
    wihngo_wallet_pubkey VARCHAR(44),
    wihngo_solana_signature VARCHAR(88),

    -- Currency: USDC, EURC, USD
    currency VARCHAR(10) NOT NULL DEFAULT 'USDC',

    -- Status: pending, awaiting_payment, processing, completed, expired, cancelled, failed
    status VARCHAR(20) NOT NULL DEFAULT 'pending',

    -- Wallet info (null if not yet linked or custodial)
    sender_wallet_pubkey VARCHAR(44),
    recipient_wallet_pubkey VARCHAR(44),

    -- Payment method: wallet (Phantom/etc), custodial, pending
    payment_method VARCHAR(20) NOT NULL DEFAULT 'pending',

    -- Blockchain details (when payment is made)
    solana_signature VARCHAR(88),
    confirmations INT NOT NULL DEFAULT 0,

    -- Serialized transaction (for wallet-based payments)
    serialized_transaction TEXT,

    -- Platform fee (legacy, kept for compatibility)
    platform_fee_percent NUMERIC(5, 2) NOT NULL DEFAULT 5,

    -- Invoice number for compliance
    invoice_number VARCHAR(50),

    -- Idempotency and error tracking
    idempotency_key VARCHAR(255),
    error_message TEXT,

    -- Timestamps
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    paid_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT chk_support_amount_positive CHECK (support_amount > 0),
    CONSTRAINT chk_platform_support_non_negative CHECK (platform_support_amount >= 0),
    CONSTRAINT chk_total_equals_sum CHECK (total_amount = support_amount + platform_support_amount)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_support_intents_supporter ON support_intents(supporter_user_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_bird ON support_intents(bird_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_recipient ON support_intents(recipient_user_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_status ON support_intents(status);
CREATE INDEX IF NOT EXISTS idx_support_intents_created ON support_intents(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_support_intents_wihngo_amount ON support_intents(wihngo_support_amount) WHERE wihngo_support_amount > 0;
CREATE INDEX IF NOT EXISTS ix_support_intents_invoice_number ON support_intents(invoice_number) WHERE invoice_number IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_support_intents_idempotency ON support_intents(idempotency_key) WHERE idempotency_key IS NOT NULL;

-- Trigger for updated_at
DROP TRIGGER IF EXISTS support_intents_updated_at ON support_intents;
CREATE TRIGGER support_intents_updated_at
    BEFORE UPDATE ON support_intents
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Comments
COMMENT ON TABLE support_intents IS 'Bird support intents - Bird-First Payment Model: 100% of bird_amount to owner, wihngo_support is optional and additive';
COMMENT ON COLUMN support_intents.bird_id IS 'The bird being supported. NULL when supporting Wihngo platform directly without a specific bird.';
COMMENT ON COLUMN support_intents.bird_amount IS 'Amount going to bird owner - 100% untouched, never deducted from';
COMMENT ON COLUMN support_intents.wihngo_support_amount IS 'Optional support for Wihngo - additive, not deducted from bird amount. Minimum $0.05 if > 0';
COMMENT ON COLUMN support_intents.idempotency_key IS 'Unique key to prevent duplicate transactions';
COMMENT ON COLUMN support_intents.error_message IS 'Error message if the transaction failed or was rejected';

-- ============================================
-- 2. ADD needs_support TO BIRDS TABLE
-- ============================================
ALTER TABLE birds ADD COLUMN IF NOT EXISTS needs_support BOOLEAN NOT NULL DEFAULT false;

COMMENT ON COLUMN birds.needs_support IS
'When true, this bird appears in the "birds need support" list.
Set by the bird owner to indicate their bird needs community support.';

-- ============================================
-- 3. CREATE WEEKLY SUPPORT TRACKING TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS weekly_bird_support_rounds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    week_start_date DATE NOT NULL,
    times_supported INTEGER NOT NULL DEFAULT 0,
    last_supported_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,

    -- One record per bird per week
    CONSTRAINT uq_bird_week UNIQUE (bird_id, week_start_date)
);

CREATE INDEX IF NOT EXISTS idx_weekly_support_week ON weekly_bird_support_rounds(week_start_date);
CREATE INDEX IF NOT EXISTS idx_weekly_support_bird ON weekly_bird_support_rounds(bird_id);

COMMENT ON TABLE weekly_bird_support_rounds IS
'Tracks how many times each bird has received support in a given week.
Birds can receive support once per week (1 round).
After all birds supported, show thank you message. Resets every Sunday.';

-- ============================================
-- VERIFICATION
-- ============================================
DO $$
BEGIN
    RAISE NOTICE '✅ support_intents table created';
    RAISE NOTICE '✅ birds.needs_support column added';
    RAISE NOTICE '✅ weekly_bird_support_rounds table created';
    RAISE NOTICE '✅ All migrations complete!';
END $$;
