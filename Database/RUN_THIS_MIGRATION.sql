-- ============================================
-- HD WALLET SUPPORT MIGRATION
-- ============================================
-- This script adds support for HD wallet address generation
-- Run this on your PostgreSQL database
-- Date: 2025-12-11
-- ============================================

BEGIN;

-- 1. Add address_index column to track HD derivation path
ALTER TABLE crypto_payment_requests 
ADD COLUMN IF NOT EXISTS address_index INTEGER NULL;

COMMENT ON COLUMN crypto_payment_requests.address_index IS 
'HD wallet derivation path index (BIP44). Null for non-HD addresses. Example: index 42 = m/44''/60''/0''/0/42';

-- 2. Create index for efficient querying
CREATE INDEX IF NOT EXISTS ix_crypto_payment_requests_address_index 
ON crypto_payment_requests(address_index);

-- 3. Create sequences for atomic HD address index allocation
-- These ensure thread-safe, collision-free address generation

-- Global fallback sequence
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq IS 
'Global HD wallet address index sequence (fallback for unrecognized networks)';

-- Ethereum mainnet
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_ethereum
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_ethereum IS 
'HD wallet address index sequence for Ethereum mainnet (m/44''/60''/0''/0/X)';

-- Sepolia testnet
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_sepolia
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_sepolia IS 
'HD wallet address index sequence for Sepolia testnet (m/44''/60''/0''/0/X)';

-- Tron mainnet
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_tron
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_tron IS 
'HD wallet address index sequence for Tron mainnet (m/44''/60''/0''/0/X, then convert to Tron)';

-- Binance Smart Chain
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_binance_smart_chain
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_binance_smart_chain IS 
'HD wallet address index sequence for Binance Smart Chain (m/44''/60''/0''/0/X)';

-- Polygon
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_polygon
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_polygon IS 
'HD wallet address index sequence for Polygon (m/44''/60''/0''/0/X)';

-- Bitcoin
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_bitcoin
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_bitcoin IS 
'HD wallet address index sequence for Bitcoin (m/44''/0''/0''/0/X)';

-- Solana
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_solana
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_solana IS 
'HD wallet address index sequence for Solana (m/44''/501''/0''/0/X)';

COMMIT;

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

-- Check column was added
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'crypto_payment_requests' 
AND column_name = 'address_index';

-- Check index was created
SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'crypto_payment_requests'
AND indexname = 'ix_crypto_payment_requests_address_index';

-- Check sequences were created (should return 8 rows)
SELECT 
    sequence_name,
    start_value,
    increment_by,
    last_value
FROM information_schema.sequences 
WHERE sequence_name LIKE 'hd_address_index_seq%'
ORDER BY sequence_name;

-- ============================================
-- ROLLBACK SCRIPT (if needed)
-- ============================================
-- Uncomment and run if you need to undo the migration

-- BEGIN;
-- 
-- DROP SEQUENCE IF EXISTS hd_address_index_seq CASCADE;
-- DROP SEQUENCE IF EXISTS hd_address_index_seq_ethereum CASCADE;
-- DROP SEQUENCE IF EXISTS hd_address_index_seq_sepolia CASCADE;
-- DROP SEQUENCE IF EXISTS hd_address_index_seq_tron CASCADE;
-- DROP SEQUENCE IF EXISTS hd_address_index_seq_binance_smart_chain CASCADE;
-- DROP SEQUENCE IF EXISTS hd_address_index_seq_polygon CASCADE;
-- DROP SEQUENCE IF EXISTS hd_address_index_seq_bitcoin CASCADE;
-- DROP SEQUENCE IF EXISTS hd_address_index_seq_solana CASCADE;
-- 
-- DROP INDEX IF EXISTS ix_crypto_payment_requests_address_index;
-- 
-- ALTER TABLE crypto_payment_requests 
-- DROP COLUMN IF EXISTS address_index;
-- 
-- COMMIT;
