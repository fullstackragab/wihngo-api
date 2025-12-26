# ?? CRYPTO SYSTEM QUICK START

## ? TL;DR

**Old System (REMOVED):**
- USDT, ETH, BNB
- Tron, BSC networks

**New System (ACTIVE):**
- **USDC & EURC** only
- **6 networks:** Ethereum, Solana, Polygon, Base, Avalanche, Stellar
- **12 total combinations** (all valid)

---

## ?? Run This NOW

```bash
# Connect to database
psql "postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require"

# Execute migration
\i Database/migrations/EXECUTE_NOW_new_wallets.sql

# Verify (should show 12 wallets)
SELECT currency, COUNT(*) FROM platform_wallets WHERE is_active = TRUE GROUP BY currency;
```

---

## ? What Was Updated

| Component | Status | File |
|-----------|--------|------|
| Configuration | ? Done | `appsettings.json` |
| Backend Service | ? Done | `Services/CryptoPaymentService.cs` |
| Database Migration | ? Ready | `Database/migrations/*.sql` |
| Documentation | ? Done | Multiple MD files |

---

## ?? Mobile App - Quick Copy/Paste

```typescript
// New types
type SupportedCurrency = 'USDC' | 'EURC';
type SupportedNetwork = 'ethereum' | 'solana' | 'polygon' | 'base' | 'avalanche' | 'stellar';

// All combinations valid
const VALID_COMBINATIONS = {
  USDC: ['ethereum', 'solana', 'polygon', 'base', 'avalanche', 'stellar'],
  EURC: ['ethereum', 'solana', 'polygon', 'base', 'avalanche', 'stellar']
};
```

---

## ?? Network Identifiers

| Network | ID | Chain ID | Confirmations |
|---------|----|----|---------------|
| Ethereum | `ethereum` | 1 | 12 |
| Solana | `solana` | - | 32 |
| Polygon | `polygon` | 137 | 128 |
| Base | `base` | 8453 | 12 |
| Avalanche | `avalanche` | 43114 | 10 |
| Stellar | `stellar` | - | 1 |

---

## ?? Currency Identifiers

| Currency | ID | Pegged To |
|----------|----|----|
| USD Coin | `USDC` | US Dollar |
| Euro Coin | `EURC` | Euro |

---

## ?? Recommended By Use Case

**Cheapest & Fastest:**
- ?? Stellar (any currency)
- ?? Solana (any currency)

**Most Trusted:**
- ?? Ethereum (any currency)

**Best Balance:**
- ?? Polygon (any currency)
- ?? Base (any currency)

---

## ?? Test Payment

```bash
# API endpoint
POST /api/payments/crypto/create

# Request body
{
  "amountUsd": 10,
  "currency": "USDC",
  "network": "stellar",
  "purpose": "premium_subscription",
  "plan": "monthly"
}

# Should return payment with wallet address
```

---

## ?? Verify Everything Works

```sql
-- Check wallets (should be 12)
SELECT currency, network FROM platform_wallets WHERE is_active = TRUE;

-- Count by currency (EURC: 6, USDC: 6)
SELECT currency, COUNT(*) FROM platform_wallets WHERE is_active = TRUE GROUP BY currency;

-- Check old wallets are inactive
SELECT COUNT(*) FROM platform_wallets WHERE is_active = FALSE;
```

---

## ?? Common Issues

**Error: "No wallet configured"**
- ? Solution: Run database migration

**Error: "Currency not supported"**
- ? Solution: Use `USDC` or `EURC` (not USDT)

**Error: "Network not supported"**
- ? Solution: Use one of 6 new networks (not Tron/BSC)

---

## ?? Full Documentation

| Doc | Purpose |
|-----|---------|
| `CRYPTO_SYSTEM_UPDATE_SUMMARY.md` | Complete overview |
| `MOBILE_APP_NEW_CRYPTO_SUPPORT.md` | Mobile integration guide |
| `Database/migrations/README_ADD_WALLETS.md` | Migration instructions |

---

## ?? Emergency Rollback

```sql
-- If something goes wrong, re-activate old wallets
UPDATE platform_wallets SET is_active = TRUE WHERE currency IN ('USDT', 'ETH', 'BNB');
UPDATE platform_wallets SET is_active = FALSE WHERE currency IN ('USDC', 'EURC');
```

---

## ? Success Criteria

- [ ] Database migration runs successfully
- [ ] 12 wallets active in database
- [ ] Can create USDC payment on Stellar
- [ ] Can create EURC payment on Ethereum
- [ ] Old currencies (USDT/ETH/BNB) return error
- [ ] Old networks (Tron/BSC) return error

---

**Created:** December 2024  
**Version:** 3.0  
**Status:** ? Ready to Deploy
