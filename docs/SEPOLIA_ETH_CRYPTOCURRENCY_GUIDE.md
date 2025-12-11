# Sepolia ETH Cryptocurrency Support - Complete Guide

## Overview

Sepolia ETH is now fully supported as a cryptocurrency for testing payments on the Sepolia testnet. This document explains how the system handles Sepolia ETH and how it differs from other supported cryptocurrencies.

---

## Key Concepts

### 1. **Sepolia ETH = Testnet ETH**

- Sepolia is Ethereum's official testnet
- Sepolia ETH is the native currency (not a token like USDT)
- Exchange rate is the same as mainnet ETH
- Has **no real monetary value** - safe for development

### 2. **Currency vs Network**

When creating a payment for Sepolia:
- **Currency**: `ETH` (the native cryptocurrency)
- **Network**: `sepolia` (the testnet)

This is different from other combinations:
- USDT on Tron: `currency: 'USDT'`, `network: 'tron'`
- USDT on Ethereum: `currency: 'USDT'`, `network: 'ethereum'`
- ETH on Sepolia: `currency: 'ETH'`, `network: 'sepolia'`

---

## Backend Implementation

### Exchange Rate Handling

Sepolia ETH uses the **same exchange rate as mainnet ETH** because:
1. It's the same cryptocurrency (just on a different network)
2. Exchange rate APIs (like CoinGecko) don't distinguish between mainnet and testnet
3. The value is calculated the same way

**In `CryptoPaymentService.cs`:**
```csharp
// When currency is 'ETH', it looks up the ETH rate
var rate = await _context.CryptoExchangeRates
    .FirstOrDefaultAsync(r => r.Currency == "ETH");
```

### Platform Wallet Configuration

**In `AppDbContext.cs`:**
```csharp
new PlatformWallet
{
    Id = Guid.NewGuid(),
    Currency = "ETH",                               // Native ETH
    Network = "sepolia",                            // Testnet
    Address = "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
}
```

### Blockchain Verification

**In `BlockchainVerificationService.cs`:**
- Sepolia uses the same EVM verification as Ethereum mainnet
- RPC URL: `https://sepolia.infura.io/v3/{ProjectId}`
- Requires only **6 confirmations** (vs 12 on mainnet)
- Native ETH transactions use `Web3.Convert.FromWei()` for amount

### Payment Processing

**In `CryptoPaymentService.cs`:**
- Required confirmations: **6 blocks** (~1.2 minutes)
- Payment URI: `ethereum:{address}` (standard EVM format)
- Token decimals: **18** (native ETH standard)

---

## Frontend Implementation

### Payment Request Example

```javascript
// For Sepolia testnet
const payment = await cryptoPaymentApi.createPayment({
  amountUsd: 10.00,
  currency: 'ETH',        // ? Use ETH, not USDT
  network: 'sepolia',     // ? Testnet
  birdId: yourBirdId,
  purpose: 'premium_subscription',
  plan: 'monthly'
});
```

### Dynamic Currency Selection

**Updated component logic:**
```javascript
useEffect(() => {
  const initPayment = async () => {
    try {
      // Dynamically set currency based on network
      const currency = selectedNetwork === 'sepolia' ? 'ETH' : 'USDT';
      
      await createPayment({
        amountUsd,
        currency,              // ETH for Sepolia, USDT for others
        network: selectedNetwork,
        birdId,
        purpose: 'premium_subscription',
        plan
      });
    } catch (err) {
      Alert.alert('Error', 'Failed to create payment request');
    }
  };
  initPayment();
}, [selectedNetwork]);
```

### Display Currency Correctly

```javascript
// Don't hardcode currency
<Text>{payment.amountCrypto.toFixed(6)} {payment.currency}</Text>

// Not this:
<Text>{payment.amountCrypto.toFixed(6)} USDT</Text>
```

---

## Complete Flow Example

### 1. User Selects Sepolia Network

```javascript
const networks = [
  { id: 'tron', name: 'Tron (TRC-20)', fee: 'Low', currency: 'USDT' },
  { id: 'ethereum', name: 'Ethereum (ERC-20)', fee: 'High', currency: 'USDT' },
  { id: 'sepolia', name: 'Sepolia Testnet (ETH)', fee: 'Low (Test)', currency: 'ETH' }
];
```

### 2. Create Payment Request

**Request:**
```json
{
  "amountUsd": 10.00,
  "currency": "ETH",
  "network": "sepolia",
  "birdId": "123e4567-e89b-12d3-a456-426614174000",
  "purpose": "premium_subscription",
  "plan": "monthly"
}
```

**Response:**
```json
{
  "paymentRequest": {
    "id": "payment-guid",
    "userId": "user-guid",
    "amountUsd": 10.00,
    "amountCrypto": 0.003333,     // Based on ETH rate (~$3000)
    "currency": "ETH",             // Native ETH
    "network": "sepolia",          // Testnet
    "exchangeRate": 3000.00,       // Current ETH/USD rate
    "walletAddress": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
    "requiredConfirmations": 6,    // Faster than mainnet
    "status": "pending"
  }
}
```

### 3. User Sends Payment

- Opens MetaMask on Sepolia network
- Sends **0.003333 ETH** to the wallet address
- Transaction appears immediately on Sepolia Etherscan

### 4. Verify Transaction

**Request:**
```json
{
  "transactionHash": "0xabc123...",
  "userWalletAddress": "0xUSER_ADDRESS..."
}
```

**Backend Process:**
1. Connects to Sepolia RPC
2. Fetches transaction details
3. Verifies amount: `Web3.Convert.FromWei(tx.Value.Value)` = 0.003333 ETH
4. Checks recipient: `0x4cc28f4cea7b440858b903b5c46685cb1478cdc4` ?
5. Confirms amount matches (±1% tolerance)
6. Status: `confirming` (0/6 confirmations)

### 5. Confirmations Progress

```
Block 1: Status = confirming (1/6) - ~12 seconds
Block 2: Status = confirming (2/6) - ~24 seconds
Block 3: Status = confirming (3/6) - ~36 seconds
Block 4: Status = confirming (4/6) - ~48 seconds
Block 5: Status = confirming (5/6) - ~60 seconds
Block 6: Status = confirmed (6/6) - ~72 seconds
Status changed to: completed - Premium activated!
```

---

## Important Differences: ETH vs USDT

| Aspect | Sepolia ETH | USDT (Tron/Ethereum/BSC) |
|--------|-------------|--------------------------|
| **Type** | Native cryptocurrency | ERC-20/TRC-20/BEP-20 Token |
| **Decimals** | 18 | 6 (Tron, Ethereum) / 18 (BSC) |
| **Transaction Type** | Native value transfer | Contract call |
| **Amount Extraction** | `Web3.Convert.FromWei(value)` | Decode from event logs |
| **Logs Required** | No | Yes (Transfer event) |
| **Exchange Rate** | Variable (~$3000) | Stable (~$1.00) |

---

## Testing Checklist

### Backend ?
- [x] Sepolia wallet in `platform_wallets` table
- [x] ETH exchange rate in `crypto_exchange_rates` table
- [x] Sepolia network in `BlockchainVerificationService`
- [x] Sepolia confirmations (6 blocks) in `CryptoPaymentService`
- [x] Native ETH amount parsing (18 decimals)
- [x] Payment URI generation for Sepolia

### Frontend ?
- [x] Sepolia network option in UI
- [x] Dynamic currency selection (ETH for Sepolia)
- [x] Display actual currency from API response
- [x] Instructions show correct currency
- [x] Testnet warning for Sepolia

### Documentation ?
- [x] Supported cryptocurrencies table updated
- [x] Sepolia testing section added
- [x] Code examples with ETH currency
- [x] Production migration guide

---

## Common Issues & Solutions

### Issue 1: "Exchange rate not available for ETH"

**Cause:** ETH rate not in database
**Solution:** Run the exchange rate update job or add manually:
```sql
INSERT INTO crypto_exchange_rates (id, currency, usd_rate, source, last_updated)
VALUES (gen_random_uuid(), 'ETH', 3000.00, 'coingecko', NOW());
```

### Issue 2: Payment shows USDT instead of ETH

**Cause:** Frontend hardcoded currency
**Solution:** Use `payment.currency` from API response:
```javascript
// Correct
{payment.amountCrypto} {payment.currency}

// Wrong
{payment.amountCrypto} USDT
```

### Issue 3: Amount mismatch error

**Cause:** ETH has 18 decimals, very precise amounts
**Solution:** System allows 1% tolerance. Ensure wallet sends exact amount or slightly more.

### Issue 4: Transaction not found

**Cause:** Blockchain propagation delay or wrong RPC
**Solution:** 
- Wait 15-20 seconds after sending
- Verify Infura project ID is configured
- Check transaction on Sepolia Etherscan first

---

## Configuration Requirements

### Backend (`appsettings.json`)

```json
{
  "BlockchainSettings": {
    "Infura": {
      "ProjectId": "YOUR_INFURA_PROJECT_ID"
    }
  },
  "ExchangeRateSettings": {
    "CoinGeckoApiKey": "YOUR_COINGECKO_KEY" // Optional
  }
}
```

### Environment Variables (Alternative)

```bash
BlockchainSettings__Infura__ProjectId=your_infura_key
ExchangeRateSettings__CoinGeckoApiKey=your_coingecko_key
```

---

## Production Considerations

### DO NOT Use Sepolia in Production

Sepolia is **testnet only**. For production:

```javascript
// Development (Testnet)
{
  currency: 'ETH',
  network: 'sepolia'
}

// Production (Mainnet)
{
  currency: 'ETH',        // For real ETH payments
  network: 'ethereum'
}

// Or use stablecoins (recommended)
{
  currency: 'USDT',       // Stable value
  network: 'tron'         // Low fees
}
```

### Remove Sepolia Option in Production

```javascript
// Production network list (no Sepolia)
const networks = process.env.NODE_ENV === 'production'
  ? [
      { id: 'tron', name: 'Tron (TRC-20)', currency: 'USDT' },
      { id: 'ethereum', name: 'Ethereum (ERC-20)', currency: 'USDT' },
      { id: 'binance-smart-chain', name: 'BSC (BEP-20)', currency: 'USDT' }
    ]
  : [
      // Include Sepolia only in development
      { id: 'sepolia', name: 'Sepolia Testnet (ETH)', currency: 'ETH' },
      ...productionNetworks
    ];
```

---

## API Response Examples

### Get Platform Wallet

**Request:**
```http
GET /api/payments/crypto/wallet/ETH/sepolia
```

**Response:**
```json
{
  "currency": "ETH",
  "network": "sepolia",
  "address": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
  "qrCode": "ethereum:0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
  "isActive": true
}
```

### Get Exchange Rate

**Request:**
```http
GET /api/payments/crypto/rates/ETH
```

**Response:**
```json
{
  "currency": "ETH",
  "usdRate": 3000.00,
  "lastUpdated": "2025-01-15T10:30:00Z",
  "source": "coingecko"
}
```

---

## Summary

? **Sepolia ETH is fully supported** with:
- Currency: `ETH` (not USDT)
- Network: `sepolia`
- Exchange rate: Same as mainnet ETH
- Confirmations: 6 blocks (faster)
- Decimals: 18 (native ETH)
- Testnet only - no real value

? **Key Implementation Points:**
- Backend automatically uses ETH exchange rate
- Frontend must specify `currency: 'ETH'` for Sepolia
- Display currency dynamically from API response
- Never use Sepolia in production

? **Testing Ready:**
- Get test ETH from faucets
- Use MetaMask on Sepolia network
- Send exact amount shown
- Wait ~1.2 minutes for confirmation

For detailed testing instructions, see `SEPOLIA_QUICK_START.md`.

---

**Last Updated:** January 2025  
**Currency:** ETH  
**Network:** sepolia (testnet)  
**Confirmations:** 6 blocks  
**Receive Address:** 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4
