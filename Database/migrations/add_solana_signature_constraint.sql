-- Migration: Add unique constraint on solana_signature to prevent replay attacks
-- Date: 2026-01-01
-- Description: Ensures the same Solana transaction signature cannot be used for multiple payments

BEGIN;

-- Add unique constraint on solana_signature (only for non-null values)
-- This prevents replay attacks where an attacker tries to use the same transaction
-- signature for multiple payment records
ALTER TABLE p2p_payments
ADD CONSTRAINT uq_p2p_payments_solana_signature
UNIQUE (solana_signature);

-- Note: The index idx_p2p_payments_signature already exists from the original migration
-- but it's not a unique index. The constraint above will create a unique index automatically.

-- Add idempotency_key column for preventing double-submissions
ALTER TABLE p2p_payments
ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(64);

-- Add index for idempotency lookups
CREATE INDEX IF NOT EXISTS idx_p2p_payments_idempotency
ON p2p_payments (id, idempotency_key)
WHERE idempotency_key IS NOT NULL;

-- Add error_message column for storing failure reasons
ALTER TABLE p2p_payments
ADD COLUMN IF NOT EXISTS error_message TEXT;

-- Add timeout status support (for transactions that never confirm)
-- No schema change needed - status column already supports any string value

COMMIT;
