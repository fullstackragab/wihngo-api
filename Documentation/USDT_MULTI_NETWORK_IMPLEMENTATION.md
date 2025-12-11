# USDT Multi-Network Payment System Implementation

## Overview

The Wihngo platform now supports USDT cryptocurrency payments across three major blockchain networks:
- **TRON** (TRC-20)
- **Ethereum** (ERC-20)
- **Binance Smart Chain** (BEP-20)

This gives users flexibility to pay with USDT using their preferred blockchain network.

---

## Network Details

### TRON (TRC-20)
- **Platform Wallet Address**: `TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA`
- **Network ID**: `tron`
- **Required Confirmations**: 19
- **Token Contract**: TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t (USDT TRC-20)
- **Average Confirmation Time**: ~3 minutes
- **Transaction Fees**: Very low (~1-5 TRX)
- **Best For**: Low-fee transactions, frequent small payments

### Ethereum (ERC-20)
- **Platform Wallet Address**: `0x4cc28f4cea7b440858b903b5c46685cb1478cdc4`
- **Network ID**: `ethereum`
- **Chain ID**: 1
- **Required Confirmations**: 12
- **Token Contract**: 0xdAC17F958D2ee523a2206206994597C13D831ec7 (USDT ERC-20)
- **Average Confirmation Time**: ~3 minutes
- **Transaction Fees**: Variable (depends on gas prices)
- **Best For**: High-value transactions, maximum security

### Binance Smart Chain (BEP-20)
- **Platform Wallet Address**: `0x83675000ac9915614afff618906421a2baea0020`
- **Network ID**: `binance-smart-chain`
- **Chain ID**: 56
- **Required Confirmations**: 15
- **Token Contract**: 0x55d398326f99059fF775485246999027B3197955 (USDT BEP-20)
- **Average Confirmation Time**: ~45 seconds
- **Transaction Fees**: Low (~0.1-0.5 BNB)
- **Best For**: Fast confirmations, moderate fees

---

## Configuration

### appsettings.json

```json
{
  "CryptoPayment": {
    "USDT": {
      "Tron": {
        "Address": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
        "Network": "tron",
        "ChainId": null,
        "Enabled": true
      },
      "Ethereum": {
        "Address": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
        "Network": "ethereum",
        "ChainId": "1",
        "Enabled": true
      },
      "BinanceSmartChain": {
        "Address": "0x83675000ac9915614afff618906421a2baea0020",
        "Network": "binance-smart-chain",
        "ChainId": "56",
        "Enabled": true
      }
    },
    "MinimumAmount": 10.0,
    "PaymentExpirationMinutes": 30,
    "PollingIntervalSeconds": 15
  },
  "BlockchainSettings": {
    "TronGrid": {
      "ApiUrl": "https://api.trongrid.io",
      "ApiKey": "YOUR_TRONGRID_API_KEY"
    },
    "Infura": {
      "ProjectId": "YOUR_INFURA_PROJECT_ID"
    }
  }
}
```

---

## Database Migration

### Execute Migration

Run the following SQL file to add Ethereum and BSC wallet support:

```bash
psql -h YOUR_HOST -U YOUR_USER -d wihngo -f Database/migrations/add_multi_network_usdt_wallets.sql
```

Or execute directly in your PostgreSQL client:

```sql
-- Add Ethereum wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'USDT',
    'ethereum',
    '0x4cc28f4cea7b440858b903b5c46685cb1478cdc4',
    TRUE,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (currency, network, address) DO NOTHING;

-- Add BSC wallet
INSERT INTO platform_wallets (id, currency, network, address, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'USDT',
    'binance-smart-chain',
    '0x83675000ac9915614afff618906421a2baea0020',
    TRUE,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (currency, network, address) DO NOTHING;
```

### Verify Installation

```sql
-- Check all USDT wallets
SELECT currency, network, address, is_active
FROM platform_wallets
WHERE currency = 'USDT'
ORDER BY network;
```

Expected output:
```
currency | network              | address                                    | is_active
---------|----------------------|--------------------------------------------|-----------
USDT     | binance-smart-chain  | 0x83675000ac9915614afff618906421a2baea0020 | true
USDT     | ethereum             | 0x4cc28f4cea7b440858b903b5c46685cb1478cdc4 | true
USDT     | tron                 | TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA        | true
```

---

## API Usage

### 1. Get Available Wallets

```http
GET /api/payments/crypto/wallet/USDT/tron
GET /api/payments/crypto/wallet/USDT/ethereum
GET /api/payments/crypto/wallet/USDT/binance-smart-chain
```

**Response Example:**
```json
{
  "currency": "USDT",
  "network": "ethereum",
  "address": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
  "qrCode": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
  "isActive": true
}
```

### 2. Create Payment Request

```http
POST /api/payments/crypto/create
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "amountUsd": 50.00,
  "currency": "USDT",
  "network": "ethereum",
  "purpose": "premium_subscription",
  "plan": "monthly",
  "birdId": "123e4567-e89b-12d3-a456-426614174000"
}
```

**Response:**
```json
{
  "paymentRequest": {
    "id": "789e4567-e89b-12d3-a456-426614174000",
    "amountUsd": 50.00,
    "amountCrypto": 50.00,
    "currency": "USDT",
    "network": "ethereum",
    "walletAddress": "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
    "qrCodeData": "ethereum:0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
    "paymentUri": "ethereum:0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
    "requiredConfirmations": 12,
    "status": "pending",
    "expiresAt": "2024-12-11T12:30:00Z"
  },
  "message": "Payment request created successfully"
}
```

### 3. Verify Payment Transaction

After the user sends USDT to the wallet address, verify the transaction:

```http
POST /api/payments/crypto/{paymentId}/verify
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "transactionHash": "0xabcdef1234567890...",
  "userWalletAddress": "0x1234567890abcdef..."
}
```

### 4. Check Payment Status

Poll this endpoint to check confirmation status:

```http
POST /api/payments/crypto/{paymentId}/check-status
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "id": "789e4567-e89b-12d3-a456-426614174000",
  "status": "confirming",
  "confirmations": 8,
  "requiredConfirmations": 12,
  "transactionHash": "0xabcdef1234567890..."
}
```

---

## Payment Flow

### User Journey

1. **Select Network**: User chooses their preferred network (TRON, Ethereum, or BSC)
2. **Create Payment**: Frontend calls `/api/payments/crypto/create` with network parameter
3. **Display Payment Info**: Show QR code and wallet address to user
4. **User Sends USDT**: User transfers USDT from their wallet to the provided address
5. **Submit Transaction**: User submits the transaction hash via `/verify` endpoint
6. **Monitor Confirmations**: Frontend polls `/check-status` every 15 seconds
7. **Payment Confirmed**: After required confirmations, payment status becomes "confirmed"
8. **Premium Activated**: Backend automatically activates premium subscription

### Status Progression

```
pending ? confirming ? confirmed ? completed
                     ?
                  expired/cancelled/failed
```

---

## Network Selection Guidance

### Recommend TRON When:
- User wants lowest transaction fees
- Payment amount is under $100
- User is familiar with TRON ecosystem
- Fast confirmation is desired

### Recommend Ethereum When:
- User prioritizes security and decentralization
- Payment amount is large ($1000+)
- User already has ETH for gas fees
- User prefers the most established network

### Recommend BSC When:
- User wants balance of speed and cost
- User is familiar with Binance ecosystem
- User wants faster confirmations than Ethereum
- User has BNB available for gas fees

---

## Blockchain Integration

### TRON Verification
- **API**: TronGrid API
- **Endpoint**: `https://api.trongrid.io`
- **Token Standard**: TRC-20
- **Decimals**: 6
- **Verification Method**: Transaction logs parsing

### Ethereum Verification
- **Provider**: Infura / Alchemy
- **Endpoint**: `https://mainnet.infura.io/v3/PROJECT_ID`
- **Token Standard**: ERC-20
- **Decimals**: 6
- **Verification Method**: Web3 transaction receipt

### BSC Verification
- **Provider**: BSC RPC
- **Endpoint**: `https://bsc-dataseed.binance.org`
- **Token Standard**: BEP-20
- **Decimals**: 18
- **Verification Method**: Web3 transaction receipt

---

## Security Considerations

### Wallet Security
- ? **Public addresses only** stored in database
- ? **No private keys** in configuration or code
- ? **Hardware wallet** recommended for production wallets
- ? **Multi-signature** wallets recommended for high-value storage

### Transaction Verification
- ? **On-chain verification** for all transactions
- ? **Amount validation** with 1% tolerance
- ? **Recipient address** validation
- ? **Confirmation counting** before completion
- ? **Duplicate transaction** prevention

### API Security
- ? **JWT authentication** required
- ? **Rate limiting** on payment endpoints
- ? **User ownership** validation
- ? **HTTPS only** in production

---

## Monitoring & Maintenance

### Background Jobs

The system includes automatic background jobs (configured in `Program.cs`):

1. **Exchange Rate Updates**: Every 5 minutes
   ```csharp
   RecurringJob.AddOrUpdate<ExchangeRateUpdateJob>(
       "update-exchange-rates",
       job => job.UpdateExchangeRatesAsync(),
       "*/5 * * * *"
   );
   ```

2. **Payment Monitoring**: Every 30 seconds
   ```csharp
   RecurringJob.AddOrUpdate<PaymentMonitorJob>(
       "monitor-payments",
       job => job.MonitorPendingPaymentsAsync(),
       "*/30 * * * * *"
   );
   ```

3. **Payment Expiration**: Every hour
   ```csharp
   RecurringJob.AddOrUpdate<PaymentMonitorJob>(
       "expire-payments",
       job => job.ExpireOldPaymentsAsync(),
       "0 * * * *"
   );
   ```

### Health Checks

Monitor these metrics:
- Payment success rate by network
- Average confirmation time
- Failed transaction rate
- API response times
- Blockchain API availability

### Alerts

Set up alerts for:
- Payment stuck in "confirming" for > 30 minutes
- Exchange rate API failures
- Blockchain API downtime
- Unusual transaction patterns
- Wallet balance changes

---

## Testing

### Test Networks

For development and testing, use testnet addresses:

**TRON Testnet (Nile)**
- Testnet Faucet: https://nileex.io/join/getJoinPage
- Explorer: https://nile.tronscan.org

**Ethereum Testnet (Sepolia)**
- Testnet Faucet: https://sepoliafaucet.com/
- Explorer: https://sepolia.etherscan.io

**BSC Testnet**
- Testnet Faucet: https://testnet.binance.org/faucet-smart
- Explorer: https://testnet.bscscan.com

### Manual Testing Steps

1. Create payment request for each network
2. Send actual testnet USDT to wallet addresses
3. Submit transaction hash
4. Verify confirmation counting
5. Check premium activation after confirmation
6. Test payment expiration (wait 30 minutes)
7. Test payment cancellation
8. Test error scenarios (wrong amount, wrong address, etc.)

---

## Troubleshooting

### Issue: Payment not detecting transaction

**Solution:**
1. Verify transaction hash is correct
2. Check transaction on blockchain explorer
3. Ensure USDT was sent (not native token)
4. Verify recipient address matches payment request
5. Check if transaction has at least 1 confirmation

### Issue: Stuck in "confirming" status

**Solution:**
1. Check current confirmation count
2. Verify blockchain is not experiencing congestion
3. Check if PaymentMonitorJob is running
4. Manually trigger `/check-status` endpoint
5. Review background job logs in Hangfire dashboard

### Issue: Wrong network selected

**Solution:**
- User must create new payment request with correct network
- Cancel existing payment request
- USDT sent to wrong network may be lost (cross-chain transfers are not supported)

---

## Production Deployment Checklist

- [ ] Update wallet addresses in `appsettings.json`
- [ ] Execute database migration
- [ ] Configure blockchain API keys (TronGrid, Infura)
- [ ] Test all three networks with small amounts
- [ ] Enable HTTPS/SSL
- [ ] Set up monitoring and alerts
- [ ] Configure rate limiting
- [ ] Implement proper error logging
- [ ] Set up wallet balance monitoring
- [ ] Configure automatic withdrawal to cold storage
- [ ] Document incident response procedures
- [ ] Train support team on crypto payment issues
- [ ] Prepare user documentation with screenshots

---

## Support & Resources

### API Documentation
- TronGrid: https://developers.tron.network/docs
- Infura: https://docs.infura.io/
- BSCScan: https://docs.bscscan.com/

### Libraries Used
- **Nethereum**: Ethereum/BSC interaction
- **Hangfire**: Background job processing
- **Entity Framework Core**: Database ORM

### Community Resources
- TRON Developer Forum: https://forum.tron.network/
- Ethereum Stack Exchange: https://ethereum.stackexchange.com/
- BSC Community: https://www.binance.org/en/community

---

## Change Log

### Version 1.0.0 (December 2024)
- ? Added USDT support on TRON network
- ? Added USDT support on Ethereum network
- ? Added USDT support on Binance Smart Chain network
- ? Implemented automatic payment monitoring
- ? Added exchange rate updates
- ? Integrated premium subscription activation
- ? Added comprehensive API endpoints

---

## License & Legal

**Important**: Cryptocurrency payment processing involves financial regulations. Ensure compliance with:
- Local money transmission laws
- KYC/AML requirements
- Tax reporting obligations
- Consumer protection regulations

Consult with legal counsel before deploying to production.

---

**Last Updated**: December 11, 2024
**Version**: 1.0.0
**Maintained By**: Wihngo Development Team
