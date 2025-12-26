# HD Wallet Implementation - Quick Reference

## ?? What We Did

Implemented **Hierarchical Deterministic (HD) Wallets** (BIP44 standard) for unique address generation per payment - the industry-standard approach for crypto payment processing.

## ? Files Changed

| File | Purpose |
|------|---------|
| `Data/Migrations/20251211200000_AddHdWalletSupport.cs` | Database migration for HD wallet support |
| `Models/Entities/CryptoPaymentRequest.cs` | Added `AddressIndex` property |
| `Services/HdWalletService.cs` | Enhanced with BIP44 derivation, validation, logging |
| `Services/CryptoPaymentService.cs` | Updated to derive and store HD addresses |
| `BackgroundJobs/PaymentMonitorJob.cs` | Enhanced with HD tracking logs |
| `Dtos/PaymentResponseDto.cs` | Added `AddressIndex` field |

## ?? Files Created

| File | Purpose |
|------|---------|
| `Database/HD_WALLET_MIGRATION_GUIDE.md` | Complete database migration guide |
| `MOBILE_APP_INTEGRATION_GUIDE.md` | Mobile app integration instructions |
| `HD_WALLET_IMPLEMENTATION_SUMMARY.md` | Implementation summary |
| `Database/RUN_THIS_MIGRATION.sql` | SQL script to run on database |

## ?? Immediate Action Required

### 1. Run Database Migration

```bash
# Option A: Using EF Core
dotnet ef database update

# Option B: Run SQL manually
# Execute: Database/RUN_THIS_MIGRATION.sql
```

### 2. Configure HD Mnemonic

**?? CRITICAL: Use secure method for production!**

```bash
# Development (User Secrets) - SECURE
dotnet user-secrets set "PlatformWallets:HdMnemonic" "your twelve word mnemonic phrase"

# Production (Environment Variable)
export PlatformWallets__HdMnemonic="your twelve word mnemonic phrase"

# Docker
docker run -e PlatformWallets__HdMnemonic="your mnemonic" ...
```

### 3. Restart Application

```bash
dotnet run
# or
systemctl restart wihngo
```

### 4. Verify It Works

```bash
# Create test payment
curl -X POST https://your-api.com/api/payments/crypto/create \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amountUsd": 10,
    "currency": "USDT",
    "network": "tron",
    "purpose": "test"
  }'

# Check response has unique address and addressIndex
```

## ?? Database Verification

```sql
-- Check migration ran successfully
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'crypto_payment_requests' 
AND column_name = 'address_index';

-- Check sequences exist (should return 8 rows)
SELECT sequence_name 
FROM information_schema.sequences 
WHERE sequence_name LIKE 'hd_address_index_seq%';

-- Test payment with HD address
SELECT id, wallet_address, address_index, network, status
FROM crypto_payment_requests
WHERE address_index IS NOT NULL
ORDER BY created_at DESC
LIMIT 5;
```

## ?? Mobile App Updates

**For GitHub Copilot working on Expo React Native app:**

### Key Changes

1. **API Response Now Includes:**
   ```typescript
   interface PaymentResponseDto {
     // ...existing fields...
     addressIndex?: number;  // NEW: HD wallet derivation index
   }
   ```

2. **Each Payment Gets Unique Address:**
   - ? Don't hardcode wallet addresses
   - ? Always use `walletAddress` from API response

3. **No Breaking Changes:**
   - If app already uses `walletAddress` from API, it works automatically
   - `addressIndex` is optional informational field

### Example Usage

```typescript
// Create payment
const payment = await createPayment();

// Display unique QR code
<QRCode value={payment.walletAddress} />

// Show address
<Text selectable>{payment.walletAddress}</Text>

// Optional: Show HD info
{payment.addressIndex && (
  <Text>Payment Address #{payment.addressIndex}</Text>
)}

// Poll for status
const checkStatus = async () => {
  const updated = await fetch(`/api/payments/crypto/${payment.id}`);
  // Check updated.status: pending ? confirming ? confirmed ? completed
};
```

### Complete Integration Guide

See: `MOBILE_APP_INTEGRATION_GUIDE.md`

## ?? How It Works

```
???????????????
?  User       ?
?  Requests   ?
?  Payment    ?
???????????????
       ?
       ?
???????????????????????????????????????
?  Backend Allocates Unique Index     ?
?  SELECT nextval('hd_seq_tron')      ?
?  ? Returns: 42                      ?
???????????????????????????????????????
       ?
       ?
???????????????????????????????????????
?  Derive HD Address                  ?
?  Mnemonic ? m/44'/60'/0'/0/42       ?
?  ? TXyz1abc123...                   ?
???????????????????????????????????????
       ?
       ?
???????????????????????????????????????
?  Store in Database                  ?
?  wallet_address: TXyz1abc123...     ?
?  address_index: 42                  ?
???????????????????????????????????????
       ?
       ?
???????????????????????????????????????
?  Return to User                     ?
?  Show QR code with unique address   ?
???????????????????????????????????????
       ?
       ?
???????????????????????????????????????
?  Background Job Monitors            ?
?  Scans TXyz1abc123... for incoming  ?
?  Detects payment automatically      ?
???????????????????????????????????????
```

## ?? Benefits

| Before | After |
|--------|-------|
| ? Static address for all payments | ? Unique address per payment |
| ? Cannot identify which user paid | ? Perfect tracking via address_index |
| ? Relies on amount matching | ? Blockchain-based verification |
| ? Exchange rate issues | ? No dependency on amounts |
| ? Collision risk | ? Atomic allocation, no collisions |

## ?? Security Checklist

- [ ] HD mnemonic stored in **environment variable** (not config file)
- [ ] Mnemonic **never committed** to source control
- [ ] Production uses **secure key management** (Azure Key Vault, AWS Secrets Manager)
- [ ] Mnemonic **backed up** in multiple secure locations
- [ ] **Separate mnemonics** for dev/staging/production
- [ ] **Monitoring** enabled for all HD-derived addresses
- [ ] **Access control** limits who can view mnemonic

## ?? Troubleshooting

### Issue: All payments use same address

**Cause:** HD mnemonic not configured

**Fix:**
```bash
# Check logs for: "[HD Wallet] HD mnemonic not configured"
dotnet user-secrets set "PlatformWallets:HdMnemonic" "your mnemonic"
# Restart app
```

### Issue: Sequences not found

**Cause:** Migration not run

**Fix:**
```bash
dotnet ef database update
# Or run: Database/RUN_THIS_MIGRATION.sql
```

### Issue: Invalid mnemonic error

**Cause:** Wrong format (must be 12, 15, 18, 21, or 24 words)

**Fix:**
```csharp
// Generate new mnemonic
using NBitcoin;
var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
Console.WriteLine(mnemonic.ToString());
```

## ?? Monitoring

```sql
-- HD wallet usage report
SELECT 
    network,
    COUNT(*) as total_payments,
    COUNT(address_index) as hd_payments,
    MIN(address_index) as min_index,
    MAX(address_index) as max_index
FROM crypto_payment_requests
GROUP BY network;

-- Recent HD payments
SELECT 
    wallet_address,
    address_index,
    network,
    amount_crypto,
    currency,
    status
FROM crypto_payment_requests
WHERE address_index IS NOT NULL
ORDER BY created_at DESC
LIMIT 10;
```

## ?? Documentation

| Document | Purpose |
|----------|---------|
| `Database/HD_WALLET_MIGRATION_GUIDE.md` | Complete database migration guide with security best practices |
| `MOBILE_APP_INTEGRATION_GUIDE.md` | Mobile app integration with TypeScript examples |
| `HD_WALLET_IMPLEMENTATION_SUMMARY.md` | Implementation details and architecture |
| `Database/RUN_THIS_MIGRATION.sql` | SQL script for database migration |

## ?? BIP44 Standards

**Derivation Paths:**
```
Bitcoin:  m/44'/0'/0'/0/{index}
Ethereum: m/44'/60'/0'/0/{index}
Tron:     m/44'/60'/0'/0/{index} ? Convert to Tron
BSC:      m/44'/60'/0'/0/{index}
Polygon:  m/44'/60'/0'/0/{index}
Solana:   m/44'/501'/0'/0/{index}
```

**References:**
- [BIP39 - Mnemonic Code](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)
- [BIP44 - Multi-Account Hierarchy](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki)
- [SLIP44 - Coin Types](https://github.com/satoshilabs/slips/blob/master/slip-0044.md)

## ? Result

? **Industry-standard HD wallet implementation**
? **Unique address per payment request**
? **Robust blockchain verification**
? **No dependency on payment amounts**
? **Backward compatible with existing payments**
? **Complete documentation for mobile app**
? **Production-ready with security best practices**

## ?? Support

1. Check application logs for HD wallet messages
2. Review documentation in created markdown files
3. Test with Sepolia testnet before production
4. Contact backend team with payment ID and logs if issues persist
