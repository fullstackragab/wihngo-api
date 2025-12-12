-- ============================================
-- Wihngo Database Setup Script
-- Fresh installation with all tables and seed data
-- ============================================

-- Step 1: Create database (run this separately as postgres superuser if needed)
-- CREATE DATABASE wihngo;

-- Step 2: Connect to wihngo database
\c wihngo;

-- Step 3: Verify we're connected
SELECT current_database();

-- The migrations will create all tables, so we just need to prepare
-- This script is mainly for verification after migrations run

-- ============================================
-- VERIFICATION QUERIES (run after migrations)
-- ============================================

-- Check all tables created by migrations
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Check invoice payment system tables specifically
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN (
    'invoices',
    'payments', 
    'supported_tokens',
    'refund_requests',
    'payment_events',
    'audit_logs',
    'webhooks_received',
    'blockchain_cursors'
)
ORDER BY table_name;

-- Check invoice sequence
SELECT sequence_name, last_value 
FROM information_schema.sequences 
WHERE sequence_name = 'wihngo_invoice_seq';

-- Check supported tokens seed data
SELECT id, token_symbol, chain, mint_address, is_active 
FROM supported_tokens 
ORDER BY chain, token_symbol;

-- Check migration history
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId";

-- ============================================
-- ADDITIONAL SEED DATA (optional)
-- ============================================

-- Add test user (password: Test123!)
-- Password hash for "Test123!" using BCrypt
INSERT INTO users (user_id, name, email, password_hash, created_at)
VALUES (
    gen_random_uuid(),
    'Test User',
    'test@wihngo.com',
    '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', -- Test123!
    NOW()
)
ON CONFLICT (email) DO NOTHING;

-- Verify user created
SELECT user_id, name, email, created_at FROM users WHERE email = 'test@wihngo.com';

ECHO '? Database setup verification complete!';
