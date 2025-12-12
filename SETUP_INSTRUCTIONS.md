# ============================================
# Wihngo Database Setup - Fresh Installation
# ============================================

## Prerequisites
# 1. PostgreSQL 14+ installed and running
# 2. Database credentials ready
# 3. .NET 10 SDK installed

## Step 1: Create the Database
# Connect to PostgreSQL and create the database
psql -U postgres -c "DROP DATABASE IF EXISTS wihngo;"
psql -U postgres -c "CREATE DATABASE wihngo;"

## Step 2: Update Connection String
# Edit appsettings.Development.json with your credentials:
# "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=YOUR_PASSWORD"

## Step 3: Apply All Migrations
# This will create all tables including the invoice payment system
dotnet ef database update

## Step 4: Verify Schema
# Run verification queries
psql -U postgres -d wihngo -f database-setup.sql

## Step 5: Update Merchant Addresses
# After migrations complete, update with your actual wallet addresses
psql -U postgres -d wihngo -c "
UPDATE supported_tokens 
SET merchant_receiving_address = 'YOUR_SOLANA_ADDRESS'
WHERE chain = 'solana';

UPDATE supported_tokens 
SET merchant_receiving_address = '0xYOUR_BASE_ADDRESS'
WHERE chain = 'base';
"

## Step 6: Verify Everything
psql -U postgres -d wihngo -c "
SELECT 'Migration History:' as info;
SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";

SELECT 'Invoice Payment Tables:' as info;
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('invoices', 'payments', 'supported_tokens', 'refund_requests', 'payment_events', 'audit_logs', 'webhooks_received', 'blockchain_cursors')
ORDER BY table_name;

SELECT 'Supported Tokens:' as info;
SELECT token_symbol, chain, mint_address, merchant_receiving_address, is_active FROM supported_tokens;

SELECT 'Invoice Sequence:' as info;
SELECT last_value FROM wihngo_invoice_seq;
"

## Step 7: Test the Application
dotnet run

# The application should start without errors
# Check the console output for:
# - Hangfire initialization success
# - Database connection success
# - All services registered

## Optional: Add Test Data
# If you want a test user account
psql -U postgres -d wihngo -c "
INSERT INTO users (user_id, name, email, password_hash, created_at)
VALUES (
    gen_random_uuid(),
    'Test User',
    'test@wihngo.com',
    '\$2a\$11\$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou',
    NOW()
)
ON CONFLICT (email) DO NOTHING;
"

# Login credentials:
# Email: test@wihngo.com
# Password: Test123!

# ============================================
# Quick Test Endpoints
# ============================================

# Health check
curl http://localhost:5000/test

# Create invoice (requires authentication)
# First login to get JWT token, then:
curl -X POST http://localhost:5000/api/v1/invoices \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amountFiat": 10.00,
    "fiatCurrency": "USD",
    "preferredPaymentMethods": ["paypal", "solana", "base"],
    "metadata": { "purpose": "test" }
  }'
