# Quick Fix: Add Sepolia Wallet to Database

## Problem
Backend returns: **"No wallet configured for ETH on sepolia"**

## Solution
Run this SQL in your PostgreSQL database to add the Sepolia wallet.

### Option 1: Using pgAdmin or Database Client

1. Open your PostgreSQL client (pgAdmin, DBeaver, etc.)
2. Connect to your `wihngo` database
3. Run this SQL:

```sql
-- Insert Sepolia Testnet Wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'ETH',
    'sepolia',
    '0x4cc28f4cea7b440858b903b5c46685cb1478cdc4',
    true,
    NOW(),
    NOW()
)
ON CONFLICT (currency, network, address) DO NOTHING;

-- Verify
SELECT * FROM platform_wallets WHERE network = 'sepolia';
```

### Option 2: Using PowerShell/Command Line

```sh
# Connect to PostgreSQL (adjust connection string as needed)
psql -h localhost -p 5432 -U postgres -d wihngo

# Then run the INSERT command above
```

### Option 3: Using the SQL Script File

The complete script is saved in `scripts/add_sepolia_wallet.sql`. Run it with:

```sh
psql -h localhost -p 5432 -U postgres -d wihngo -f scripts/add_sepolia_wallet.sql
```

---

## After Running SQL

1. ? **No need to restart backend** (already running with updated code)
2. ? **Try your payment again** from the app
3. ? **Should work immediately**

---

## Expected Result

After adding the wallet, your payment creation should succeed with:

```json
{
  "paymentRequest": {
    "currency": "ETH",
    "network": "sepolia",
    "walletAddress": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
    "amountCrypto": 0.00366667,
    "status": "pending"
  }
}
```

---

## Verification Queries

Check if wallet exists:
```sql
SELECT * FROM platform_wallets WHERE currency = 'ETH' AND network = 'sepolia';
```

Check if exchange rate exists:
```sql
SELECT * FROM crypto_exchange_rates WHERE currency = 'ETH';
```

If ETH rate doesn't exist, add it:
```sql
INSERT INTO crypto_exchange_rates (id, currency, usd_rate, source, last_updated)
VALUES (gen_random_uuid(), 'ETH', 3000.00, 'coingecko', NOW())
ON CONFLICT (currency) DO UPDATE SET usd_rate = 3000.00, last_updated = NOW();
```

---

**This will fix the "No wallet configured" error!** ??
