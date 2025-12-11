# Quick Start Guide - USDT Multi-Network Payments

## For Developers

### 1. Database Setup (2 minutes)

Execute this SQL in your PostgreSQL database:

```sql
-- Add Ethereum wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (gen_random_uuid(), 'USDT', 'ethereum', '0x4cc28f4cea7b440858b903b5c46685cb1478cdc4', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO NOTHING;

-- Add BSC wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (gen_random_uuid(), 'USDT', 'binance-smart-chain', '0x83675000ac9915614afff618906421a2baea0020', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (currency, network, address) DO NOTHING;

-- Verify (should show 3 rows)
SELECT currency, network, address FROM platform_wallets WHERE currency = 'USDT';
```

### 2. Configuration Check

Verify `appsettings.json` has all three networks configured:

```json
{
  "CryptoPayment": {
    "USDT": {
      "Tron": { "Address": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA", "Network": "tron", "Enabled": true },
      "Ethereum": { "Address": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4", "Network": "ethereum", "ChainId": "1", "Enabled": true },
      "BinanceSmartChain": { "Address": "0x83675000ac9915614afff618906421a2baea0020", "Network": "binance-smart-chain", "ChainId": "56", "Enabled": true }
    }
  }
}
```

### 3. API Endpoints

#### Get Wallet Address
```
GET /api/payments/crypto/wallet/{currency}/{network}

Examples:
- GET /api/payments/crypto/wallet/USDT/tron
- GET /api/payments/crypto/wallet/USDT/ethereum
- GET /api/payments/crypto/wallet/USDT/binance-smart-chain
```

#### Create Payment
```
POST /api/payments/crypto/create
Headers: Authorization: Bearer {token}
Body:
{
  "amountUsd": 50.00,
  "currency": "USDT",
  "network": "ethereum",  // or "tron" or "binance-smart-chain"
  "purpose": "premium_subscription",
  "plan": "monthly",
  "birdId": "{bird-id}"
}
```

#### Verify Payment
```
POST /api/payments/crypto/{paymentId}/verify
Headers: Authorization: Bearer {token}
Body:
{
  "transactionHash": "0xabcdef...",
  "userWalletAddress": "0x123..."
}
```

#### Check Status
```
POST /api/payments/crypto/{paymentId}/check-status
Headers: Authorization: Bearer {token}
```

### 4. Testing Flow

#### Frontend Integration:
```typescript
// 1. User selects network
const network = 'ethereum'; // or 'tron' or 'binance-smart-chain'

// 2. Create payment request
const payment = await fetch('/api/payments/crypto/create', {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    amountUsd: 50,
    currency: 'USDT',
    network: network,
    purpose: 'premium_subscription',
    plan: 'monthly',
    birdId: birdId
  })
}).then(r => r.json());

// 3. Show wallet address and QR code
displayPaymentInfo(payment.paymentRequest.walletAddress, payment.paymentRequest.qrCodeData);

// 4. User sends USDT, then submit transaction hash
await fetch(`/api/payments/crypto/${payment.paymentRequest.id}/verify`, {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    transactionHash: txHash,
    userWalletAddress: userAddress
  })
}).then(r => r.json());

// 5. Poll for confirmation
const checkStatus = async () => {
  const status = await fetch(`/api/payments/crypto/${payment.paymentRequest.id}/check-status`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(r => r.json());
  
  console.log(`Status: ${status.status}, Confirmations: ${status.confirmations}/${status.requiredConfirmations}`);
  
  if (status.status === 'completed') {
    // Premium activated!
    clearInterval(pollInterval);
  }
};

const pollInterval = setInterval(checkStatus, 15000); // Every 15 seconds
```

### 5. Network Comparison Table

| Network | Confirmations | Avg Time | Fee | Best For |
|---------|--------------|----------|-----|----------|
| TRON | 19 | ~3 min | Very Low | Small payments |
| Ethereum | 12 | ~3 min | Variable | High security |
| BSC | 15 | ~45 sec | Low | Fast confirmations |

### 6. Common Issues & Solutions

**Issue**: Payment not detecting
- ? Check transaction on blockchain explorer
- ? Verify correct USDT token sent (not native token)
- ? Ensure transaction has at least 1 confirmation

**Issue**: Stuck confirming
- ? Check Hangfire dashboard (`/hangfire`)
- ? Verify PaymentMonitorJob is running
- ? Manually call `/check-status`

**Issue**: Wrong network
- ? Cannot recover - must create new payment
- ? Funds sent to wrong network may be lost

### 7. Background Jobs Running

Check Hangfire dashboard at `/hangfire` to verify:
- ? `update-exchange-rates` - Every 5 minutes
- ? `monitor-payments` - Every 30 seconds
- ? `expire-payments` - Every hour

### 8. Testing with Testnet

Use testnet addresses for development:

**TRON Nile Testnet**
- Get testnet TRX: https://nileex.io/join/getJoinPage
- Explorer: https://nile.tronscan.org

**Ethereum Sepolia**
- Get testnet ETH: https://sepoliafaucet.com/
- Explorer: https://sepolia.etherscan.io

**BSC Testnet**
- Get testnet BNB: https://testnet.binance.org/faucet-smart
- Explorer: https://testnet.bscscan.com

### 9. Production Checklist

Before going live:
- [ ] Update wallet addresses in `appsettings.json`
- [ ] Run database migration
- [ ] Configure API keys (TronGrid, Infura)
- [ ] Test with real but small amounts ($1-5)
- [ ] Monitor Hangfire jobs
- [ ] Set up wallet balance alerts
- [ ] Document support procedures

### 10. Support

For issues:
1. Check Hangfire dashboard for job failures
2. Review application logs
3. Verify blockchain explorer for transaction status
4. Check API keys are configured correctly
5. See full documentation: `Documentation/USDT_MULTI_NETWORK_IMPLEMENTATION.md`

---

**Setup Time**: ~5 minutes
**Testing Time**: ~10 minutes per network
**Total Time to Production**: ~30 minutes

Good luck! ??
