-- ============================================
-- HD WALLET MIGRATION - READY TO EXECUTE
-- ============================================
-- Database: wihngo (PostgreSQL)
-- Purpose: Add HD wallet support for unique payment addresses
-- Date: 2025-12-11
-- Safe to run: Uses IF NOT EXISTS (idempotent)
-- ============================================

-- Start transaction
BEGIN;

-- 1. Add address_index column
ALTER TABLE crypto_payment_requests 
ADD COLUMN IF NOT EXISTS address_index INTEGER NULL;

COMMENT ON COLUMN crypto_payment_requests.address_index IS 
'HD wallet derivation path index (BIP44). Null for non-HD addresses. Example: index 42 = m/44''/60''/0''/0/42';

-- 2. Create index on address_index
CREATE INDEX IF NOT EXISTS ix_crypto_payment_requests_address_index 
ON crypto_payment_requests(address_index);

-- 3. Create HD address sequences
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq IS 
'Global HD wallet address index sequence (fallback)';

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_ethereum
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_ethereum IS 
'HD wallet sequence for Ethereum mainnet';

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_sepolia
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_sepolia IS 
'HD wallet sequence for Sepolia testnet';

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_tron
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_tron IS 
'HD wallet sequence for Tron mainnet';

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_binance_smart_chain
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_binance_smart_chain IS 
'HD wallet sequence for Binance Smart Chain';

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_polygon
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_polygon IS 
'HD wallet sequence for Polygon';

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_bitcoin
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_bitcoin IS 
'HD wallet sequence for Bitcoin';

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_solana
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

COMMENT ON SEQUENCE hd_address_index_seq_solana IS 
'HD wallet sequence for Solana';

-- Commit transaction
COMMIT;

-- ============================================
-- VERIFICATION QUERIES
-- ============================================
-- Run these to verify the migration succeeded

-- Verify column exists
SELECT 
    column_name,
    data_type,
    is_nullable,
    'Column created successfully' as status
FROM information_schema.columns 
WHERE table_name = 'crypto_payment_requests' 
AND column_name = 'address_index';

-- Verify index exists
SELECT 
    indexname,
    indexdef,
    'Index created successfully' as status
FROM pg_indexes 
WHERE tablename = 'crypto_payment_requests' 
AND indexname = 'ix_crypto_payment_requests_address_index';

-- Verify sequences (should return 8 rows)
SELECT 
    sequence_name,
    start_value,
    increment,
    'Sequence created successfully' as status
FROM information_schema.sequences 
WHERE sequence_name LIKE 'hd_address_index_seq%'
ORDER BY sequence_name;

-- Count sequences
SELECT 
    COUNT(*) as total_sequences,
    CASE 
        WHEN COUNT(*) = 8 THEN 'SUCCESS: All 8 sequences created'
        ELSE 'ERROR: Expected 8 sequences, found ' || COUNT(*)::text
    END as verification_status
FROM information_schema.sequences 
WHERE sequence_name LIKE 'hd_address_index_seq%';
