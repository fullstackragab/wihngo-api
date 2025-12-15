-- ================================================================
-- WIHNGO PAYOUT SYSTEM MIGRATION
-- ================================================================
-- This migration creates the multi-payout strategy tables for bird owners
-- Supports IBAN/SEPA, PayPal, and Crypto (USDC/EURC on Solana & Base)
-- ================================================================

-- Drop existing tables if they exist (for development)
DROP TABLE IF EXISTS payout_transactions CASCADE;
DROP TABLE IF EXISTS payout_methods CASCADE;
DROP TABLE IF EXISTS payout_balances CASCADE;

-- ================================================================
-- 1. PAYOUT METHODS TABLE
-- ================================================================
-- Stores payment methods for bird owners
CREATE TABLE payout_methods (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    method_type VARCHAR(50) NOT NULL,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- IBAN/SEPA fields
    account_holder_name VARCHAR(255),
    iban VARCHAR(34),
    bic VARCHAR(11),
    bank_name VARCHAR(255),

    -- PayPal fields
    paypal_email VARCHAR(255),

    -- Crypto fields
    wallet_address VARCHAR(255),
    network VARCHAR(50),
    currency VARCHAR(10),

    CONSTRAINT fk_payout_methods_users FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE CASCADE,
    
    CONSTRAINT chk_method_type CHECK (method_type IN (
        'iban', 'paypal', 'usdc-solana', 'eurc-solana', 'usdc-base', 'eurc-base'
    ))
);

-- Indexes for performance
CREATE INDEX idx_payout_methods_user_id ON payout_methods(user_id);
CREATE INDEX idx_payout_methods_is_default ON payout_methods(user_id, is_default);
CREATE INDEX idx_payout_methods_method_type ON payout_methods(method_type);

-- Comments for documentation
COMMENT ON TABLE payout_methods IS 'Payment methods for bird owner payouts';
COMMENT ON COLUMN payout_methods.method_type IS 'Payment method: iban, paypal, usdc-solana, eurc-solana, usdc-base, eurc-base';
COMMENT ON COLUMN payout_methods.is_default IS 'Whether this is the default payout method for the user';
COMMENT ON COLUMN payout_methods.is_verified IS 'Whether this payment method has been verified';

-- ================================================================
-- 2. PAYOUT TRANSACTIONS TABLE
-- ================================================================
-- Tracks all payout transactions
CREATE TABLE payout_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    payout_method_id UUID NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    currency VARCHAR(10) NOT NULL DEFAULT 'EUR',
    status VARCHAR(50) NOT NULL DEFAULT 'pending',

    -- Fee breakdown
    platform_fee DECIMAL(18, 2) NOT NULL DEFAULT 0,
    provider_fee DECIMAL(18, 2) NOT NULL DEFAULT 0,
    net_amount DECIMAL(18, 2) NOT NULL,

    -- Processing details
    scheduled_at TIMESTAMPTZ NOT NULL,
    processed_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    failure_reason VARCHAR(500),
    transaction_id VARCHAR(255),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_payout_transactions_users FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_payout_transactions_methods FOREIGN KEY (payout_method_id) 
        REFERENCES payout_methods(id) ON DELETE RESTRICT,
    
    CONSTRAINT chk_payout_status CHECK (status IN (
        'pending', 'processing', 'completed', 'failed', 'cancelled'
    )),
    
    CONSTRAINT chk_payout_amount CHECK (amount > 0),
    CONSTRAINT chk_payout_net_amount CHECK (net_amount > 0)
);

-- Indexes for performance
CREATE INDEX idx_payout_transactions_user_id ON payout_transactions(user_id);
CREATE INDEX idx_payout_transactions_status ON payout_transactions(status);
CREATE INDEX idx_payout_transactions_scheduled_at ON payout_transactions(scheduled_at);
CREATE INDEX idx_payout_transactions_created_at ON payout_transactions(created_at DESC);

-- Comments for documentation
COMMENT ON TABLE payout_transactions IS 'Payout transaction records';
COMMENT ON COLUMN payout_transactions.status IS 'Transaction status: pending, processing, completed, failed, cancelled';
COMMENT ON COLUMN payout_transactions.platform_fee IS 'Wihngo platform fee (5%)';
COMMENT ON COLUMN payout_transactions.provider_fee IS 'Payment provider fee (varies by method)';
COMMENT ON COLUMN payout_transactions.net_amount IS 'Final amount sent to bird owner';
COMMENT ON COLUMN payout_transactions.transaction_id IS 'External payment provider transaction ID';

-- ================================================================
-- 3. PAYOUT BALANCES TABLE
-- ================================================================
-- Tracks available balance for each bird owner
CREATE TABLE payout_balances (
    user_id UUID PRIMARY KEY,
    available_balance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    pending_balance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    currency VARCHAR(10) NOT NULL DEFAULT 'EUR',
    last_payout_date TIMESTAMPTZ,
    next_payout_date TIMESTAMPTZ NOT NULL DEFAULT (DATE_TRUNC('month', NOW()) + INTERVAL '1 month'),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_payout_balances_users FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE CASCADE,
    
    CONSTRAINT chk_available_balance CHECK (available_balance >= 0),
    CONSTRAINT chk_pending_balance CHECK (pending_balance >= 0)
);

-- Comments for documentation
COMMENT ON TABLE payout_balances IS 'Available payout balances for bird owners';
COMMENT ON COLUMN payout_balances.available_balance IS 'Balance available for withdrawal (95% of received support minus platform fee)';
COMMENT ON COLUMN payout_balances.pending_balance IS 'Balance being processed or in clearing';
COMMENT ON COLUMN payout_balances.next_payout_date IS 'Next scheduled payout date (1st of next month)';

-- ================================================================
-- 4. TRIGGER TO AUTOMATICALLY UPDATE BALANCES
-- ================================================================
-- Automatically update payout balance when support transactions are added

CREATE OR REPLACE FUNCTION update_payout_balance_on_support() 
RETURNS TRIGGER AS $$
BEGIN
    -- Insert or update payout balance for the bird owner
    -- Calculate 95% of the support amount (5% platform fee)
    INSERT INTO payout_balances (user_id, available_balance, updated_at)
    SELECT 
        b.owner_id,
        NEW.amount * 0.95,
        NOW()
    FROM birds b
    WHERE b.bird_id = NEW.bird_id
    ON CONFLICT (user_id) 
    DO UPDATE SET
        available_balance = payout_balances.available_balance + (NEW.amount * 0.95),
        updated_at = NOW();
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_payout_balance
    AFTER INSERT ON support_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_payout_balance_on_support();

COMMENT ON TRIGGER trigger_update_payout_balance ON support_transactions 
    IS 'Automatically updates bird owner payout balance when support is received';

-- ================================================================
-- 5. FUNCTION TO CALCULATE TOTAL EARNINGS
-- ================================================================
-- Helper function to get total earnings for a bird owner

CREATE OR REPLACE FUNCTION get_bird_owner_earnings(owner_user_id UUID)
RETURNS TABLE (
    total_earned DECIMAL(18, 2),
    total_paid_out DECIMAL(18, 2),
    platform_fee_paid DECIMAL(18, 2),
    provider_fees_paid DECIMAL(18, 2),
    available_balance DECIMAL(18, 2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(
            (SELECT SUM(st.amount * 0.95)
             FROM support_transactions st
             JOIN birds b ON b.bird_id = st.bird_id
             WHERE b.owner_id = owner_user_id), 0
        ) AS total_earned,
        COALESCE(
            (SELECT SUM(pt.net_amount)
             FROM payout_transactions pt
             WHERE pt.user_id = owner_user_id AND pt.status = 'completed'), 0
        ) AS total_paid_out,
        COALESCE(
            (SELECT SUM(pt.platform_fee)
             FROM payout_transactions pt
             WHERE pt.user_id = owner_user_id AND pt.status = 'completed'), 0
        ) AS platform_fee_paid,
        COALESCE(
            (SELECT SUM(pt.provider_fee)
             FROM payout_transactions pt
             WHERE pt.user_id = owner_user_id AND pt.status = 'completed'), 0
        ) AS provider_fees_paid,
        COALESCE(pb.available_balance, 0) AS available_balance
    FROM payout_balances pb
    WHERE pb.user_id = owner_user_id;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION get_bird_owner_earnings(UUID) 
    IS 'Calculates total earnings summary for a bird owner';

-- ================================================================
-- 6. FUNCTION TO ENFORCE SINGLE DEFAULT PAYOUT METHOD
-- ================================================================
-- Ensure only one payout method is marked as default per user

CREATE OR REPLACE FUNCTION enforce_single_default_payout_method()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.is_default = TRUE THEN
        -- Unset all other default methods for this user
        UPDATE payout_methods
        SET is_default = FALSE, updated_at = NOW()
        WHERE user_id = NEW.user_id 
          AND id != NEW.id
          AND is_default = TRUE;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_enforce_single_default
    BEFORE INSERT OR UPDATE OF is_default ON payout_methods
    FOR EACH ROW
    WHEN (NEW.is_default = TRUE)
    EXECUTE FUNCTION enforce_single_default_payout_method();

COMMENT ON TRIGGER trigger_enforce_single_default ON payout_methods
    IS 'Ensures only one payout method is marked as default per user';

-- ================================================================
-- 7. FUNCTION TO UPDATE TIMESTAMPS
-- ================================================================
-- Automatically update updated_at timestamp

CREATE OR REPLACE FUNCTION update_modified_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_payout_methods_updated_at
    BEFORE UPDATE ON payout_methods
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_timestamp();

CREATE TRIGGER trigger_payout_transactions_updated_at
    BEFORE UPDATE ON payout_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_timestamp();

CREATE TRIGGER trigger_payout_balances_updated_at
    BEFORE UPDATE ON payout_balances
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_timestamp();

-- ================================================================
-- 8. SEED NEXT PAYOUT DATE FOR EXISTING USERS
-- ================================================================
-- Initialize payout balance records for existing users who own birds

INSERT INTO payout_balances (user_id, available_balance, next_payout_date, updated_at)
SELECT DISTINCT 
    b.owner_id,
    0,
    DATE_TRUNC('month', NOW()) + INTERVAL '1 month',
    NOW()
FROM birds b
WHERE NOT EXISTS (
    SELECT 1 FROM payout_balances pb WHERE pb.user_id = b.owner_id
)
ON CONFLICT (user_id) DO NOTHING;

-- ================================================================
-- 9. GRANT PERMISSIONS (if needed)
-- ================================================================
-- Grant necessary permissions to application user
-- Adjust the username as needed for your environment

-- GRANT ALL PRIVILEGES ON TABLE payout_methods TO your_app_user;
-- GRANT ALL PRIVILEGES ON TABLE payout_transactions TO your_app_user;
-- GRANT ALL PRIVILEGES ON TABLE payout_balances TO your_app_user;

-- ================================================================
-- MIGRATION COMPLETE
-- ================================================================

SELECT 'Payout system migration completed successfully!' AS status;
