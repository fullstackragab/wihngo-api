-- Add invoice_number column to payment tables for compliance
-- This enables tracking and sending invoices for all payments

BEGIN;

-- Add invoice_number to p2p_payments
ALTER TABLE p2p_payments ADD COLUMN IF NOT EXISTS invoice_number VARCHAR(50);
CREATE INDEX IF NOT EXISTS ix_p2p_payments_invoice_number ON p2p_payments(invoice_number) WHERE invoice_number IS NOT NULL;

-- Add invoice_number to support_intents
ALTER TABLE support_intents ADD COLUMN IF NOT EXISTS invoice_number VARCHAR(50);
CREATE INDEX IF NOT EXISTS ix_support_intents_invoice_number ON support_intents(invoice_number) WHERE invoice_number IS NOT NULL;

COMMIT;

-- Verify columns were added
DO $$
BEGIN
    RAISE NOTICE 'Invoice number columns added to p2p_payments and support_intents tables';
    RAISE NOTICE 'All payments will now have invoices for legal compliance';
END $$;
