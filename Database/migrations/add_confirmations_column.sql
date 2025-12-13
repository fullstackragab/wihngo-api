-- Add confirmations column to crypto_payment_requests table
-- This column tracks the number of blockchain confirmations for a payment

BEGIN;

-- Check if column exists, if not, add it
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'crypto_payment_requests' 
        AND column_name = 'confirmations'
    ) THEN
        ALTER TABLE crypto_payment_requests 
        ADD COLUMN confirmations integer NOT NULL DEFAULT 0;
        
        RAISE NOTICE 'Added confirmations column to crypto_payment_requests';
    ELSE
        RAISE NOTICE 'Column confirmations already exists in crypto_payment_requests';
    END IF;
END $$;

COMMIT;
