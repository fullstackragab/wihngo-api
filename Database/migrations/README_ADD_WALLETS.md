# ?? ADD ALL SUPPORTED WALLETS MIGRATION

## ?? IMPORTANT - RUN THIS MIGRATION NOW

The backend has been updated to support only **USDC and EURC** on **6 networks**.

### Previous Configuration (REMOVED):
- ? USDT (all networks)
- ? ETH
- ? BNB
- ? Tron network
- ? Binance Smart Chain (BSC)

### New Configuration (ACTIVE):
- ? **USDC** on: Ethereum, Solana, Polygon, Base, Avalanche, Stellar
- ? **EURC** on: Ethereum, Solana, Polygon, Base, Avalanche, Stellar

---

## ?? What This Migration Does

Adds wallet addresses for ALL supported currency/network combinations:

| Currency | Networks | Count |
|----------|----------|-------|
| USDC | ethereum, solana, polygon, base, avalanche, stellar | 6 |
| EURC | ethereum, solana, polygon, base, avalanche, stellar | 6 |
| **TOTAL** | | **12 wallets** |

**Also deactivates old wallets** for USDT, ETH, BNB, Tron, and BSC.

---

## ?? HOW TO RUN

### Option 1: Using psql (Recommended)

```bash
# Connect to production database
psql "postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require"

# Run the migration
\i Database/migrations/add_all_supported_wallets.sql

# Check results
SELECT currency, network, address, is_active FROM platform_wallets WHERE is_active = TRUE ORDER BY currency, network;
```

### Option 2: Copy and Paste in Database Tool

1. Open your database client (pgAdmin, DBeaver, etc.)
2. Connect to the database
3. Copy the contents of `add_all_supported_wallets.sql`
4. Paste and execute

---

## ? VERIFICATION

After running the migration, verify with:

```sql
-- Should return 12 rows (2 currencies × 6 networks)
SELECT 
    currency,
    network,
    address,
    is_active
FROM platform_wallets
WHERE is_active = TRUE
ORDER BY currency, network;
```

Expected output:
```
 currency |   network   |                    address                     | is_active 
----------+-------------+-----------------------------------------------+-----------
 EURC     | avalanche   | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 EURC     | base        | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 EURC     | ethereum    | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 EURC     | polygon     | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 EURC     | solana      | EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v  | t
 EURC     | stellar     | GCKFBEIYV2U22IO2BJ4KVJOIP7XPWQGQFKKWXR6DOSJBV7STMAQSMTGG | t
 USDC     | avalanche   | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 USDC     | base        | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 USDC     | ethereum    | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 USDC     | polygon     | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4  | t
 USDC     | solana      | EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v  | t
 USDC     | stellar     | GCKFBEIYV2U22IO2BJ4KVJOIP7XPWQGQFKKWXR6DOSJBV7STMAQSMTGG | t
(12 rows)
```

### Count by Currency:
```sql
SELECT currency, COUNT(*) as wallet_count
FROM platform_wallets
WHERE is_active = TRUE
GROUP BY currency
ORDER BY currency;
```

Expected:
```
 currency | wallet_count 
----------+--------------
 EURC     |            6
 USDC     |            6
```

---

## ?? Troubleshooting

### Error: "duplicate key value violates unique constraint"
This is SAFE to ignore. It means the wallet already exists. The migration uses `ON CONFLICT` to handle this.

### Old wallets still showing as active
The migration sets old currencies (USDT, ETH, BNB) to `is_active = FALSE`. Check with:
```sql
SELECT currency, network, is_active FROM platform_wallets WHERE currency IN ('USDT', 'ETH', 'BNB');
```

---

## ?? Mobile App Impact

Once this migration runs, the mobile app will support:

### ? SUPPORTED Combinations (12 total):

| Currency | Ethereum | Solana | Polygon | Base | Avalanche | Stellar |
|----------|:--------:|:------:|:-------:|:----:|:---------:|:-------:|
| USDC     | ?       | ?     | ?      | ?   | ?        | ?      |
| EURC     | ?       | ?     | ?      | ?   | ?        | ?      |

### ? NO LONGER SUPPORTED:
- USDT (all networks) ?
- ETH ?
- BNB ?
- Tron network ?
- Binance Smart Chain (BSC) ?

---

## ?? Network Specifications

| Network   | Chain ID | Confirmations | Address Format | Notes |
|-----------|----------|---------------|----------------|-------|
| Ethereum  | 1        | 12            | 0x...          | Mainnet |
| Solana    | -        | 32            | Base58         | Mainnet-beta |
| Polygon   | 137      | 128           | 0x...          | PoS Chain |
| Base      | 8453     | 12            | 0x...          | L2 on Ethereum |
| Avalanche | 43114    | 10            | 0x...          | C-Chain |
| Stellar   | -        | 1             | G...           | Mainnet |

---

## ?? Security Note

These are BASE wallet addresses. The HD wallet system will derive unique addresses for each payment using the mnemonic configured in `appsettings.json`.

If HD mnemonic is not configured, these base addresses will be used directly.

---

## ? WHEN TO RUN

**RUN THIS NOW** - The mobile app needs these wallets to function with the new currency/network combinations!

---

## ?? Related Files

- `appsettings.json` - Updated with new wallet configurations
- `Services/CryptoPaymentService.cs` - Updated with new network support
- `MOBILE_APP_REQUIRED_CHANGES.md` - Mobile app integration guide (needs update)

---

## ?? Need Help?

1. Check if migration already ran:
   ```sql
   SELECT COUNT(*) FROM platform_wallets WHERE is_active = TRUE;
   ```
   If result is 12, migration already completed! ?

2. Check what's configured:
   ```sql
   SELECT currency, network FROM platform_wallets WHERE is_active = TRUE ORDER BY currency, network;
   ```

3. Check old wallets status:
   ```sql
   SELECT currency, network, is_active FROM platform_wallets ORDER BY is_active DESC, currency, network;
   ```

