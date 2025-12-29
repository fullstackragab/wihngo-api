-- Migration: Drop invoice_number columns (compliance cleanup)
-- Date: 2025-12-29
-- Reason: invoice_number contradicts compliance strategy (support, not commerce)

-- Drop invoice_number from support_intents
ALTER TABLE support_intents DROP COLUMN IF EXISTS invoice_number;
DROP INDEX IF EXISTS ix_support_intents_invoice_number;

-- Drop invoice_number from p2p_payments
ALTER TABLE p2p_payments DROP COLUMN IF EXISTS invoice_number;
DROP INDEX IF EXISTS ix_p2p_payments_invoice_number;

-- Verify columns are removed
DO $$
BEGIN
    RAISE NOTICE '✓ Removed invoice_number columns from support_intents and p2p_payments';
END $$;
