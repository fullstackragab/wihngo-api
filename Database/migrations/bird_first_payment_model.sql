-- ============================================
-- Migration: Bird-First Payment Model
-- Description: Update support_intents table for bird-first payment model
--
-- Changes:
-- - Rename support_amount → bird_amount (100% to bird owner)
-- - Rename platform_support_amount → wihngo_support_amount (optional, additive)
-- - Remove platform_fee_percent (no longer percentage-based)
-- - Add wihngo_wallet_pubkey column
-- - Add wihngo_solana_signature column
--
-- Bird-First Principles:
-- 1. 100% of bird_amount goes to bird owner (NEVER deducted)
-- 2. wihngo_support_amount is OPTIONAL and ADDITIVE (not a percentage)
-- 3. Minimum Wihngo support: $0.05 (if > 0), can be $0
-- 4. Two separate on-chain USDC transfers
-- ============================================

-- Step 1: Add new columns
ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS bird_amount NUMERIC(20, 6) DEFAULT 0;

ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS wihngo_support_amount NUMERIC(20, 6) DEFAULT 0;

ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS wihngo_wallet_pubkey VARCHAR(44);

ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS wihngo_solana_signature VARCHAR(88);

-- Step 2: Migrate data from old columns to new columns
-- support_amount becomes bird_amount
-- platform_support_amount becomes wihngo_support_amount (but meaning changes - now additive)
UPDATE support_intents
SET
    bird_amount = COALESCE(support_amount, 0),
    wihngo_support_amount = 0  -- Reset to 0 since old fee model was percentage-based
WHERE bird_amount = 0 OR bird_amount IS NULL;

-- Step 3: Drop old columns (run after verifying data migration)
-- Note: Uncomment these after verifying the migration works
-- ALTER TABLE support_intents DROP COLUMN IF EXISTS support_amount;
-- ALTER TABLE support_intents DROP COLUMN IF EXISTS platform_support_amount;
-- ALTER TABLE support_intents DROP COLUMN IF EXISTS platform_fee_percent;

-- Step 4: Add comment explaining the bird-first model
COMMENT ON TABLE support_intents IS 'Bird support intents - Bird-First Payment Model: 100% of bird_amount to owner, wihngo_support is optional and additive';
COMMENT ON COLUMN support_intents.bird_amount IS 'Amount going to bird owner - 100% untouched, never deducted from';
COMMENT ON COLUMN support_intents.wihngo_support_amount IS 'Optional support for Wihngo - additive, not deducted from bird amount. Minimum $0.05 if > 0';
COMMENT ON COLUMN support_intents.wihngo_wallet_pubkey IS 'Wihngo treasury wallet for receiving optional platform support';
COMMENT ON COLUMN support_intents.wihngo_solana_signature IS 'Solana signature for Wihngo support transfer (if applicable)';

-- Step 5: Create index for efficient queries
CREATE INDEX IF NOT EXISTS idx_support_intents_wihngo_amount
ON support_intents(wihngo_support_amount)
WHERE wihngo_support_amount > 0;

-- ============================================
-- Rollback script (if needed)
-- ============================================
-- ALTER TABLE support_intents DROP COLUMN IF EXISTS bird_amount;
-- ALTER TABLE support_intents DROP COLUMN IF EXISTS wihngo_support_amount;
-- ALTER TABLE support_intents DROP COLUMN IF EXISTS wihngo_wallet_pubkey;
-- ALTER TABLE support_intents DROP COLUMN IF EXISTS wihngo_solana_signature;
