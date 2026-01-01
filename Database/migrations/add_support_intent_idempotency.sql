-- Migration: Add idempotency support to support_intents table
-- Date: 2026-01-01
-- Purpose: Enable idempotent intent creation and transaction submission for Bird Support flow

-- Add idempotency_key column for preventing duplicate submissions
ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(64);

-- Add error_message column for storing failure details
ALTER TABLE support_intents
ADD COLUMN IF NOT EXISTS error_message TEXT;

-- Create index on idempotency_key for fast lookups
-- Only for active (non-expired, non-cancelled) intents within the idempotency window
CREATE INDEX IF NOT EXISTS idx_support_intents_idempotency_key
ON support_intents (idempotency_key)
WHERE idempotency_key IS NOT NULL
  AND status NOT IN ('expired', 'cancelled', 'failed');

-- Create unique constraint on solana_signature to prevent replay attacks
-- (Same as p2p_payments table)
ALTER TABLE support_intents
ADD CONSTRAINT uq_support_intents_solana_signature
UNIQUE (solana_signature);

-- NOTE: The unique constraint will fail if there are existing duplicate signatures.
-- If that happens, run this to find and clean duplicates first:
-- SELECT solana_signature, COUNT(*)
-- FROM support_intents
-- WHERE solana_signature IS NOT NULL
-- GROUP BY solana_signature
-- HAVING COUNT(*) > 1;
