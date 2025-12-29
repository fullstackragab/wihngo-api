-- Wallet Connect Intents Migration
-- Solves Android browser-switch problem for Phantom wallet connections
--
-- When Phantom deep-links back after signing, Android may open a different browser,
-- losing the user's JWT session. This table enables stateless recovery via the callback.

-- =============================================
-- CREATE WALLET CONNECT INTENTS TABLE
-- =============================================

CREATE TABLE IF NOT EXISTS wallet_connect_intents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- User who initiated (nullable for anonymous/pre-auth flows)
    user_id UUID REFERENCES users(user_id) ON DELETE SET NULL,

    -- State token for CSRF protection and intent matching
    -- Passed to Phantom, returned in callback
    state VARCHAR(64) NOT NULL,

    -- Nonce for signature verification (message the user signs)
    nonce VARCHAR(128) NOT NULL,

    -- Purpose: connect, sign, transaction, support, payment
    purpose VARCHAR(50) NOT NULL DEFAULT 'connect',

    -- Status: pending, awaiting_callback, processing, completed, expired, cancelled, failed
    status VARCHAR(20) NOT NULL DEFAULT 'pending',

    -- Wallet details (populated after callback)
    public_key VARCHAR(44),
    wallet_provider VARCHAR(50) NOT NULL DEFAULT 'phantom',
    signature VARCHAR(128),

    -- Optional redirect after completion
    redirect_url VARCHAR(500),

    -- Client-specific metadata (JSON)
    metadata JSONB,

    -- Audit fields
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),

    -- Timestamps
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- State must be unique (one-time use)
    CONSTRAINT uk_wallet_connect_intents_state UNIQUE (state)
);

-- =============================================
-- INDEXES
-- =============================================

-- Fast lookup by state (callback validation)
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_state
    ON wallet_connect_intents(state);

-- Find pending intents for a user
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_user_status
    ON wallet_connect_intents(user_id, status);

-- Cleanup expired intents
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_expires
    ON wallet_connect_intents(expires_at)
    WHERE status = 'pending' OR status = 'awaiting_callback';

-- Recent intents (for recovery flows)
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_created
    ON wallet_connect_intents(created_at DESC);

-- Find completed intents by public key (for linking verification)
CREATE INDEX IF NOT EXISTS idx_wallet_connect_intents_pubkey
    ON wallet_connect_intents(public_key)
    WHERE status = 'completed';

-- =============================================
-- UPDATED_AT TRIGGER
-- =============================================

DROP TRIGGER IF EXISTS wallet_connect_intents_updated_at ON wallet_connect_intents;
CREATE TRIGGER wallet_connect_intents_updated_at
    BEFORE UPDATE ON wallet_connect_intents
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- CLEANUP FUNCTION (optional - can be called by cron/hangfire)
-- =============================================

-- Function to expire old pending intents
CREATE OR REPLACE FUNCTION expire_wallet_connect_intents()
RETURNS INTEGER AS $$
DECLARE
    expired_count INTEGER;
BEGIN
    UPDATE wallet_connect_intents
    SET status = 'expired',
        updated_at = CURRENT_TIMESTAMP
    WHERE status IN ('pending', 'awaiting_callback')
      AND expires_at < CURRENT_TIMESTAMP;

    GET DIAGNOSTICS expired_count = ROW_COUNT;
    RETURN expired_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- COMMENTS
-- =============================================

COMMENT ON TABLE wallet_connect_intents IS
'Tracks wallet connection intents for handling Android browser-switch during Phantom deep-linking';

COMMENT ON COLUMN wallet_connect_intents.state IS
'Random CSRF token passed to Phantom and returned in callback - must be unique per intent';

COMMENT ON COLUMN wallet_connect_intents.nonce IS
'Message the user signs in their wallet - used for ownership verification';

COMMENT ON COLUMN wallet_connect_intents.purpose IS
'Why the wallet is being connected: connect, sign, transaction, support, payment';

COMMENT ON COLUMN wallet_connect_intents.status IS
'Intent lifecycle: pending → awaiting_callback → processing → completed/expired/cancelled/failed';
