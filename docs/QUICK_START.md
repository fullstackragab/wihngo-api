# Quick Start Guide - Crypto Payment Backend

## ?? Get Started in 3 Steps

### Step 1: Execute Database Migration

```bash
# Connect to your PostgreSQL database
psql -U postgres -d wihngo

# Run the migration script
\i Database/migrations/crypto_payment_system.sql

# Verify (should show 5 tables)
SELECT tablename FROM pg_tables 
WHERE tablename LIKE 'crypto_%' OR tablename = 'platform_wallets'
ORDER BY tablename;

# Exit psql
\q
```

**Expected Output:**
```
 crypto_exchange_rates
 crypto_payment_methods
 crypto_payment_requests
 crypto_transactions
 platform_wallets
(5 rows)
```

---

### Step 2: Configure API Keys (Optional for Testing)

Edit `appsettings.json` or use `dotnet user-secrets`:

```bash
# Using User Secrets (recommended)
dotnet user-secrets set "BlockchainSettings:TronGrid:ApiKey" "your-key-here"
dotnet user-secrets set "ExchangeRateSettings:CoinGeckoApiKey" "your-key-here"
dotnet user-secrets set "BlockchainSettings:Infura:ProjectId" "your-project-id"
```

**Or edit appsettings.json:**
```json
{
  "BlockchainSettings": {
    "TronGrid": {
      "ApiUrl": "https://api.trongrid.io",
      "ApiKey": "your-trongrid-api-key"
    }
  },
  "ExchangeRateSettings": {
    "CoinGeckoApiKey": "your-coingecko-api-key"
  }
}
```

---

### Step 3: Run and Test

```bash
# Build and run
dotnet run

# Or use watch mode for development
dotnet watch run
```

**Access:**
- Swagger UI: https://localhost:5001/swagger
- Hangfire Dashboard: https://localhost:5001/hangfire

---

## ?? Quick API Tests

### 1. Get Exchange Rates (No Auth)
```bash
curl https://localhost:5001/api/payments/crypto/rates
```

### 2. Get TRON USDT Wallet (No Auth)
```bash
curl https://localhost:5001/api/payments/crypto/wallet/USDT/tron
```

**Response:**
```json
{
  "currency": "USDT",
  "network": "tron",
  "address": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
  "isActive": true
}
```

### 3. Create Payment (Requires JWT)

First, get a JWT token by logging in, then:

```bash
curl -X POST https://localhost:5001/api/payments/crypto/create \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amountUsd": 9.99,
    "currency": "USDT",
    "network": "tron",
    "purpose": "premium_subscription",
    "plan": "monthly"
  }'
```

---

## ?? Background Jobs Status

Visit **https://localhost:5001/hangfire** to see:

- **update-exchange-rates**: Runs every 5 minutes
- **monitor-payments**: Runs every 1 minute  
- **expire-payments**: Runs every 1 hour

---

## ?? Troubleshooting

### Database Connection Error
```
Check your connection string in appsettings.json:
"DefaultConnection": "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=yourpassword"
```

### Hangfire Error
```
Make sure PostgreSQL connection is correct. Hangfire uses the same connection string.
```

### Exchange Rates Not Updating
```
Check Hangfire dashboard for job errors. May need CoinGecko API key if hitting rate limits.
```

---

## ?? Full Documentation

- **Complete Guide**: `IMPLEMENTATION_COMPLETE.md`
- **Database Migration**: `Database/migrations/README.md`
- **Backend Architecture**: `docs/BACKEND_CRYPTO_DOTNET_GUIDE.md`

---

## ? You're All Set!

Your crypto payment backend is ready to accept TRON USDT payments!

**Platform Wallet**: `TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA`

**Next**: Test the payment flow with a small USDT transaction!
