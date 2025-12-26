-- Support Intents Migration
-- Creates table for bird support (donation) intents
-- Supports both wallet-based and custodial payments

-- =============================================
-- SUPPORT INTENTS TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS support_intents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- The supporter
    supporter_user_id UUID NOT NULL REFERENCES users(user_id),

    -- The bird being supported
    bird_id UUID NOT NULL REFERENCES birds(bird_id),

    -- The bird owner (recipient of support)
    recipient_user_id UUID NOT NULL REFERENCES users(user_id),

    -- Amounts (all in selected currency)
    support_amount NUMERIC(20, 6) NOT NULL,
    platform_support_amount NUMERIC(20, 6) NOT NULL DEFAULT 0,
    total_amount NUMERIC(20, 6) NOT NULL,

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

CREATE INDEX IF NOT EXISTS idx_support_intents_supporter ON support_intents(supporter_user_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_bird ON support_intents(bird_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_recipient ON support_intents(recipient_user_id);
CREATE INDEX IF NOT EXISTS idx_support_intents_status ON support_intents(status);
CREATE INDEX IF NOT EXISTS idx_support_intents_created ON support_intents(created_at DESC);

-- Trigger for updated_at
DROP TRIGGER IF EXISTS support_intents_updated_at ON support_intents;
CREATE TRIGGER support_intents_updated_at
    BEFORE UPDATE ON support_intents
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
