-- ============================================
-- FIX: Add all missing database columns
-- ============================================
-- This script adds columns that exist in C# models but are missing from the database
-- Error: column c.confirmations does not exist
-- Error: column t.token_address does not exist  
-- Error: column o.metadata does not exist

BEGIN;

-- ============================================
-- 1. Add confirmations column to crypto_payment_requests
-- ============================================
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

-- ============================================
-- 2. Add token_address column to token_configurations
-- ============================================
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'token_configurations' 
        AND column_name = 'token_address'
    ) THEN
        ALTER TABLE token_configurations 
        ADD COLUMN token_address character varying(255) NOT NULL DEFAULT '';
        
        RAISE NOTICE 'Added token_address column to token_configurations';
    ELSE
        RAISE NOTICE 'Column token_address already exists in token_configurations';
    END IF;
END $$;

-- ============================================
-- 3. Add metadata column to onchain_deposits
-- ============================================
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'onchain_deposits' 
        AND column_name = 'metadata'
    ) THEN
        ALTER TABLE onchain_deposits 
        ADD COLUMN metadata jsonb;
        
        RAISE NOTICE 'Added metadata column to onchain_deposits';
    ELSE
        RAISE NOTICE 'Column metadata already exists in onchain_deposits';
    END IF;
END $$;

-- ============================================
-- 4. Verification - Check all columns exist
-- ============================================
DO $$
DECLARE
    missing_count integer := 0;
BEGIN
    -- Check crypto_payment_requests.confirmations
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'crypto_payment_requests' AND column_name = 'confirmations'
    ) THEN
        RAISE WARNING '? MISSING: crypto_payment_requests.confirmations';
        missing_count := missing_count + 1;
    ELSE
        RAISE NOTICE '? VERIFIED: crypto_payment_requests.confirmations';
    END IF;

    -- Check token_configurations.token_address
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'token_configurations' AND column_name = 'token_address'
    ) THEN
        RAISE WARNING '? MISSING: token_configurations.token_address';
        missing_count := missing_count + 1;
    ELSE
        RAISE NOTICE '? VERIFIED: token_configurations.token_address';
    END IF;

    -- Check onchain_deposits.metadata
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'onchain_deposits' AND column_name = 'metadata'
    ) THEN
        RAISE WARNING '? MISSING: onchain_deposits.metadata';
        missing_count := missing_count + 1;
    ELSE
        RAISE NOTICE '? VERIFIED: onchain_deposits.metadata';
    END IF;

    -- Final status
    IF missing_count = 0 THEN
        RAISE NOTICE '';
        RAISE NOTICE '========================================';
        RAISE NOTICE '? ALL COLUMNS VERIFIED SUCCESSFULLY';
        RAISE NOTICE '========================================';
    ELSE
        RAISE WARNING '';
        RAISE WARNING '========================================';
        RAISE WARNING '? %  COLUMNS STILL MISSING', missing_count;
        RAISE WARNING '========================================';
    END IF;
END $$;

COMMIT;

-- ============================================
-- SUCCESS MESSAGE
-- ============================================
SELECT 
    '? Migration completed successfully' as status,
    NOW() as executed_at;
