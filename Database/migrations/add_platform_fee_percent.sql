-- Add platform fee percent column to support_intents table
-- This tracks the percentage used (e.g., 5 for 5%)

ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS platform_fee_percent NUMERIC(5, 2) NOT NULL DEFAULT 5;

-- Add comment for documentation
COMMENT ON COLUMN support_intents.platform_fee_percent IS 'Platform fee percentage (e.g., 5 for 5%)';

-- Rename platform_support_amount to platform_fee for clarity (optional - keeping for backward compatibility)
-- ALTER TABLE support_intents RENAME COLUMN platform_support_amount TO platform_fee;
