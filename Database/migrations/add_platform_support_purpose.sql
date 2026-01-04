-- Migration: Add PLATFORM_SUPPORT payment purpose
-- Date: 2026-01-04
-- Description: Allow platform-only donations without a bird

-- 1. Drop and recreate purpose constraint to include PLATFORM_SUPPORT
ALTER TABLE payments DROP CONSTRAINT IF EXISTS ck_payments_purpose_valid;
ALTER TABLE payments ADD CONSTRAINT ck_payments_purpose_valid
    CHECK (purpose IN ('BIRD_SUPPORT', 'PAYOUT', 'REFUND', 'PLATFORM_SUPPORT'));

-- 2. Drop and recreate amount constraint to allow 0 for PLATFORM_SUPPORT
ALTER TABLE payments DROP CONSTRAINT IF EXISTS ck_payments_amount_positive;
ALTER TABLE payments ADD CONSTRAINT ck_payments_amount_positive
    CHECK (
        (purpose = 'PLATFORM_SUPPORT' AND amount_cents >= 0) OR
        (purpose != 'PLATFORM_SUPPORT' AND amount_cents > 0)
    );

-- 3. Update comments
COMMENT ON COLUMN payments.purpose IS 'BIRD_SUPPORT=user supporting bird, PAYOUT=platform paying out, REFUND=money back to user, PLATFORM_SUPPORT=user supporting Wihngo platform directly';
