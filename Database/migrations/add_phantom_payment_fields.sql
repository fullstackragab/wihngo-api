-- Migration: Add Phantom Payment Fields
-- Description: Adds payment_source and memo columns to payments table for Phantom wallet integration
-- Date: 2024-12-20

-- Add payment_source column to track how payment was initiated (manual or phantom)
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS payment_source VARCHAR(20) NULL;

-- Add memo column to store transaction memo for reconciliation
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS memo VARCHAR(255) NULL;

-- Add index on payment_source for querying by payment method
CREATE INDEX IF NOT EXISTS idx_payments_payment_source ON payments(payment_source);

-- Add comment for documentation
COMMENT ON COLUMN payments.payment_source IS 'How the payment was initiated: manual (user sent manually) or phantom (approved via Phantom wallet)';
COMMENT ON COLUMN payments.memo IS 'Transaction memo for backend reconciliation, format: WIHNGO:{invoice_id}';
