-- ============================================================================
-- PRODUCTION FRESH SETUP - Drop old tables and create new payment system
-- Date: 2026-01-04
-- WARNING: This will DELETE all existing data!
-- ============================================================================

-- Drop old payment-related tables (in correct order due to foreign keys)
DROP TABLE IF EXISTS crypto_payments CASCADE;
DROP TABLE IF EXISTS p2p_payments CASCADE;
DROP TABLE IF EXISTS support_intents CASCADE;
DROP TABLE IF EXISTS on_chain_deposits CASCADE;
DROP TABLE IF EXISTS onchain_deposits CASCADE;
DROP TABLE IF EXISTS ledger_entries CASCADE;
DROP TABLE IF EXISTS refund_requests CASCADE;
DROP TABLE IF EXISTS gas_sponsorships CASCADE;
DROP TABLE IF EXISTS wallet_connect_intents CASCADE;
DROP TABLE IF EXISTS webhooks_received CASCADE;
DROP TABLE IF EXISTS exchange_rates CASCADE;
DROP TABLE IF EXISTS blockchain_cursors CASCADE;
DROP TABLE IF EXISTS platform_hot_wallets CASCADE;
DROP TABLE IF EXISTS supported_tokens CASCADE;
DROP TABLE IF EXISTS token_configurations CASCADE;

-- Drop new payment tables if they exist (for re-run safety)
DROP TABLE IF EXISTS balances CASCADE;
DROP TABLE IF EXISTS settlements CASCADE;
DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS payment_derivation_counter CASCADE;

-- ============================================================================
-- CREATE NEW PAYMENT SYSTEM
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- PAYMENTS TABLE
CREATE TABLE payments (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID REFERENCES users(user_id),
    purpose             VARCHAR(20) NOT NULL,
    bird_id             UUID REFERENCES birds(bird_id),
    amount_cents        INTEGER NOT NULL,
    currency            CHAR(3) NOT NULL DEFAULT 'USD',
    provider            VARCHAR(30) NOT NULL,
    provider_ref        VARCHAR(255),
    status              VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    confirmed_at        TIMESTAMPTZ,
    destination_address VARCHAR(44),
    derivation_index    BIGINT,
    expires_at          TIMESTAMPTZ,
    claimed_at          TIMESTAMPTZ,
    buyer_email         VARCHAR(255),
    sweep_eligible_at   TIMESTAMPTZ,
    treasury_tx_hash    VARCHAR(128),
    swept_at            TIMESTAMPTZ,
    wihngo_amount_cents INTEGER DEFAULT 0,

    -- Constraints (updated for PLATFORM_SUPPORT)
    CONSTRAINT ck_payments_purpose_valid
        CHECK (purpose IN ('BIRD_SUPPORT', 'PAYOUT', 'REFUND', 'PLATFORM_SUPPORT')),
    CONSTRAINT ck_payments_amount_positive
        CHECK (
            (purpose = 'PLATFORM_SUPPORT' AND amount_cents >= 0) OR
            (purpose != 'PLATFORM_SUPPORT' AND amount_cents > 0)
        ),
    CONSTRAINT ck_payments_currency_valid
        CHECK (currency = 'USD'),
    CONSTRAINT ck_payments_provider_valid
        CHECK (provider IN ('USDC_SOLANA', 'STRIPE', 'PAYPAL', 'MANUAL', 'MANUAL_USDC_SOLANA')),
    CONSTRAINT ck_payments_status_valid
        CHECK (status IN ('pending', 'confirmed', 'failed', 'expired', 'sweep_eligible', 'swept')),
    CONSTRAINT ck_payments_confirmed_has_date
        CHECK (status != 'confirmed' OR confirmed_at IS NOT NULL)
);

-- Indexes
CREATE UNIQUE INDEX idx_payments_provider_ref_unique ON payments(provider_ref) WHERE provider_ref IS NOT NULL;
CREATE INDEX idx_payments_user_id ON payments(user_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_created_at ON payments(created_at DESC);
CREATE INDEX idx_payments_purpose ON payments(purpose);
CREATE INDEX idx_payments_bird_id ON payments(bird_id);
CREATE INDEX idx_payments_pending ON payments(created_at) WHERE status = 'pending';
CREATE INDEX idx_payments_pending_manual ON payments(expires_at) WHERE status = 'pending' AND provider = 'MANUAL_USDC_SOLANA' AND destination_address IS NOT NULL;
CREATE UNIQUE INDEX idx_payments_derivation_index_unique ON payments(derivation_index) WHERE derivation_index IS NOT NULL;
CREATE INDEX idx_payments_sweep_eligible ON payments(sweep_eligible_at) WHERE status = 'sweep_eligible' AND swept_at IS NULL;
CREATE INDEX idx_payments_swept ON payments(swept_at DESC) WHERE status = 'swept';
CREATE UNIQUE INDEX idx_payments_treasury_tx_hash_unique ON payments(treasury_tx_hash) WHERE treasury_tx_hash IS NOT NULL;

COMMENT ON TABLE payments IS 'Provider-agnostic payment ledger. Source of truth for all money movement.';
COMMENT ON COLUMN payments.purpose IS 'BIRD_SUPPORT=user supporting bird, PAYOUT=platform paying out, REFUND=money back to user, PLATFORM_SUPPORT=user supporting Wihngo platform';

-- DERIVATION COUNTER
CREATE TABLE payment_derivation_counter (
    id INTEGER PRIMARY KEY DEFAULT 1,
    next_index BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT payment_derivation_counter_single_row CHECK (id = 1)
);
INSERT INTO payment_derivation_counter (id, next_index) VALUES (1, 0);

-- SETTLEMENTS TABLE
CREATE TABLE settlements (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_id          UUID NOT NULL REFERENCES payments(id),
    bird_owner_id       UUID NOT NULL REFERENCES users(user_id),
    bird_id             UUID NOT NULL REFERENCES birds(bird_id),
    gross_amount_cents  INTEGER NOT NULL,
    platform_fee_cents  INTEGER NOT NULL,
    net_amount_cents    INTEGER NOT NULL,
    status              VARCHAR(20) NOT NULL DEFAULT 'locked',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_settlements_payment UNIQUE (payment_id),
    CONSTRAINT ck_settlements_amounts_valid CHECK (gross_amount_cents >= 0 AND platform_fee_cents >= 0 AND net_amount_cents >= 0),
    CONSTRAINT ck_settlements_net_calc CHECK (net_amount_cents = gross_amount_cents - platform_fee_cents),
    CONSTRAINT ck_settlements_status_valid CHECK (status IN ('locked', 'available', 'paid'))
);

CREATE INDEX idx_settlements_bird_owner_id ON settlements(bird_owner_id);
CREATE INDEX idx_settlements_bird_id ON settlements(bird_id);
CREATE INDEX idx_settlements_status ON settlements(status);
CREATE INDEX idx_settlements_created_at ON settlements(created_at DESC);
CREATE INDEX idx_settlements_available ON settlements(bird_owner_id, created_at) WHERE status = 'available';

-- BALANCES TABLE
CREATE TABLE balances (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    owner_type          VARCHAR(20) NOT NULL,
    owner_id            UUID NOT NULL,
    available_cents     INTEGER NOT NULL DEFAULT 0,
    locked_cents        INTEGER NOT NULL DEFAULT 0,
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_balances_owner UNIQUE (owner_type, owner_id),
    CONSTRAINT ck_balances_owner_type_valid CHECK (owner_type IN ('USER', 'PLATFORM')),
    CONSTRAINT ck_balances_amounts_non_negative CHECK (available_cents >= 0 AND locked_cents >= 0)
);

CREATE INDEX idx_balances_owner ON balances(owner_type, owner_id);

-- ============================================================================
-- DONE
-- ============================================================================
SELECT 'Production fresh setup complete!' AS status;
