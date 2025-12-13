-- ============================================
-- SIMPLE FIX - Just 3 Statements
-- ============================================
-- Copy and paste these 3 lines into your database

-- 1. Add confirmations column to crypto_payment_requests
ALTER TABLE crypto_payment_requests 
ADD COLUMN IF NOT EXISTS confirmations integer NOT NULL DEFAULT 0;

-- 2. Add token_address column to token_configurations
ALTER TABLE token_configurations 
ADD COLUMN IF NOT EXISTS token_address character varying(255) NOT NULL DEFAULT '';

-- 3. Add metadata column to onchain_deposits
ALTER TABLE onchain_deposits 
ADD COLUMN IF NOT EXISTS metadata jsonb;

-- Done! That's all you need.
