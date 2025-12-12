# Wihngo Payment System - Configuration Guide

## Overview

The Wihngo payment backend supports full payment lifecycle with mandatory invoice/receipt issuance for every successful payment. Supported payment methods:
- **PayPal** (instant, automated refunds)
- **Solana** (USDC/EURC SPL tokens)
- **Base** (USDC/EURC ERC-20 tokens)

## Environment Variables

### Database
```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=your_password"
```

### JWT Authentication
```bash
Jwt__Key="your-secret-key-min-32-chars"
Jwt__Issuer="https://wihngo.com"
Jwt__Audience="https://wihngo.com"
```

### Invoice Configuration
```json
{
  "Invoice": {
    "CompanyName": "Wihngo Inc.",
    "CompanyAddress": "123 Main Street, Suite 100, San Francisco, CA 94105, USA",
    "TaxNumber": "",
    "ContactEmail": "support@wihngo.com",
    "SupportPhone": "+1-555-0100",
    "WebsiteUrl": "https://wihngo.com",
    "InvoicePrefix": "WIH",
    "DefaultReceiptNotes": "This payment is not a charitable donation. Wihngo is a for-profit company and this payment is not eligible for tax deduction unless otherwise stated.",
    "StoragePath": "invoices",
    "UseS3Storage": false,
    "S3BucketName": "",
    "S3Region": "",
    "InvoiceExpiryMinutes": 30
  }
}
```

### SMTP Configuration (for email receipts)
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseSsl": true,
    "Username": "noreply@wihngo.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@wihngo.com",
    "FromName": "Wihngo"
  }
}
```

### PayPal Configuration
```json
{
  "PayPal": {
    "ClientId": "your-paypal-client-id",
    "ClientSecret": "your-paypal-client-secret",
    "Mode": "sandbox",
    "WebhookId": "your-webhook-id",
    "ReturnUrl": "https://wihngo.com/payment/success",
    "CancelUrl": "https://wihngo.com/payment/cancel"
  }
}
```

**Setup Instructions:**
1. Create a PayPal Business account at https://developer.paypal.com
2. Create a REST API app to get ClientId and ClientSecret
3. Create a webhook for payment events at https://developer.paypal.com/dashboard/webhooks
4. Subscribe to these events:
   - `PAYMENT.CAPTURE.COMPLETED`
   - `PAYMENT.CAPTURE.DENIED`
   - `PAYMENT.CAPTURE.DECLINED`
   - `PAYMENT.CAPTURE.REFUNDED`
5. Set webhook URL to: `https://your-domain.com/api/v1/webhooks/paypal`

### Solana Configuration
```json
{
  "Solana": {
    "RpcUrl": "https://api.mainnet-beta.solana.com",
    "MerchantWalletAddress": "your-solana-wallet-address",
    "ConfirmationBlocks": 32,
    "PollingIntervalSeconds": 10
  }
}
```

**Setup Instructions:**
1. Generate a Solana wallet for your merchant account
2. Use a reliable RPC provider (Helius, QuickNode, or Alchemy recommended)
3. Configure supported tokens in the database (see below)

### Base Configuration
```json
{
  "Base": {
    "RpcUrl": "https://mainnet.base.org",
    "MerchantWalletAddress": "0xYourEthereumAddress",
    "PaymentReceiverContract": "",
    "ConfirmationBlocks": 12,
    "PollingIntervalSeconds": 15
  }
}
```

**Setup Instructions:**
1. Generate an Ethereum-compatible wallet for Base network
2. Use a reliable RPC provider (Alchemy, Infura, or QuickNode)
3. Optionally deploy a PaymentReceiver contract for better tracking

## Database Setup

### Run Migrations
```bash
dotnet ef database update
```

### Configure Supported Tokens

The system uses the `supported_tokens` table to define which tokens are accepted. The migration seeds default tokens, but you can add more:

```sql
INSERT INTO supported_tokens (id, token_symbol, chain, mint_address, merchant_receiving_address, decimals, is_active, tolerance_percent, created_at)
VALUES 
  (gen_random_uuid(), 'USDC', 'solana', 'EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v', 'YourSolanaMerchantAddress', 6, true, 0.5, NOW()),
  (gen_random_uuid(), 'EURC', 'solana', 'HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr', 'YourSolanaMerchantAddress', 6, true, 0.5, NOW()),
  (gen_random_uuid(), 'USDC', 'base', '0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913', '0xYourBaseMerchantAddress', 6, true, 0.5, NOW()),
  (gen_random_uuid(), 'EURC', 'base', '0x60a3E35Cc302bFA44Cb288Bc5a4F316Fdb1adb42', '0xYourBaseMerchantAddress', 6, true, 0.5, NOW());
```

**Token Addresses:**
- **Solana USDC:** `EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v`
- **Solana EURC:** `HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr`
- **Base USDC:** `0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913`
- **Base EURC:** `0x60a3E35Cc302bFA44Cb288Bc5a4F316Fdb1adb42`

## API Endpoints

### Invoice Management

#### Create Invoice
```http
POST /api/v1/invoices
Authorization: Bearer {token}
Content-Type: application/json

{
  "birdId": "uuid",
  "amountFiat": 10.00,
  "fiatCurrency": "USD",
  "preferredPaymentMethods": ["paypal", "solana", "base"],
  "metadata": {
    "note": "Premium subscription"
  }
}
```

#### Get Invoice
```http
GET /api/v1/invoices/{invoiceId}
Authorization: Bearer {token}
```

#### Download Invoice PDF
```http
GET /api/v1/invoices/{invoiceId}/download
Authorization: Bearer {token}
```

#### List User Invoices
```http
GET /api/v1/invoices?page=1&pageSize=20
Authorization: Bearer {token}
```

#### Create Refund Request
```http
POST /api/v1/invoices/{invoiceId}/refund
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 10.00,
  "currency": "USD",
  "reason": "Customer requested refund"
}
```

### Payment Submission

#### Submit Payment (from mobile)
```http
POST /api/v1/payments/submit
Authorization: Bearer {token}
Content-Type: application/json

{
  "invoiceId": "uuid",
  "txHash": "transaction-hash",
  "paymentMethod": "solana",
  "payerWalletAddress": "wallet-address",
  "token": "USDC",
  "chain": "solana",
  "amountCrypto": 10.000000
}
```

#### Get Payment Status
```http
GET /api/v1/payments/{invoiceId}/status
Authorization: Bearer {token}
```

### Webhooks

#### PayPal Webhook
```http
POST /api/v1/webhooks/paypal
Content-Type: application/json

# PayPal will send webhook events here
# Signature verification is automatic
```

### Push Notifications

#### Register Push Token
```http
POST /api/v1/users/{userId}/push-token
Authorization: Bearer {token}
Content-Type: application/json

{
  "pushToken": "ExponentPushToken[xxx]",
  "deviceType": "ios",
  "deviceName": "iPhone 14"
}
```

## Payment Lifecycle

1. **CREATED** - Invoice created with payment instructions
2. **AWAITING_PAYMENT** - Waiting for payment confirmation
3. **ONCHAIN_CONFIRMING** - Transaction detected, awaiting confirmations
4. **CONFIRMED** - Payment confirmed on-chain or by PayPal
5. **INVOICE_ISSUED** - Invoice number assigned, PDF generated, receipt emailed
6. **COMPLETED** - Full lifecycle complete

### Error States
- **FAILED** - Payment failed
- **CANCELED** - Invoice canceled
- **REFUNDED** - Payment refunded
- **EXPIRED** - Invoice expired without payment

## Background Jobs

### Blockchain Listeners
- **SolanaListenerService** - Monitors SPL token transfers
- **EvmListenerService** - Monitors ERC-20 transfers on Base

### Scheduled Jobs (Hangfire)
- **ReconciliationJob** - Daily at 3 AM UTC
  - Checks for stuck invoices
  - Detects orphaned payments
  - Reports anomalies to admin email

## Testing

### Local Development
```bash
# Start PostgreSQL
docker run -d --name wihngo-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=wihngo \
  -p 5432:5432 \
  postgres:16

# Run migrations
dotnet ef database update

# Start application
dotnet run
```

### Test Endpoints
- Health check: `GET http://localhost:5000/test`
- Hangfire dashboard: `http://localhost:5000/hangfire`

### PayPal Sandbox Testing
1. Use sandbox credentials in configuration
2. Create test buyer/seller accounts at https://developer.paypal.com/dashboard/accounts
3. Use sandbox credit cards for testing

### Crypto Testing
1. Use testnet/devnet for Solana (devnet RPC URL)
2. Use Sepolia or Base Goerli for testing Base transactions
3. Get test tokens from faucets

## Security Considerations

1. **PayPal Webhook Verification** - All webhooks are signature-verified
2. **Idempotency** - Duplicate webhooks/transactions are handled via unique constraints
3. **Transaction Deduplication** - `tx_hash` unique constraint prevents double-processing
4. **Atomic Invoice Numbering** - PostgreSQL sequence ensures no duplicate invoice numbers
5. **User Authorization** - All endpoints verify ownership before returning data

## Monitoring & Alerts

### Logs to Monitor
- Invoice generation failures
- Payment confirmation delays
- Blockchain listener errors
- Refund processing failures
- Reconciliation anomalies

### Metrics to Track
- Invoice creation rate
- Payment success rate by method
- Average time to confirmation
- Refund rate
- Revenue by payment method

## Production Checklist

- [ ] Configure production database with connection pooling
- [ ] Set up S3 bucket for invoice PDF storage
- [ ] Configure production SMTP credentials
- [ ] Switch PayPal to "live" mode
- [ ] Use mainnet RPC URLs for Solana and Base
- [ ] Set up monitoring/alerting (Sentry, Datadog, etc.)
- [ ] Configure proper JWT secret (min 32 chars)
- [ ] Set HTTPS requirement in JWT config
- [ ] Implement proper admin authentication for Hangfire dashboard
- [ ] Set up database backups
- [ ] Configure rate limiting on API endpoints
- [ ] Set up SSL certificates
- [ ] Review and update company legal information in invoice config

## Support

For issues or questions:
- Email: support@wihngo.com
- GitHub: https://github.com/fullstackragab/wihngo-api

## License

Proprietary - Wihngo Inc.
