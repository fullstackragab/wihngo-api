# ?? Crypto Payment API Documentation for Frontend

## Base URL
```
Development: https://horsier-maliah-semilyrical.ngrok-free.dev/api/
Production: https://wihngo-api.onrender.com/api/
```

## Authentication
All endpoints except rates and wallet info require JWT Bearer token:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## API Endpoints

### 1. Create Payment Request

**POST** `/api/payments/crypto/create`

Creates a new crypto payment request.

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "birdId": "optional-guid",
  "amountUsd": 4.99,
  "currency": "USDT",
  "network": "tron",
  "purpose": "premium_subscription",
  "plan": "monthly"
}
```

**Field Descriptions:**
- `birdId` (optional): UUID of the bird for premium subscription
- `amountUsd` (required): Amount in USD (min: 5, max: 100000)
- `currency` (required): One of: BTC, ETH, USDT, USDC, BNB, SOL, DOGE
- `network` (required): One of: bitcoin, ethereum, tron, binance-smart-chain, polygon, solana
- `purpose` (required): One of: premium_subscription, donation, purchase
- `plan` (optional): For subscriptions: monthly, yearly, lifetime

**Success Response (201 Created):**
```json
{
  "paymentRequest": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "userId": "user-guid",
    "birdId": "bird-guid-or-null",
    "amountUsd": 4.99,
    "amountCrypto": 4.95,
    "currency": "USDT",
    "network": "tron",
    "exchangeRate": 1.008,
    "walletAddress": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
    "userWalletAddress": null,
    "qrCodeData": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
    "paymentUri": "tron:TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA?amount=4.95",
    "transactionHash": null,
    "confirmations": 0,
    "requiredConfirmations": 19,
    "status": "pending",
    "purpose": "premium_subscription",
    "plan": "monthly",
    "expiresAt": "2025-12-11T12:30:00Z",
    "confirmedAt": null,
    "completedAt": null,
    "createdAt": "2025-12-11T12:00:00Z",
    "updatedAt": "2025-12-11T12:00:00Z"
  },
  "message": "Payment request created successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Validation error
```json
{
  "error": "Invalid currency"
}
```
- `401 Unauthorized`: Missing/invalid token
- `500 Internal Server Error`: Server error

---

### 2. Get Payment Status

**GET** `/api/payments/crypto/{paymentId}`

Get current payment status. Use for polling every 5 seconds.

**Headers:**
```
Authorization: Bearer {token}
```

**Success Response (200 OK):**
```json
{
  "id": "payment-guid",
  "userId": "user-guid",
  "birdId": "bird-guid-or-null",
  "amountUsd": 4.99,
  "amountCrypto": 4.95,
  "currency": "USDT",
  "network": "tron",
  "exchangeRate": 1.008,
  "walletAddress": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
  "userWalletAddress": "user-wallet-if-detected",
  "qrCodeData": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
  "paymentUri": "tron:TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA?amount=4.95",
  "transactionHash": "abc123...",
  "confirmations": 15,
  "requiredConfirmations": 19,
  "status": "confirming",
  "purpose": "premium_subscription",
  "plan": "monthly",
  "expiresAt": "2025-12-11T12:30:00Z",
  "confirmedAt": null,
  "completedAt": null,
  "createdAt": "2025-12-11T12:00:00Z",
  "updatedAt": "2025-12-11T12:15:00Z"
}
```

**Payment Status Values:**
- `pending`: Waiting for transaction
- `confirming`: Transaction detected, waiting for confirmations
- `confirmed`: Sufficient confirmations received
- `completed`: Payment processed successfully
- `expired`: Payment window expired
- `cancelled`: User cancelled
- `failed`: Transaction failed

**Error Responses:**
- `404 Not Found`: Payment not found or user doesn't own it
- `401 Unauthorized`: Missing/invalid token

---

### 3. Verify Payment

**POST** `/api/payments/crypto/{paymentId}/verify`

Manually verify a payment with transaction hash.

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "transactionHash": "0xabc123...",
  "userWalletAddress": "optional-user-wallet-address"
}
```

**Success Response (200 OK):**
```json
{
  "id": "payment-guid",
  "status": "confirming",
  "transactionHash": "0xabc123...",
  "confirmations": 1,
  "message": "Transaction found and being verified",
  // ... full payment object
}
```

**Error Responses:**
- `400 Bad Request`: Transaction not found or invalid
```json
{
  "error": "Transaction not found on blockchain"
}
```
- `404 Not Found`: Payment not found

---

### 4. Get Payment History

**GET** `/api/payments/crypto/history?page=1&pageSize=20`

Get user's payment history with pagination.

**Headers:**
```
Authorization: Bearer {token}
```

**Query Parameters:**
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20)

**Success Response (200 OK):**
```json
{
  "payments": [
    {
      "id": "payment-guid",
      "amountUsd": 4.99,
      "amountCrypto": 4.95,
      "currency": "USDT",
      "network": "tron",
      "status": "completed",
      "purpose": "premium_subscription",
      "plan": "monthly",
      "createdAt": "2025-12-11T12:00:00Z",
      "completedAt": "2025-12-11T12:10:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 15
}
```

---

### 5. Get All Exchange Rates

**GET** `/api/payments/crypto/rates`

Get current exchange rates for all cryptocurrencies. No authentication required.

**Success Response (200 OK):**
```json
[
  {
    "currency": "BTC",
    "usdRate": 43250.50,
    "lastUpdated": "2025-12-11T12:00:00Z",
    "source": "coingecko"
  },
  {
    "currency": "ETH",
    "usdRate": 2250.75,
    "lastUpdated": "2025-12-11T12:00:00Z",
    "source": "coingecko"
  },
  {
    "currency": "USDT",
    "usdRate": 1.008,
    "lastUpdated": "2025-12-11T12:00:00Z",
    "source": "coingecko"
  }
]
```

**Note:** Rates are updated every 5 minutes automatically.

---

### 6. Get Specific Exchange Rate

**GET** `/api/payments/crypto/rates/{currency}`

Get exchange rate for specific cryptocurrency. No authentication required.

**Example:** `/api/payments/crypto/rates/USDT`

**Success Response (200 OK):**
```json
{
  "currency": "USDT",
  "usdRate": 1.008,
  "lastUpdated": "2025-12-11T12:00:00Z",
  "source": "coingecko"
}
```

**Error Response:**
- `404 Not Found`: Currency not supported
```json
{
  "error": "Currency not found"
}
```

---

### 7. Get Platform Wallet

**GET** `/api/payments/crypto/wallet/{currency}/{network}`

Get platform wallet info for specific currency/network. No authentication required.

**Example:** `/api/payments/crypto/wallet/USDT/tron`

**Success Response (200 OK):**
```json
{
  "currency": "USDT",
  "network": "tron",
  "address": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
  "qrCode": "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
  "isActive": true
}
```

**Error Response:**
- `404 Not Found`: No wallet configured for this currency/network

---

### 8. Cancel Payment

**POST** `/api/payments/crypto/{paymentId}/cancel`

Cancel a pending payment.

**Headers:**
```
Authorization: Bearer {token}
```

**Success Response (200 OK):**
```json
{
  "id": "payment-guid",
  "status": "cancelled",
  "message": "Payment cancelled successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Cannot cancel (already processed/expired)
```json
{
  "error": "Cannot cancel payment with status 'completed'"
}
```
- `404 Not Found`: Payment not found

---

## Frontend Implementation Guide

### 1. Payment Flow

```javascript
// Step 1: Create payment
const createPayment = async () => {
  const response = await fetch('/api/payments/crypto/create', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      amountUsd: 4.99,
      currency: 'USDT',
      network: 'tron',
      purpose: 'premium_subscription',
      plan: 'monthly'
    })
  });
  
  const data = await response.json();
  return data.paymentRequest;
};

// Step 2: Display QR code and wallet address
const displayPayment = (payment) => {
  // Show QR code using payment.qrCodeData
  // Display payment.walletAddress for copying
  // Show countdown timer using payment.expiresAt
  // Start polling for status
  startPolling(payment.id);
};

// Step 3: Poll for payment status
const pollPaymentStatus = async (paymentId) => {
  const response = await fetch(`/api/payments/crypto/${paymentId}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const payment = await response.json();
  
  // Update UI based on status
  if (payment.status === 'completed') {
    showSuccess();
    stopPolling();
  } else if (payment.status === 'confirming') {
    showConfirmations(payment.confirmations, payment.requiredConfirmations);
  } else if (payment.status === 'expired') {
    showExpired();
    stopPolling();
  }
  
  return payment;
};

// Poll every 5 seconds
const startPolling = (paymentId) => {
  const interval = setInterval(async () => {
    const payment = await pollPaymentStatus(paymentId);
    
    if (['completed', 'expired', 'cancelled', 'failed'].includes(payment.status)) {
      clearInterval(interval);
    }
  }, 5000);
};
```

### 2. QR Code Generation

Use any QR code library (e.g., `qrcode` npm package):

```javascript
import QRCode from 'qrcode';

const generateQR = async (qrCodeData) => {
  const qrCodeUrl = await QRCode.toDataURL(qrCodeData);
  // Display qrCodeUrl in <img> tag
  return qrCodeUrl;
};
```

### 3. Countdown Timer

```javascript
const startCountdown = (expiresAt) => {
  const expirationTime = new Date(expiresAt).getTime();
  
  const interval = setInterval(() => {
    const now = new Date().getTime();
    const distance = expirationTime - now;
    
    if (distance < 0) {
      clearInterval(interval);
      showExpired();
      return;
    }
    
    const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((distance % (1000 * 60)) / 1000);
    
    updateTimerDisplay(`${minutes}:${seconds.toString().padStart(2, '0')}`);
  }, 1000);
};
```

### 4. Error Handling

```javascript
const handleApiError = (error, response) => {
  if (response.status === 401) {
    // Redirect to login
    redirectToLogin();
  } else if (response.status === 400) {
    // Show validation error
    showError(error.error);
  } else if (response.status === 404) {
    // Show not found error
    showError('Payment not found');
  } else {
    // Show generic error
    showError('Something went wrong. Please try again.');
  }
};
```

---

## Supported Currency/Network Combinations

| Currency | Networks |
|----------|----------|
| BTC | bitcoin |
| ETH | ethereum, binance-smart-chain, polygon |
| USDT | ethereum (ERC-20), tron (TRC-20), binance-smart-chain (BEP-20), polygon |
| USDC | ethereum (ERC-20), binance-smart-chain (BEP-20), polygon |
| BNB | binance-smart-chain |
| SOL | solana |
| DOGE | dogecoin |

**Recommended for MVP**: USDT on TRON (TRC-20) - lowest fees, fast confirmations

---

## Required Confirmations by Network

| Network | Confirmations |
|---------|--------------|
| Bitcoin | 2 |
| Ethereum | 12 |
| TRON | 19 |
| BSC | 15 |
| Polygon | 128 |
| Solana | 32 |

---

## Testing

### Test User Creation
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

### Test Payment Creation
```bash
curl -X POST http://localhost:5000/api/payments/crypto/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "amountUsd": 4.99,
    "currency": "USDT",
    "network": "tron",
    "purpose": "premium_subscription",
    "plan": "monthly"
  }'
```

---

## Common Issues & Solutions

### Issue: Payment not detected
**Solution**: User may have sent wrong amount or to wrong address. Use manual verification endpoint with transaction hash.

### Issue: Confirmations taking long
**Solution**: Show progress bar with current confirmations. Different networks have different confirmation times.

### Issue: Payment expired
**Solution**: Allow user to create new payment request. Do not extend expiration time.

### Issue: Wrong network selected
**Solution**: Validate currency/network combination before creation. Show clear network selection in UI.

---

## Best Practices

1. **Always poll for status** - Don't rely on user refresh
2. **Show clear instructions** - Display exact amount and address
3. **Implement copy button** - For wallet address
4. **Show countdown timer** - So user knows time remaining
5. **Handle expiration gracefully** - Allow creating new payment
6. **Display confirmations** - Show progress: "15/19 confirmations"
7. **Add manual verification** - Allow user to paste transaction hash
8. **Validate before sending** - Check currency/network compatibility
9. **Show transaction fee info** - So user knows total cost
10. **Handle all statuses** - pending, confirming, confirmed, completed, expired, cancelled, failed

---

## Rate Limits

Current implementation has no rate limits, but consider:
- Max 1 pending payment per user
- Minimum 1 minute between payment creations
- Maximum 10 payment history requests per minute

---

## Support

For API issues or questions:
- Check HTTP status codes and error messages
- Review network compatibility
- Verify JWT token is valid
- Check Hangfire dashboard for background job status

---

**API Version**: 1.0.0  
**Last Updated**: December 11, 2025  
**Documentation**: Complete
