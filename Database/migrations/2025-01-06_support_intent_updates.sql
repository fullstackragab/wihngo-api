-- Migration: Support Intent Updates for Wihngo-only support
-- Date: 2025-01-06
-- Changes:
--   1. Add idempotency_key column for duplicate transaction prevention
--   2. Add error_message column for tracking payment failures
--   3. Make bird_id nullable to support Wihngo-only donations (no specific bird)
--   4. Update FK constraint to allow NULL bird_id

-- 1. Add idempotency_key column
ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(255);

-- 2. Add error_message column for tracking failures
ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS error_message TEXT;

-- 3. Make bird_id nullable (required for Wihngo-only support)
ALTER TABLE support_intents
ALTER COLUMN bird_id DROP NOT NULL;

-- 4. Update foreign key constraint to allow NULL and cascade properly
-- First drop the existing constraint
ALTER TABLE support_intents
DROP CONSTRAINT IF EXISTS support_intents_bird_id_fkey;

-- Then recreate with ON DELETE SET NULL
ALTER TABLE support_intents
ADD CONSTRAINT support_intents_bird_id_fkey
FOREIGN KEY (bird_id) REFERENCES birds(bird_id) ON DELETE SET NULL;

-- Add index on idempotency_key for faster lookups
CREATE INDEX IF NOT EXISTS idx_support_intents_idempotency
ON support_intents(idempotency_key) WHERE idempotency_key IS NOT NULL;

-- Comments
COMMENT ON COLUMN support_intents.idempotency_key IS
'Unique key to prevent duplicate transactions. Format: supporter_id:bird_id:amount:timestamp';

COMMENT ON COLUMN support_intents.error_message IS
'Error message if the transaction failed or was rejected';

COMMENT ON COLUMN support_intents.bird_id IS
'The bird being supported. NULL when supporting Wihngo platform directly without a specific bird.';
