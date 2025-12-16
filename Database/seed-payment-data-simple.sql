-- =============================================
-- Wihngo Payment System Seed Data (Simplified)
-- =============================================
-- This script populates payment-related tables with test data
-- Prerequisites: Users and Birds must already exist from database-reset-seed.sql
-- =============================================

BEGIN;

-- =============================================
-- PLATFORM WALLETS (Crypto wallet addresses)
-- =============================================
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES
  -- Solana USDC
  (gen_random_uuid(), 'USDC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, NOW() - INTERVAL '180 days', NOW()),
  -- Solana EURC
  (gen_random_uuid(), 'EURC', 'solana', 'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn', TRUE, NOW() - INTERVAL '180 days', NOW())
ON CONFLICT (currency, network, address) DO UPDATE
  SET is_active = TRUE, updated_at = NOW();

-- =============================================
-- CRYPTO EXCHANGE RATES
-- =============================================
INSERT INTO crypto_exchange_rates (id, currency, usd_rate, source, last_updated)
VALUES
  (gen_random_uuid(), 'USDC', 1.00, 'coingecko', NOW()),
  (gen_random_uuid(), 'EURC', 1.09, 'coingecko', NOW())
ON CONFLICT (currency) DO UPDATE SET
  usd_rate = EXCLUDED.usd_rate,
  last_updated = EXCLUDED.last_updated;

-- =============================================
-- CRYPTO PAYMENT METHODS (User saved wallets)
-- =============================================
INSERT INTO crypto_payment_methods (
  id, user_id, wallet_address, currency, network, label, is_default, verified, created_at, updated_at
) VALUES
-- Alice's payment methods
(gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU', 'USDC', 'solana', 'My Main Wallet', TRUE, TRUE, NOW() - INTERVAL '80 days', NOW()),
(gen_random_uuid(), '11111111-1111-1111-1111-111111111111', '7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU', 'EURC', 'solana', 'My Main Wallet', FALSE, TRUE, NOW() - INTERVAL '75 days', NOW()),

-- Bob's payment methods
(gen_random_uuid(), '22222222-2222-2222-2222-222222222222', '8YHHBzKXt3JC9L4F2mUKvPcRdQE7jGxH9vQBzXhXC3nT', 'USDC', 'solana', 'Ledger Wallet', TRUE, TRUE, NOW() - INTERVAL '55 days', NOW()),

-- Carol's payment method (new user)
(gen_random_uuid(), '33333333-3333-3333-3333-333333333333', '9ZJKmN7vQ8TY5kP2cHxD3fL6wRgE8nVB4sYtXhZuK9pW', 'USDC', 'solana', 'Trust Wallet', TRUE, FALSE, NOW() - INTERVAL '12 days', NOW()),

-- David's payment methods
(gen_random_uuid(), '44444444-4444-4444-4444-444444444444', 'AzYBv8K2mP5tN9cR4xWdF7jLqE3hTuG6sVbX1CkZnH8Q', 'USDC', 'solana', 'Rescue Fund Wallet', TRUE, TRUE, NOW() - INTERVAL '110 days', NOW()),

-- Emma's payment methods
(gen_random_uuid(), '55555555-5555-5555-5555-555555555555', 'BcZCv9K3nQ6uO0dS5yXeG8kMrF4iTvH7tWcY2DlAoI9R', 'USDC', 'solana', 'School Program Wallet', TRUE, TRUE, NOW() - INTERVAL '40 days', NOW())
ON CONFLICT (user_id, wallet_address, currency, network) DO NOTHING;

-- =============================================
-- CRYPTO PAYMENT REQUESTS WITH TRANSACTIONS
-- =============================================
-- Using WITH clause to create requests and link transactions

-- Payment Request 1: Alice - Ruby Premium (Completed)
WITH new_payment AS (
  INSERT INTO crypto_payment_requests (
    id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
    wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
    confirmations, required_confirmations, status, purpose, plan, metadata,
    expires_at, confirmed_at, completed_at, created_at, updated_at
  ) VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111',
    'aaaaaaaa-0001-0001-0001-000000000001',
    3.00, 3.000000, 'USDC', 'solana', 1.00,
    'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
    '7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=3.0&label=Ruby%20Premium',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=3.0',
    '4KVMjKXPJ8GzBLH9vQxYb2WfKqR7NcE5mTsU3DpZhA1L',
    32, 1, 'completed', 'premium_bird', 'monthly',
    '{"bird_name": "Ruby", "subscription_months": 1}'::jsonb,
    NOW() - INTERVAL '14 days 23 hours',
    NOW() - INTERVAL '15 days 22 minutes',
    NOW() - INTERVAL '15 days 20 minutes',
    NOW() - INTERVAL '15 days 1 hour',
    NOW() - INTERVAL '15 days 20 minutes'
  ) RETURNING id
)
INSERT INTO crypto_transactions (
  id, payment_request_id, transaction_hash, from_address, to_address,
  amount, currency, network, confirmations, block_number, block_hash,
  fee, gas_used, status, raw_transaction, detected_at, confirmed_at
) SELECT
  gen_random_uuid(), id, '4KVMjKXPJ8GzBLH9vQxYb2WfKqR7NcE5mTsU3DpZhA1L',
  '7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU',
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  3.000000, 'USDC', 'solana', 32, 245678900,
  '7mVn8YPqR3sWdF2kL9xGhTbN1cJ5eUoM6zKaQ4rEtH3B',
  0.000005, 5000, 'confirmed',
  '{"version": "legacy", "recent_blockhash": "7mVn8YPqR3sWdF2kL9xGhTbN1cJ5eUoM6zKaQ4rEtH3B"}'::jsonb,
  NOW() - INTERVAL '15 days 25 minutes',
  NOW() - INTERVAL '15 days 22 minutes'
FROM new_payment;

-- Payment Request 2: Bob - Sunshine Lifetime Premium (Completed)
WITH new_payment AS (
  INSERT INTO crypto_payment_requests (
    id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
    wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
    confirmations, required_confirmations, status, purpose, plan, metadata,
    expires_at, confirmed_at, completed_at, created_at, updated_at
  ) VALUES (
    gen_random_uuid(),
    '22222222-2222-2222-2222-222222222222',
    'bbbbbbbb-0001-0001-0001-000000000001',
    70.00, 70.000000, 'USDC', 'solana', 1.00,
    'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
    '8YHHBzKXt3JC9L4F2mUKvPcRdQE7jGxH9vQBzXhXC3nT',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=70.0&label=Sunshine%20Premium',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=70.0',
    '5LWNkLYQK9HaCMI0wRyZc3XgKsN8dF6nUtV4EqAbB2M',
    45, 1, 'completed', 'premium_bird', 'lifetime',
    '{"bird_name": "Sunshine", "subscription_type": "lifetime"}'::jsonb,
    NOW() - INTERVAL '44 days 23 hours',
    NOW() - INTERVAL '45 days 18 minutes',
    NOW() - INTERVAL '45 days 15 minutes',
    NOW() - INTERVAL '45 days 1 hour',
    NOW() - INTERVAL '45 days 15 minutes'
  ) RETURNING id
)
INSERT INTO crypto_transactions (
  id, payment_request_id, transaction_hash, from_address, to_address,
  amount, currency, network, confirmations, block_number, block_hash,
  fee, gas_used, status, raw_transaction, detected_at, confirmed_at
) SELECT
  gen_random_uuid(), id, '5LWNkLYQK9HaCMI0wRyZc3XgKsN8dF6nUtV4EqAbB2M',
  '8YHHBzKXt3JC9L4F2mUKvPcRdQE7jGxH9vQBzXhXC3nT',
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  70.000000, 'USDC', 'solana', 45, 245234500,
  '8nWo9ZQsS4tXeG3lM0yHiUcO2dK6fVpN7AkbR5sFuI4C',
  0.000005, 5000, 'confirmed',
  '{"version": "legacy", "recent_blockhash": "8nWo9ZQsS4tXeG3lM0yHiUcO2dK6fVpN7AkbR5sFuI4C"}'::jsonb,
  NOW() - INTERVAL '45 days 22 minutes',
  NOW() - INTERVAL '45 days 18 minutes'
FROM new_payment;

-- Payment Request 3: Alice - Phoenix Donation (Completed)
WITH new_payment AS (
  INSERT INTO crypto_payment_requests (
    id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
    wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
    confirmations, required_confirmations, status, purpose, plan, metadata,
    expires_at, confirmed_at, completed_at, created_at, updated_at
  ) VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111',
    'dddddddd-0001-0001-0001-000000000001',
    25.00, 25.000000, 'USDC', 'solana', 1.00,
    'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
    '7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=25.0&label=Phoenix%20Support',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=25.0',
    '6MXOlMZRLA0IbDJ2xSzad4YhLtO9eG7oVuW5FrBcC3N',
    28, 1, 'completed', 'donation', NULL,
    '{"bird_name": "Phoenix", "message": "For Phoenix medical care!"}'::jsonb,
    NOW() - INTERVAL '49 days 23 hours',
    NOW() - INTERVAL '50 days 12 minutes',
    NOW() - INTERVAL '50 days 10 minutes',
    NOW() - INTERVAL '50 days 1 hour',
    NOW() - INTERVAL '50 days 10 minutes'
  ) RETURNING id
)
INSERT INTO crypto_transactions (
  id, payment_request_id, transaction_hash, from_address, to_address,
  amount, currency, network, confirmations, block_number, block_hash,
  fee, gas_used, status, raw_transaction, detected_at, confirmed_at
) SELECT
  gen_random_uuid(), id, '6MXOlMZRLA0IbDJ2xSzad4YhLtO9eG7oVuW5FrBcC3N',
  '7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU',
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  25.000000, 'USDC', 'solana', 28, 245123400,
  '9oXp0ARtT5uYfH4mN1zIjVdP3eL7gWqO8BlcS6tGvJ5D',
  0.000005, 5000, 'confirmed',
  '{"version": "legacy", "recent_blockhash": "9oXp0ARtT5uYfH4mN1zIjVdP3eL7gWqO8BlcS6tGvJ5D"}'::jsonb,
  NOW() - INTERVAL '50 days 15 minutes',
  NOW() - INTERVAL '50 days 12 minutes'
FROM new_payment;

-- Payment Request 4: Emma - Professor Hoot Premium with EURC (Completed)
WITH new_payment AS (
  INSERT INTO crypto_payment_requests (
    id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
    wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
    confirmations, required_confirmations, status, purpose, plan, metadata,
    expires_at, confirmed_at, completed_at, created_at, updated_at
  ) VALUES (
    gen_random_uuid(),
    '55555555-5555-5555-5555-555555555555',
    'eeeeeeee-0001-0001-0001-000000000001',
    64.22, 58.900000, 'EURC', 'solana', 1.09,
    'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
    'BcZCv9K3nQ6uO0dS5yXeG8kMrF4iTvH7tWcY2DlAoI9R',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=58.9&label=Professor%20Hoot%20Premium',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=58.9',
    '8OZQnOcTNC2KdFL4zUcbe6ajNvQ1gI9qXwY7HtDeE5P',
    22, 1, 'completed', 'premium_bird', 'lifetime',
    '{"bird_name": "Professor Hoot", "subscription_type": "lifetime"}'::jsonb,
    NOW() - INTERVAL '34 days 23 hours',
    NOW() - INTERVAL '35 days 25 minutes',
    NOW() - INTERVAL '35 days 22 minutes',
    NOW() - INTERVAL '35 days 1 hour',
    NOW() - INTERVAL '35 days 22 minutes'
  ) RETURNING id
)
INSERT INTO crypto_transactions (
  id, payment_request_id, transaction_hash, from_address, to_address,
  amount, currency, network, confirmations, block_number, block_hash,
  fee, gas_used, status, raw_transaction, detected_at, confirmed_at
) SELECT
  gen_random_uuid(), id, '8OZQnOcTNC2KdFL4zUcbe6ajNvQ1gI9qXwY7HtDeE5P',
  'BcZCv9K3nQ6uO0dS5yXeG8kMrF4iTvH7tWcY2DlAoI9R',
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  58.900000, 'EURC', 'solana', 22, 245456700,
  'BqZr2CTvV7wAhJ6oP3BKlXfR5gN9iYsQ0EndU8vIxL7F',
  0.000005, 5000, 'confirmed',
  '{"version": "legacy", "recent_blockhash": "BqZr2CTvV7wAhJ6oP3BKlXfR5gN9iYsQ0EndU8vIxL7F"}'::jsonb,
  NOW() - INTERVAL '35 days 28 minutes',
  NOW() - INTERVAL '35 days 25 minutes'
FROM new_payment;

-- Payment Request 5: Carol - Ruby Donation (Pending - waiting for payment)
INSERT INTO crypto_payment_requests (
  id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
  wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
  confirmations, required_confirmations, status, purpose, plan, metadata,
  expires_at, confirmed_at, completed_at, created_at, updated_at
) VALUES (
  gen_random_uuid(),
  '33333333-3333-3333-3333-333333333333',
  'aaaaaaaa-0001-0001-0001-000000000001',
  10.00, 10.000000, 'USDC', 'solana', 1.00,
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  NULL,
  'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=10.0&label=Ruby%20Support',
  'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=10.0',
  NULL, 0, 1, 'pending', 'donation', NULL,
  '{"bird_name": "Ruby", "message": "Ruby is adorable! My first donation!"}'::jsonb,
  NOW() + INTERVAL '55 minutes', NULL, NULL,
  NOW() - INTERVAL '5 minutes',
  NOW() - INTERVAL '5 minutes'
);

-- Payment Request 6: Bob - Bella Premium (Pending)
INSERT INTO crypto_payment_requests (
  id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
  wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
  confirmations, required_confirmations, status, purpose, plan, metadata,
  expires_at, confirmed_at, completed_at, created_at, updated_at
) VALUES (
  gen_random_uuid(),
  '22222222-2222-2222-2222-222222222222',
  'bbbbbbbb-0002-0002-0002-000000000002',
  3.00, 3.000000, 'USDC', 'solana', 1.00,
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  NULL,
  'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=3.0&label=Bella%20Premium',
  'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=3.0',
  NULL, 0, 1, 'pending', 'premium_bird', 'monthly',
  '{"bird_name": "Bella", "subscription_months": 1}'::jsonb,
  NOW() + INTERVAL '45 minutes', NULL, NULL,
  NOW() - INTERVAL '15 minutes',
  NOW() - INTERVAL '15 minutes'
);

-- Payment Request 7: Alice - Sunshine Donation (Expired)
INSERT INTO crypto_payment_requests (
  id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
  wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
  confirmations, required_confirmations, status, purpose, plan, metadata,
  expires_at, confirmed_at, completed_at, created_at, updated_at
) VALUES (
  gen_random_uuid(),
  '11111111-1111-1111-1111-111111111111',
  'bbbbbbbb-0001-0001-0001-000000000001',
  20.00, 20.000000, 'USDC', 'solana', 1.00,
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  NULL,
  'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=20.0&label=Sunshine%20Support',
  'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=20.0',
  NULL, 0, 1, 'expired', 'donation', NULL,
  '{"bird_name": "Sunshine"}'::jsonb,
  NOW() - INTERVAL '2 hours', NULL, NULL,
  NOW() - INTERVAL '3 hours',
  NOW() - INTERVAL '2 hours'
);

-- Payment Request 8: Emma - Phoenix Donation (Confirming - just detected)
WITH new_payment AS (
  INSERT INTO crypto_payment_requests (
    id, user_id, bird_id, amount_usd, amount_crypto, currency, network, exchange_rate,
    wallet_address, user_wallet_address, qr_code_data, payment_uri, transaction_hash,
    confirmations, required_confirmations, status, purpose, plan, metadata,
    expires_at, confirmed_at, completed_at, created_at, updated_at
  ) VALUES (
    gen_random_uuid(),
    '55555555-5555-5555-5555-555555555555',
    'dddddddd-0001-0001-0001-000000000001',
    35.00, 35.000000, 'USDC', 'solana', 1.00,
    'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
    'BcZCv9K3nQ6uO0dS5yXeG8kMrF4iTvH7tWcY2DlAoI9R',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=35.0&label=Phoenix%20Support',
    'solana:AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn?amount=35.0',
    'CRDTtUhXRG6OhJP8DVgdh9ekRzU5kM3uBabC2MxIiI9T',
    1, 1, 'confirming', 'donation', NULL,
    '{"bird_name": "Phoenix", "message": "For Phoenix recovery fund!"}'::jsonb,
    NOW() + INTERVAL '50 minutes',
    NOW() - INTERVAL '2 minutes', NULL,
    NOW() - INTERVAL '8 minutes',
    NOW() - INTERVAL '2 minutes'
  ) RETURNING id
)
INSERT INTO crypto_transactions (
  id, payment_request_id, transaction_hash, from_address, to_address,
  amount, currency, network, confirmations, block_number, block_hash,
  fee, gas_used, status, raw_transaction, detected_at, confirmed_at
) SELECT
  gen_random_uuid(), id, 'CRDTtUhXRG6OhJP8DVgdh9ekRzU5kM3uBabC2MxIiI9T',
  'BcZCv9K3nQ6uO0dS5yXeG8kMrF4iTvH7tWcY2DlAoI9R',
  'AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn',
  35.000000, 'USDC', 'solana', 1, 245889900,
  'FuDv6GXzZ1AEkN0sT7FOpaJV9iR3mCvU4IrhY2xMzP1J',
  0.000005, 5000, 'pending',
  '{"version": "legacy", "recent_blockhash": "FuDv6GXzZ1AEkN0sT7FOpaJV9iR3mCvU4IrhY2xMzP1J"}'::jsonb,
  NOW() - INTERVAL '3 minutes', NULL
FROM new_payment;

COMMIT;

-- =============================================
-- VERIFICATION QUERIES
-- =============================================

SELECT '======================================' as separator;
SELECT 'Payment Data Seeding Completed!' as status;
SELECT '======================================' as separator;
SELECT '';

-- Summary counts
SELECT 'Platform Wallets: ' || COUNT(*) || ' active' as info FROM platform_wallets WHERE is_active = TRUE;
SELECT 'Exchange Rates: ' || COUNT(*) as info FROM crypto_exchange_rates;
SELECT 'Payment Methods: ' || COUNT(*) as info FROM crypto_payment_methods;
SELECT 'Payment Requests: ' || COUNT(*) as info FROM crypto_payment_requests;
SELECT 'Crypto Transactions: ' || COUNT(*) as info FROM crypto_transactions;

SELECT '';
SELECT '--- Payment Request Status Breakdown ---' as info;
SELECT
  status,
  COUNT(*) as count,
  SUM(amount_usd)::numeric(10,2) as total_usd
FROM crypto_payment_requests
GROUP BY status
ORDER BY status;

SELECT '';
SELECT '--- Payment Request Purpose Breakdown ---' as info;
SELECT
  purpose,
  COUNT(*) as count,
  SUM(amount_usd)::numeric(10,2) as total_usd
FROM crypto_payment_requests
GROUP BY purpose
ORDER BY purpose;

SELECT '';
SELECT '--- Recent Payment Requests ---' as info;
SELECT
  cpr.status,
  cpr.currency,
  cpr.amount_usd,
  cpr.purpose,
  u.name as user_name,
  b.name as bird_name,
  cpr.created_at
FROM crypto_payment_requests cpr
JOIN users u ON cpr.user_id = u.user_id
LEFT JOIN birds b ON cpr.bird_id = b.bird_id
ORDER BY cpr.created_at DESC
LIMIT 10;

SELECT '';
SELECT '--- User Payment Activity ---' as info;
SELECT
  u.name,
  COUNT(cpr.id) as payment_count,
  SUM(CASE WHEN cpr.status = 'completed' THEN cpr.amount_usd ELSE 0 END)::numeric(10,2) as total_paid,
  COUNT(CASE WHEN cpr.status = 'completed' THEN 1 END) as completed_payments,
  COUNT(CASE WHEN cpr.status = 'pending' THEN 1 END) as pending_payments
FROM users u
LEFT JOIN crypto_payment_requests cpr ON u.user_id = cpr.user_id
GROUP BY u.user_id, u.name
HAVING COUNT(cpr.id) > 0
ORDER BY total_paid DESC;

SELECT '';
SELECT '======================================' as separator;
SELECT 'Payment data is ready for testing!' as status;
SELECT '======================================' as separator;
