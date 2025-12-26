# ?? WIHNGO CRYPTO PAYMENT DEPLOYMENT GUIDE

## ? Configuration Complete

Your Wihngo crypto payment system has been configured with your actual wallet addresses.

---

## ?? Your Wallet Addresses

### **USDC & EURC** (both currencies use same addresses)

| Network | Address | Format |
|---------|---------|--------|
| **Solana** | `AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn` | Base58 |
| **Ethereum** | `0xfcc173a7569492439ec3df467d0ec0c05c0f541c` | 0x... |
| **Polygon** | `0xfcc173a7569492439ec3df467d0ec0c05c0f541c` | 0x... |
| **Base** | `0xfcc173a7569492439ec3df467d0ec0c05c0f541c` | 0x... |
| **Stellar** | `GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG` | G... |

---

## ?? Supported Combinations

**10 Total Combinations:**

| Currency | Solana | Ethereum | Polygon | Base | Stellar |
|----------|:------:|:--------:|:-------:|:----:|:-------:|
| **USDC** | ? | ? | ? | ? | ? |
| **EURC** | ? | ? | ? | ? | ? |

---

## ?? DEPLOYMENT STEPS

### Step 1: Run Database Migration

```bash
# Connect to your database
psql "postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require"

# Run the migration
\i Database/migrations/EXECUTE_NOW_new_wallets.sql
```

### Step 2: Verify Wallets

```sql
-- Should return 10 rows
SELECT currency, network, address FROM platform_wallets WHERE is_active = TRUE ORDER BY currency, network;
```

Expected output:
```
 currency |  network  |                      address                       
----------+-----------+---------------------------------------------------
 EURC     | base      | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 EURC     | ethereum  | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 EURC     | polygon   | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 EURC     | solana    | AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn
 EURC     | stellar   | GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG
 USDC     | base      | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 USDC     | ethereum  | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 USDC     | polygon   | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 USDC     | solana    | AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn
 USDC     | stellar   | GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG
(10 rows)
```

### Step 3: Test Payment Creation

```bash
# Test USDC on Solana (recommended - cheapest & fastest)
curl -X POST https://your-api.com/api/payments/crypto/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "amountUsd": 10,
    "currency": "USDC",
    "network": "solana",
    "purpose": "premium_subscription",
    "plan": "monthly"
  }'
```

Should return:
```json
{
  "id": "...",
  "walletAddress": "AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn",
  "currency": "USDC",
  "network": "solana",
  "amountCrypto": 10.0,
  "status": "pending"
}
```

---

## ?? Network Specifications

| Network | Chain ID | Confirmations | Avg Fee | Speed | Your Address |
|---------|----------|---------------|---------|-------|--------------|
| **Solana** | - | 32 | $0.001 | ~30 sec | `AE6jnde...ADacn` |
| **Ethereum** | 1 | 12 | $5-50 | ~3 min | `0xfcc17...f541c` |
| **Polygon** | 137 | 128 | $0.01-0.10 | ~2 min | `0xfcc17...f541c` |
| **Base** | 8453 | 12 | $0.01-0.50 | ~2 min | `0xfcc17...f541c` |
| **Stellar** | - | 1 | <$0.001 | ~5 sec | `GDMOO...JC4HG` |

---

## ?? Mobile App Configuration

The mobile app needs these network identifiers:

```typescript
// Supported networks
type SupportedNetwork = 'solana' | 'ethereum' | 'polygon' | 'base' | 'stellar';

// Supported currencies
type SupportedCurrency = 'USDC' | 'EURC';

// All combinations are valid
const VALID_COMBINATIONS = {
  USDC: ['solana', 'ethereum', 'polygon', 'base', 'stellar'],
  EURC: ['solana', 'ethereum', 'polygon', 'base', 'stellar']
};
```

---

## ?? Recommended Payment Options for Users

### **Best for Most Users:**
1. ?? **Stellar** - Nearly free ($<0.001) + instant (5 sec)
2. ?? **Solana** - Very cheap ($0.001) + fast (30 sec)
3. ?? **Polygon** - Low cost ($0.01-0.10) + reasonable speed (2 min)

### **For Maximum Trust:**
- **Ethereum** - Most established, but higher fees ($5-50)

### **For Coinbase Users:**
- **Base** - Coinbase's L2, good balance of cost/trust

---

## ?? Security Notes

### **Your Addresses:**
- ? **EVM Chains** (Ethereum, Polygon, Base): Same address `0xfcc173a7569492439ec3df467d0ec0c05c0f541c`
  - This is common practice for EVM-compatible chains
  - You control the same private key across all three

- ? **Solana**: Dedicated address `AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn`

- ? **Stellar**: Dedicated address `GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG`

### **HD Wallet Support:**
If you configure an HD mnemonic in `appsettings.json`, the system will derive unique addresses per payment while still maintaining control through your master seed.

```json
"PlatformWallets": {
  "HdMnemonic": "your twelve or twenty-four word seed phrase here"
}
```

---

## ?? Testing Checklist

Before going live, test each combination:

### USDC Payments:
- [ ] USDC on Solana
- [ ] USDC on Ethereum
- [ ] USDC on Polygon
- [ ] USDC on Base
- [ ] USDC on Stellar

### EURC Payments:
- [ ] EURC on Solana
- [ ] EURC on Ethereum
- [ ] EURC on Polygon
- [ ] EURC on Base
- [ ] EURC on Stellar

### Test Flow:
1. Create payment ? Get wallet address
2. Send small test amount to address
3. Verify transaction detected
4. Confirm payment completes after required confirmations

---

## ?? Expected Transaction Fees (to receive)

When users send payments to your addresses:

| Network | User Pays | You Receive |
|---------|-----------|-------------|
| Stellar | ~$0.00001 | 99.999% |
| Solana | ~$0.001 | 99.9% |
| Polygon | $0.01-0.10 | 99%+ |
| Base | $0.01-0.50 | 99%+ |
| Ethereum | $5-50 | 95%+ |

**Note:** Users pay network fees, not you. The amounts shown are what users pay to send the transaction.

---

## ?? Monitoring Your Wallets

### Block Explorers:

| Network | Explorer | Your Wallet Link |
|---------|----------|------------------|
| **Solana** | Solscan | `https://solscan.io/account/AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn` |
| **Ethereum** | Etherscan | `https://etherscan.io/address/0xfcc173a7569492439ec3df467d0ec0c05c0f541c` |
| **Polygon** | PolygonScan | `https://polygonscan.com/address/0xfcc173a7569492439ec3df467d0ec0c05c0f541c` |
| **Base** | BaseScan | `https://basescan.org/address/0xfcc173a7569492439ec3df467d0ec0c05c0f541c` |
| **Stellar** | Stellar Expert | `https://stellar.expert/explorer/public/account/GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG` |

---

## ?? Support Queries

### Check wallet configuration:
```sql
SELECT 
    currency,
    network,
    address,
    is_active,
    created_at
FROM platform_wallets
WHERE is_active = TRUE
ORDER BY currency, network;
```

### Count wallets by currency:
```sql
SELECT currency, COUNT(*) 
FROM platform_wallets 
WHERE is_active = TRUE 
GROUP BY currency;
-- Should return: EURC: 5, USDC: 5
```

### View recent payments:
```sql
SELECT 
    id,
    currency,
    network,
    amount_usd,
    amount_crypto,
    wallet_address,
    status,
    created_at
FROM crypto_payment_requests
WHERE created_at > NOW() - INTERVAL '7 days'
ORDER BY created_at DESC
LIMIT 20;
```

---

## ?? Next Steps

1. ? **Run database migration** (Step 1 above)
2. ? **Verify 10 wallets active** (Step 2 above)
3. ? **Test payment creation** (Step 3 above)
4. ?? **Update mobile app** with new payment options
5. ?? **Notify users** of new crypto payment options
6. ?? **Monitor** first transactions on each network

---

## ?? Quick Commands

```bash
# Connect to database
export DB_URL="postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require"

# Run migration
psql "$DB_URL" -f Database/migrations/EXECUTE_NOW_new_wallets.sql

# Check status
psql "$DB_URL" -c "SELECT currency, COUNT(*) FROM platform_wallets WHERE is_active = TRUE GROUP BY currency;"

# View your addresses
psql "$DB_URL" -c "SELECT currency, network, address FROM platform_wallets WHERE is_active = TRUE ORDER BY currency, network;"
```

---

**Status:** ? Ready to Deploy  
**Your Wallets:** ? Configured  
**Migration Ready:** ? Yes  
**Action Required:** ?? Run database migration now!

---

**Wihngo Platform**  
**Crypto Payment System v3.0**  
**Last Updated:** December 2024
