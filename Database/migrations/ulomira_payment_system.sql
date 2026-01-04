-- ============================================================================
-- WIHNGO PLATFORM - ULOMIRA-STYLE PAYMENT SYSTEM
-- ============================================================================
-- Provider-agnostic payment infrastructure ported from Ulomira.
-- Adapted for bird support payments instead of book purchases.
--
-- Design principle: Platform reacts to payment EVENTS, not payment METHODS.
-- USDC, Stripe, PayPal - all produce the same internal payment records.
-- ============================================================================

-- Enable UUID generation extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- PART 1: PAYMENTS TABLE (Provider-Agnostic)
-- ============================================================================
-- Tracks payment intent -> confirmation lifecycle.
-- This is the SOURCE OF TRUTH for all money movement.
-- ============================================================================

CREATE TABLE IF NOT EXISTS payments (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID REFERENCES users(user_id),  -- NULL for anonymous manual payments

    -- Purpose of this payment
    purpose             VARCHAR(20) NOT NULL,

    -- Target entity (bird for BIRD_SUPPORT, null for others)
    bird_id             UUID REFERENCES birds(bird_id),

    -- Amount in smallest currency unit (cents for USD)
    amount_cents        INTEGER NOT NULL,

    -- Always USD internally (USDC is just an implementation detail)
    currency            CHAR(3) NOT NULL DEFAULT 'USD',

    -- Payment provider (switchable adapter)
    provider            VARCHAR(30) NOT NULL,

    -- External reference from provider (tx hash, Stripe ID, etc.)
    -- Unique when not null to prevent double-spend
    provider_ref        VARCHAR(255),

    -- Payment lifecycle status
    status              VARCHAR(20) NOT NULL DEFAULT 'pending',

    -- Timestamps
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    confirmed_at        TIMESTAMPTZ,

    -- Manual payment fields (HD-derived addresses)
    destination_address VARCHAR(44),
    derivation_index    BIGINT,
    expires_at          TIMESTAMPTZ,
    claimed_at          TIMESTAMPTZ,
    buyer_email         VARCHAR(255),

    -- Sweep fields (14-day delayed sweep to treasury)
    sweep_eligible_at   TIMESTAMPTZ,
    treasury_tx_hash    VARCHAR(128),
    swept_at            TIMESTAMPTZ,

    -- Wihngo support (optional additional amount to platform)
    wihngo_amount_cents INTEGER DEFAULT 0,

    -- Constraints
    CONSTRAINT ck_payments_purpose_valid
        CHECK (purpose IN ('BIRD_SUPPORT', 'PAYOUT', 'REFUND')),
    CONSTRAINT ck_payments_amount_positive
        CHECK (amount_cents > 0),
    CONSTRAINT ck_payments_currency_valid
        CHECK (currency = 'USD'),
    CONSTRAINT ck_payments_provider_valid
        CHECK (provider IN ('USDC_SOLANA', 'STRIPE', 'PAYPAL', 'MANUAL', 'MANUAL_USDC_SOLANA')),
    CONSTRAINT ck_payments_status_valid
        CHECK (status IN ('pending', 'confirmed', 'failed', 'expired', 'sweep_eligible', 'swept')),
    CONSTRAINT ck_payments_confirmed_has_date
        CHECK (status != 'confirmed' OR confirmed_at IS NOT NULL)
);

-- Unique constraint on provider_ref when not null (prevents double-spend)
CREATE UNIQUE INDEX IF NOT EXISTS idx_payments_provider_ref_unique
ON payments(provider_ref)
WHERE provider_ref IS NOT NULL;

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_payments_user_id ON payments(user_id);
CREATE INDEX IF NOT EXISTS idx_payments_status ON payments(status);
CREATE INDEX IF NOT EXISTS idx_payments_created_at ON payments(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_payments_purpose ON payments(purpose);
CREATE INDEX IF NOT EXISTS idx_payments_bird_id ON payments(bird_id);

-- Partial index for pending payments (for monitoring/retry logic)
CREATE INDEX IF NOT EXISTS idx_payments_pending
ON payments(created_at)
WHERE status = 'pending';

-- Index for pending manual payments
CREATE INDEX IF NOT EXISTS idx_payments_pending_manual
ON payments(expires_at)
WHERE status = 'pending' AND provider = 'MANUAL_USDC_SOLANA' AND destination_address IS NOT NULL;

-- Index for derivation index uniqueness (prevents address reuse)
CREATE UNIQUE INDEX IF NOT EXISTS idx_payments_derivation_index_unique
ON payments(derivation_index)
WHERE derivation_index IS NOT NULL;

-- Sweep indexes
CREATE INDEX IF NOT EXISTS idx_payments_sweep_eligible
ON payments(sweep_eligible_at)
WHERE status = 'sweep_eligible' AND swept_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_payments_swept
ON payments(swept_at DESC)
WHERE status = 'swept';

CREATE UNIQUE INDEX IF NOT EXISTS idx_payments_treasury_tx_hash_unique
ON payments(treasury_tx_hash)
WHERE treasury_tx_hash IS NOT NULL;

COMMENT ON TABLE payments IS 'Provider-agnostic payment ledger. Source of truth for all money movement.';
COMMENT ON COLUMN payments.purpose IS 'BIRD_SUPPORT=user supporting bird, PAYOUT=platform paying out, REFUND=money back to user';
COMMENT ON COLUMN payments.amount_cents IS 'Amount in cents. Always positive.';
COMMENT ON COLUMN payments.currency IS 'Always USD internally. USDC is just an implementation detail.';
COMMENT ON COLUMN payments.provider IS 'Payment adapter: USDC_SOLANA, STRIPE, PAYPAL, MANUAL, MANUAL_USDC_SOLANA';
COMMENT ON COLUMN payments.provider_ref IS 'External reference (tx hash, Stripe payment_intent ID, etc.). Unique to prevent double-spend.';
COMMENT ON COLUMN payments.status IS 'pending=awaiting confirmation, confirmed=success, failed=declined, expired=timeout, sweep_eligible=can sweep, swept=finalized';

-- ============================================================================
-- PART 2: ATOMIC COUNTER FOR HD DERIVATION INDICES
-- ============================================================================
-- Single-row table ensures atomicity via UPDATE ... RETURNING
-- ============================================================================

CREATE TABLE IF NOT EXISTS payment_derivation_counter (
    id INTEGER PRIMARY KEY DEFAULT 1,
    next_index BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT payment_derivation_counter_single_row CHECK (id = 1)
);

INSERT INTO payment_derivation_counter (id, next_index)
VALUES (1, 0)
ON CONFLICT (id) DO NOTHING;

COMMENT ON TABLE payment_derivation_counter IS 'Atomic counter for HD wallet derivation indices. Single-row table.';

-- ============================================================================
-- PART 3: SETTLEMENTS TABLE (Bird Owner Revenue)
-- ============================================================================
-- Immutable revenue breakdown per payment.
-- Created when a BIRD_SUPPORT payment is confirmed.
-- Links payment to bird owner earnings.
-- ============================================================================

CREATE TABLE IF NOT EXISTS settlements (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_id          UUID NOT NULL REFERENCES payments(id),
    bird_owner_id       UUID NOT NULL REFERENCES users(user_id),
    bird_id             UUID NOT NULL REFERENCES birds(bird_id),

    -- Revenue breakdown (in cents)
    gross_amount_cents  INTEGER NOT NULL,
    platform_fee_cents  INTEGER NOT NULL,
    net_amount_cents    INTEGER NOT NULL,

    -- Settlement lifecycle
    -- LOCKED = within refund window, not yet available for payout
    -- AVAILABLE = refund window passed, can be paid out
    -- PAID = included in a payout
    status              VARCHAR(20) NOT NULL DEFAULT 'locked',

    -- Timestamps
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Constraints
    CONSTRAINT uq_settlements_payment UNIQUE (payment_id),
    CONSTRAINT ck_settlements_amounts_valid
        CHECK (gross_amount_cents >= 0 AND platform_fee_cents >= 0 AND net_amount_cents >= 0),
    CONSTRAINT ck_settlements_net_calc
        CHECK (net_amount_cents = gross_amount_cents - platform_fee_cents),
    CONSTRAINT ck_settlements_status_valid
        CHECK (status IN ('locked', 'available', 'paid'))
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_settlements_bird_owner_id ON settlements(bird_owner_id);
CREATE INDEX IF NOT EXISTS idx_settlements_bird_id ON settlements(bird_id);
CREATE INDEX IF NOT EXISTS idx_settlements_status ON settlements(status);
CREATE INDEX IF NOT EXISTS idx_settlements_created_at ON settlements(created_at DESC);

-- Partial index for available settlements (payout candidates)
CREATE INDEX IF NOT EXISTS idx_settlements_available
ON settlements(bird_owner_id, created_at)
WHERE status = 'available';

COMMENT ON TABLE settlements IS 'Immutable bird owner revenue breakdown per payment. Created on confirmed BIRD_SUPPORT.';
COMMENT ON COLUMN settlements.gross_amount_cents IS 'Full payment amount before platform fee';
COMMENT ON COLUMN settlements.platform_fee_cents IS 'Wihngo platform fee (configurable %)';
COMMENT ON COLUMN settlements.net_amount_cents IS 'Amount payable to bird owner';
COMMENT ON COLUMN settlements.status IS 'locked=in refund window, available=can be paid, paid=in payout';

-- ============================================================================
-- PART 4: BALANCES TABLE (Internal Wallet Abstraction)
-- ============================================================================
-- Abstract wallet with NO crypto awareness.
-- Tracks available and locked amounts for users and platform.
-- ============================================================================

CREATE TABLE IF NOT EXISTS balances (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    owner_type          VARCHAR(20) NOT NULL,
    owner_id            UUID NOT NULL,

    -- Balance amounts (in cents)
    available_cents     INTEGER NOT NULL DEFAULT 0,
    locked_cents        INTEGER NOT NULL DEFAULT 0,

    -- Last update timestamp
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Constraints
    CONSTRAINT uq_balances_owner UNIQUE (owner_type, owner_id),
    CONSTRAINT ck_balances_owner_type_valid
        CHECK (owner_type IN ('USER', 'PLATFORM')),
    CONSTRAINT ck_balances_amounts_non_negative
        CHECK (available_cents >= 0 AND locked_cents >= 0)
);

-- Index for quick lookups
CREATE INDEX IF NOT EXISTS idx_balances_owner ON balances(owner_type, owner_id);

COMMENT ON TABLE balances IS 'Internal wallet abstraction. No crypto awareness - just USD amounts.';
COMMENT ON COLUMN balances.owner_type IS 'USER=bird owner, PLATFORM=Wihngo';
COMMENT ON COLUMN balances.available_cents IS 'Funds available for withdrawal/use';
COMMENT ON COLUMN balances.locked_cents IS 'Funds locked (e.g., in refund window)';

-- ============================================================================
-- MIGRATION COMPLETE
-- ============================================================================
