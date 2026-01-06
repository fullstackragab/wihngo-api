-- Weekly Support Subscriptions Migration
-- Enables users to subscribe to weekly bird support with reminder-based approval

-- Subscriptions table: tracks user's weekly support commitments
CREATE TABLE IF NOT EXISTS weekly_support_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Subscriber (the user paying)
    subscriber_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,

    -- Bird being supported
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,

    -- Recipient (bird owner)
    recipient_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,

    -- Amount configuration
    amount_usdc NUMERIC(12, 6) NOT NULL DEFAULT 1.00,
    wihngo_support_amount NUMERIC(12, 6) NOT NULL DEFAULT 0.00,
    currency VARCHAR(10) NOT NULL DEFAULT 'USDC',

    -- Status: active, paused, cancelled
    status VARCHAR(20) NOT NULL DEFAULT 'active',

    -- Scheduling preferences
    day_of_week INTEGER NOT NULL DEFAULT 0,  -- 0=Sunday, 6=Saturday
    preferred_hour INTEGER NOT NULL DEFAULT 10,  -- UTC hour (0-23)

    -- Reminder tracking
    next_reminder_at TIMESTAMP WITH TIME ZONE,
    last_payment_at TIMESTAMP WITH TIME ZONE,

    -- Statistics
    total_payments_count INTEGER NOT NULL DEFAULT 0,
    total_amount_paid NUMERIC(16, 6) NOT NULL DEFAULT 0,
    consecutive_missed_count INTEGER NOT NULL DEFAULT 0,

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    paused_at TIMESTAMP WITH TIME ZONE,
    cancelled_at TIMESTAMP WITH TIME ZONE,

    -- Constraints
    CONSTRAINT chk_weekly_amount_positive CHECK (amount_usdc > 0),
    CONSTRAINT chk_weekly_wihngo_non_negative CHECK (wihngo_support_amount >= 0),
    CONSTRAINT chk_weekly_day_of_week CHECK (day_of_week >= 0 AND day_of_week <= 6),
    CONSTRAINT chk_weekly_hour CHECK (preferred_hour >= 0 AND preferred_hour <= 23),
    CONSTRAINT uq_weekly_subscriber_bird UNIQUE (subscriber_user_id, bird_id)
);

-- Indexes for efficient queries
CREATE INDEX IF NOT EXISTS idx_weekly_support_subscriber ON weekly_support_subscriptions(subscriber_user_id);
CREATE INDEX IF NOT EXISTS idx_weekly_support_bird ON weekly_support_subscriptions(bird_id);
CREATE INDEX IF NOT EXISTS idx_weekly_support_status ON weekly_support_subscriptions(status);
CREATE INDEX IF NOT EXISTS idx_weekly_support_next_reminder ON weekly_support_subscriptions(next_reminder_at)
    WHERE status = 'active';
CREATE INDEX IF NOT EXISTS idx_weekly_support_recipient ON weekly_support_subscriptions(recipient_user_id);

-- Payment tracking table: tracks each weekly payment attempt
CREATE TABLE IF NOT EXISTS weekly_support_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Link to subscription
    subscription_id UUID NOT NULL REFERENCES weekly_support_subscriptions(id) ON DELETE CASCADE,

    -- Link to support intent (when user approves)
    support_intent_id UUID REFERENCES support_intents(id),

    -- Week period this payment covers
    week_start_date DATE NOT NULL,
    week_end_date DATE NOT NULL,

    -- Status: pending_reminder, reminder_sent, intent_created, completed, expired, skipped
    status VARCHAR(30) NOT NULL DEFAULT 'pending_reminder',

    -- Amount details (copied from subscription at creation time)
    amount_usdc NUMERIC(12, 6) NOT NULL,
    wihngo_support_amount NUMERIC(12, 6) NOT NULL DEFAULT 0,

    -- Reminder tracking
    reminder_sent_at TIMESTAMP WITH TIME ZONE,
    reminder_push_sent BOOLEAN NOT NULL DEFAULT FALSE,
    reminder_email_sent BOOLEAN NOT NULL DEFAULT FALSE,

    -- Intent/Payment tracking
    intent_created_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,

    -- Error tracking
    error_message VARCHAR(500),

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT uq_weekly_subscription_week UNIQUE (subscription_id, week_start_date)
);

-- Indexes for payment queries
CREATE INDEX IF NOT EXISTS idx_weekly_payment_subscription ON weekly_support_payments(subscription_id);
CREATE INDEX IF NOT EXISTS idx_weekly_payment_status ON weekly_support_payments(status);
CREATE INDEX IF NOT EXISTS idx_weekly_payment_intent ON weekly_support_payments(support_intent_id);
CREATE INDEX IF NOT EXISTS idx_weekly_payment_week ON weekly_support_payments(week_start_date);
CREATE INDEX IF NOT EXISTS idx_weekly_payment_expires ON weekly_support_payments(expires_at)
    WHERE status IN ('reminder_sent', 'intent_created');

-- Trigger for updated_at on subscriptions
CREATE OR REPLACE FUNCTION update_weekly_support_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS weekly_support_subscriptions_updated_at ON weekly_support_subscriptions;
CREATE TRIGGER weekly_support_subscriptions_updated_at
    BEFORE UPDATE ON weekly_support_subscriptions
    FOR EACH ROW
    EXECUTE FUNCTION update_weekly_support_updated_at();

DROP TRIGGER IF EXISTS weekly_support_payments_updated_at ON weekly_support_payments;
CREATE TRIGGER weekly_support_payments_updated_at
    BEFORE UPDATE ON weekly_support_payments
    FOR EACH ROW
    EXECUTE FUNCTION update_weekly_support_updated_at();
