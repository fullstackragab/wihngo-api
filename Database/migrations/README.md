# Crypto Payment System - PostgreSQL Migration Guide

## ?? Overview

This guide helps you apply the crypto payment system database schema to your PostgreSQL database.

## ??? Files Included

1. **crypto_payment_system.sql** - Main migration script (creates all tables and seed data)
2. **crypto_payment_system_rollback.sql** - Rollback script (drops all crypto payment tables)

## ?? Tables Created

### 1. `platform_wallets`
Stores cryptocurrency wallet addresses owned by the platform.

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| currency | VARCHAR(10) | Crypto currency (BTC, ETH, USDT, etc.) |
| network | VARCHAR(50) | Blockchain network (tron, ethereum, etc.) |
| address | VARCHAR(255) | Wallet address |
| private_key_encrypted | TEXT | Encrypted private key (optional) |
| derivation_path | VARCHAR(100) | HD wallet derivation path |
| is_active | BOOLEAN | Whether wallet is active |
| created_at | TIMESTAMP | Creation timestamp |
| updated_at | TIMESTAMP | Last update timestamp |

**Seeded Data**: TRON USDT wallet (`TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA`)

---

### 2. `crypto_exchange_rates`
Stores current exchange rates for cryptocurrencies.

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| currency | VARCHAR(10) | Crypto currency code |
| usd_rate | NUMERIC(20,2) | USD exchange rate |
| source | VARCHAR(50) | Rate source (e.g., 'coingecko') |
| last_updated | TIMESTAMP | Last update timestamp |

**Seeded Data**: Initial rates for BTC, ETH, USDT, USDC, BNB, SOL, DOGE

---

### 3. `crypto_payment_requests`
Main table for tracking crypto payment requests.

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| user_id | UUID | FK to users table |
| bird_id | UUID | FK to birds table (optional) |
| amount_usd | NUMERIC(10,2) | Payment amount in USD |
| amount_crypto | NUMERIC(20,10) | Payment amount in crypto |
| currency | VARCHAR(10) | Cryptocurrency used |
| network | VARCHAR(50) | Blockchain network |
| exchange_rate | NUMERIC(20,2) | Exchange rate at time of request |
| wallet_address | VARCHAR(255) | Platform wallet address |
| user_wallet_address | VARCHAR(255) | User's wallet address |
| qr_code_data | TEXT | QR code data for payment |
| payment_uri | TEXT | Payment URI |
| transaction_hash | VARCHAR(255) | Blockchain transaction hash |
| confirmations | INTEGER | Number of confirmations |
| required_confirmations | INTEGER | Required confirmations |
| status | VARCHAR(20) | Payment status |
| purpose | VARCHAR(50) | Payment purpose |
| plan | VARCHAR(20) | Subscription plan |
| metadata | JSONB | Additional metadata |
| expires_at | TIMESTAMP | Payment expiration |
| confirmed_at | TIMESTAMP | Confirmation timestamp |
| completed_at | TIMESTAMP | Completion timestamp |
| created_at | TIMESTAMP | Creation timestamp |
| updated_at | TIMESTAMP | Last update timestamp |

**Indexes**: status, transaction_hash, expires_at, user_id, bird_id

---

### 4. `crypto_transactions`
Stores blockchain transaction details.

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| payment_request_id | UUID | FK to crypto_payment_requests |
| transaction_hash | VARCHAR(255) | Blockchain transaction hash (unique) |
| from_address | VARCHAR(255) | Sender address |
| to_address | VARCHAR(255) | Recipient address |
| amount | NUMERIC(20,10) | Transaction amount |
| currency | VARCHAR(10) | Cryptocurrency |
| network | VARCHAR(50) | Blockchain network |
| confirmations | INTEGER | Number of confirmations |
| block_number | BIGINT | Block number |
| block_hash | VARCHAR(255) | Block hash |
| fee | NUMERIC(20,10) | Transaction fee |
| gas_used | BIGINT | Gas used (EVM networks) |
| status | VARCHAR(20) | Transaction status |
| raw_transaction | JSONB | Raw transaction data |
| detected_at | TIMESTAMP | Detection timestamp |
| confirmed_at | TIMESTAMP | Confirmation timestamp |

**Indexes**: payment_request_id, transaction_hash, status

---

### 5. `crypto_payment_methods`
Stores saved user wallet addresses.

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| user_id | UUID | FK to users table |
| wallet_address | VARCHAR(255) | User's wallet address |
| currency | VARCHAR(10) | Cryptocurrency |
| network | VARCHAR(50) | Blockchain network |
| label | VARCHAR(100) | User-defined label |
| is_default | BOOLEAN | Default payment method |
| verified | BOOLEAN | Whether address is verified |
| created_at | TIMESTAMP | Creation timestamp |
| updated_at | TIMESTAMP | Last update timestamp |

**Indexes**: user_id, is_default

---

## ?? Execution Steps

### Step 1: Connect to PostgreSQL

```bash
# Connect using psql
psql -U postgres -d wihngo

# Or using connection string
psql postgresql://postgres:password@localhost:5432/wihngo
```

### Step 2: Apply Migration

```bash
# Execute the migration script
psql -U postgres -d wihngo -f Database/migrations/crypto_payment_system.sql

# Or within psql session:
\i Database/migrations/crypto_payment_system.sql
```

### Step 3: Verify Tables Created

```sql
-- Check tables
SELECT tablename FROM pg_tables 
WHERE tablename LIKE 'crypto_%' OR tablename = 'platform_wallets'
ORDER BY tablename;

-- Check row counts
SELECT 
    'platform_wallets' as table_name, COUNT(*) as row_count FROM platform_wallets
UNION ALL
SELECT 'crypto_exchange_rates', COUNT(*) FROM crypto_exchange_rates
UNION ALL
SELECT 'crypto_payment_requests', COUNT(*) FROM crypto_payment_requests
UNION ALL
SELECT 'crypto_transactions', COUNT(*) FROM crypto_transactions
UNION ALL
SELECT 'crypto_payment_methods', COUNT(*) FROM crypto_payment_methods;
```

### Step 4: Verify Seed Data

```sql
-- Check platform wallet
SELECT * FROM platform_wallets;

-- Check exchange rates
SELECT currency, usd_rate, source, last_updated 
FROM crypto_exchange_rates 
ORDER BY currency;
```

---

## ?? Rollback (If Needed)

To remove all crypto payment tables:

```bash
# Execute rollback script
psql -U postgres -d wihngo -f Database/migrations/crypto_payment_system_rollback.sql

# Or within psql session:
\i Database/migrations/crypto_payment_system_rollback.sql
```

---

## ?? Post-Migration Configuration

### 1. Update appsettings.json

Ensure your connection string is correct:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=yourpassword"
  }
}
```

### 2. Configure Blockchain Settings

Add blockchain API keys to appsettings.json:

```json
{
  "BlockchainSettings": {
    "TronGrid": {
      "ApiUrl": "https://api.trongrid.io",
      "ApiKey": "your-trongrid-api-key"
    },
    "Infura": {
      "ProjectId": "your-infura-project-id",
      "ProjectSecret": "your-infura-secret"
    }
  },
  "ExchangeRateSettings": {
    "CoinGeckoApiKey": "your-coingecko-api-key",
    "UpdateIntervalMinutes": 5
  },
  "PaymentSettings": {
    "ExpirationMinutes": 30,
    "MinPaymentAmountUsd": 5.0
  }
}
```

---

## ?? Expected Results

After successful migration, you should see:

- ? 5 new tables created
- ? 1 platform wallet (TRON USDT)
- ? 7 exchange rates (BTC, ETH, USDT, USDC, BNB, SOL, DOGE)
- ? Proper indexes and foreign keys
- ? Automatic timestamp triggers

---

## ?? Test Queries

### Query 1: Check All Tables
```sql
\dt crypto_*
\dt platform_wallets
```

### Query 2: Check Constraints
```sql
SELECT 
    tc.table_name, 
    tc.constraint_name, 
    tc.constraint_type
FROM information_schema.table_constraints tc
WHERE tc.table_name LIKE 'crypto_%' OR tc.table_name = 'platform_wallets'
ORDER BY tc.table_name, tc.constraint_type;
```

### Query 3: Check Indexes
```sql
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename LIKE 'crypto_%' OR tablename = 'platform_wallets'
ORDER BY tablename, indexname;
```

---

## ?? Troubleshooting

### Error: relation "users" does not exist

The migration assumes `users` and `birds` tables already exist. If they don't, you have two options:

1. **Create those tables first**
2. **Modify the migration to remove foreign key constraints**:

```sql
-- Comment out these lines in the migration script:
-- CONSTRAINT fk_crypto_payment_requests_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
-- CONSTRAINT fk_crypto_payment_requests_bird FOREIGN KEY (bird_id) REFERENCES birds(id) ON DELETE SET NULL
```

### Error: Duplicate key value

If you run the migration twice, the seed data will fail because of unique constraints. This is safe to ignore, or use:

```sql
ON CONFLICT (currency, network, address) DO NOTHING;
ON CONFLICT (currency) DO NOTHING;
```

---

## ? Success Checklist

- [ ] PostgreSQL database connection verified
- [ ] Migration script executed successfully
- [ ] 5 tables created (platform_wallets, crypto_exchange_rates, crypto_payment_requests, crypto_transactions, crypto_payment_methods)
- [ ] 1 platform wallet seeded (TRON USDT)
- [ ] 7 exchange rates seeded
- [ ] Indexes and constraints created
- [ ] Triggers created for automatic timestamp updates
- [ ] Connection string updated in appsettings.json
- [ ] Blockchain API keys configured

---

## ?? Next Steps

1. **Build the .NET project**: `dotnet build`
2. **Run the application**: `dotnet run`
3. **Access Hangfire dashboard**: `https://localhost:5001/hangfire`
4. **Test API endpoints**: Use Swagger UI at `https://localhost:5001/swagger`
5. **Monitor background jobs**: Check Hangfire dashboard for exchange rate updates and payment monitoring

---

**?? Your crypto payment backend is now ready!**
