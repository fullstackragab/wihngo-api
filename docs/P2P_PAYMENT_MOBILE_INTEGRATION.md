# P2P USDC Payment - Mobile Integration Guide

## Overview

This guide covers integration with the Wihngo P2P USDC payment system. The backend is configured for **Solana Devnet** for testing.

## Environment Configuration

### Current Configuration (Devnet)
```
Network: Solana Devnet
USDC Mint: 4zMMC9srt5Ri5X14GAgXhaHii3GnPAEERYPJgZJDncDU
RPC URL: https://api.devnet.solana.com
Sponsor Wallet: 6GXVP4mTMNqihNARivweYwc6rtuih1ivoJN7bcAEWWCV
```

### Production Configuration (Mainnet - when ready)
```
Network: Solana Mainnet
USDC Mint: EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v
RPC URL: https://api.mainnet-beta.solana.com
```

---

## API Endpoints

Base URL: `https://your-api-domain.com/api`

### 1. Link Wallet
Links a Phantom wallet to the user's account.

```
POST /api/wallets/link
Authorization: Bearer {jwt_token}

Request:
{
  "publicKey": "UserWalletPublicKey44chars",
  "signature": "base58_encoded_signature",
  "message": "Sign this message to link your wallet to Wihngo: {timestamp}"
}

Response (200):
{
  "walletId": "uuid",
  "publicKey": "UserWalletPublicKey44chars",
  "isPrimary": true,
  "linkedAt": "2024-12-26T00:00:00Z"
}
```

### 2. Get User Balance
Returns the user's USDC balance and gas availability.

```
GET /api/wallets/balance
Authorization: Bearer {jwt_token}

Response (200):
{
  "balanceUsdc": 100.50,
  "availableUsdc": 100.50,
  "pendingUsdc": 0.00,
  "hasGas": true,
  "walletAddress": "UserWalletPublicKey44chars"
}
```

### 3. Preflight Payment
Validates a payment before creating an intent. Call this first to show accurate fee information.

```
POST /api/payments/preflight
Authorization: Bearer {jwt_token}

Request:
{
  "recipientId": "recipient_user_id_or_username",
  "amount": 25.00
}

Response (200):
{
  "valid": true,
  "recipientName": "John Doe",
  "recipientId": "uuid",
  "amount": 25.00,
  "networkFee": 0.01,
  "totalAmount": 25.01,
  "gasSponsored": true,
  "errorMessage": null,
  "errorCode": null
}

Error Response:
{
  "valid": false,
  "errorMessage": "Recipient not found",
  "errorCode": "RECIPIENT_NOT_FOUND"
}
```

**Error Codes:**
- `RECIPIENT_NOT_FOUND` - User doesn't exist
- `SELF_PAYMENT` - Cannot pay yourself
- `INVALID_AMOUNT` - Amount out of range ($0.01 - $10,000)
- `INSUFFICIENT_BALANCE` - Not enough USDC
- `NO_WALLET` - Sender has no linked wallet

### 4. Create Payment Intent
Creates a payment record and returns an unsigned transaction for Phantom to sign.

```
POST /api/payments/intents
Authorization: Bearer {jwt_token}

Request:
{
  "recipientUserId": "uuid",
  "amountUsdc": 25.00,
  "memo": "Thanks for lunch!"  // optional, max 255 chars
}

Response (200):
{
  "paymentId": "uuid",
  "serializedTransaction": "base64_encoded_unsigned_transaction",
  "amountUsdc": 25.00,
  "feeUsdc": 0.01,
  "totalUsdc": 25.01,
  "gasSponsored": true,
  "expiresAt": "2024-12-26T00:15:00Z",
  "recipientName": "John Doe",
  "recipientWallet": "RecipientWalletPublicKey"
}
```

### 5. Submit Signed Transaction
After the user signs in Phantom, submit the signed transaction.

```
POST /api/payments/submit
Authorization: Bearer {jwt_token}

Request:
{
  "paymentId": "uuid",
  "signedTransaction": "base64_encoded_signed_transaction"
}

Response (200):
{
  "paymentId": "uuid",
  "status": "Submitted",
  "solanaSignature": "transaction_signature_88chars",
  "errorMessage": null
}

Error Response:
{
  "paymentId": "uuid",
  "status": "Failed",
  "errorMessage": "Transaction expired"
}
```

### 6. Get Payment Status
Poll this endpoint to check payment confirmation status.

```
GET /api/payments/intents/{paymentId}
Authorization: Bearer {jwt_token}

Response (200):
{
  "paymentId": "uuid",
  "status": "Confirmed",  // Pending, AwaitingSignature, Submitted, Confirming, Confirmed, Completed, Failed, Expired, Cancelled
  "amountUsdc": 25.00,
  "feeUsdc": 0.01,
  "solanaSignature": "transaction_signature_88chars",
  "confirmations": 1,
  "requiredConfirmations": 1,
  "createdAt": "2024-12-26T00:00:00Z",
  "confirmedAt": "2024-12-26T00:01:00Z"
}
```

**Payment Statuses:**
| Status | Description |
|--------|-------------|
| `Pending` | Payment created, waiting for signature |
| `AwaitingSignature` | Transaction built, waiting for user to sign |
| `Submitted` | Transaction submitted to Solana |
| `Confirming` | Transaction seen, waiting for confirmations |
| `Confirmed` | Required confirmations reached |
| `Completed` | Payment fully processed, balances updated |
| `Failed` | Transaction failed on-chain |
| `Expired` | Payment intent expired (15 minutes) |
| `Cancelled` | User cancelled the payment |

### 7. Cancel Payment
Cancel a pending payment (before submission).

```
POST /api/payments/{paymentId}/cancel
Authorization: Bearer {jwt_token}

Response (200):
{
  "success": true,
  "message": "Payment cancelled"
}
```

### 8. Get Payment History
Returns paginated list of user's payments (sent and received).

```
GET /api/payments?page=1&pageSize=20
Authorization: Bearer {jwt_token}

Response (200):
{
  "items": [
    {
      "paymentId": "uuid",
      "status": "Completed",
      "amountUsdc": 25.00,
      "memo": "Thanks for lunch!",
      "createdAt": "2024-12-26T00:00:00Z",
      "isSender": true,
      "otherParty": {
        "userId": "uuid",
        "name": "John Doe",
        "profileImage": "https://..."
      }
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20
}
```

---

## Payment Flow

```
1. User enters amount and recipient
   ↓
2. POST /api/payments/preflight
   → Validate payment, show fee breakdown
   ↓
3. User confirms payment
   ↓
4. POST /api/payments/intents
   → Create payment, get unsigned transaction
   ↓
5. Open Phantom with serializedTransaction
   → User signs transaction
   ↓
6. POST /api/payments/submit
   → Submit signed transaction to Solana
   ↓
7. Poll GET /api/payments/intents/{id}
   → Wait for Confirmed/Completed status
   ↓
8. Show success screen
```

---

## Phantom Integration (React Native)

### Opening Phantom for Transaction Signing

```typescript
import * as Linking from 'expo-linking';
import * as WebBrowser from 'expo-web-browser';
import bs58 from 'bs58';

const PHANTOM_DEEP_LINK = 'phantom://';

async function signAndSendTransaction(serializedTransaction: string, paymentId: string) {
  // Decode the base64 transaction
  const transactionBytes = Buffer.from(serializedTransaction, 'base64');
  const encodedTransaction = bs58.encode(transactionBytes);

  // Build redirect URL for your app
  const redirectUrl = Linking.createURL(`send/pay?paymentId=${paymentId}`);

  // Build Phantom deep link
  const params = new URLSearchParams({
    dapp_encryption_public_key: YOUR_DAPP_PUBLIC_KEY, // Your app's X25519 public key
    cluster: 'devnet', // or 'mainnet-beta' for production
    redirect_link: redirectUrl,
    transaction: encodedTransaction,
  });

  const url = `${PHANTOM_DEEP_LINK}v1/signAndSendTransaction?${params.toString()}`;

  // Open Phantom
  await Linking.openURL(url);
}
```

### Handling Phantom Response

When Phantom redirects back to your app, extract the signature:

```typescript
// In your deep link handler
function handlePhantomResponse(url: string) {
  const params = new URLSearchParams(url.split('?')[1]);
  const signature = params.get('signature');
  const errorCode = params.get('errorCode');

  if (errorCode) {
    // User rejected or error occurred
    console.error('Phantom error:', errorCode);
    return;
  }

  if (signature) {
    // Transaction was signed and sent
    // The signature is the Solana transaction signature
    // Now poll the backend for confirmation
    pollPaymentStatus(paymentId);
  }
}
```

---

## Gas Sponsorship

The platform automatically sponsors gas (SOL) fees for users who have insufficient SOL balance.

- **Threshold**: If user has < 0.00001 SOL, gas is sponsored
- **Fee**: Flat $0.01 USDC added to transaction
- **UX**: Show as "Network fee: $0.01 (processed automatically)"

The `gasSponsored` field in API responses indicates whether gas sponsorship is applied.

---

## Testing on Devnet

### Get Devnet USDC

1. Get devnet SOL from [Solana Faucet](https://faucet.solana.com/)
2. Get devnet USDC by:
   - Using a devnet USDC faucet
   - Or creating a test token account

### Test Wallet Setup

1. Install Phantom wallet
2. Switch to Devnet in settings
3. Create/import a test wallet
4. Link wallet to your Wihngo account via `/api/wallets/link`

---

## Error Handling

### HTTP Status Codes

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Invalid/expired JWT |
| 404 | Not Found - Payment/wallet not found |
| 500 | Server Error |

### Common Error Responses

```json
{
  "error": "Error message here"
}
```

Or for preflight/submit:
```json
{
  "valid": false,
  "errorMessage": "Description",
  "errorCode": "ERROR_CODE"
}
```

---

## Polling Strategy

For payment confirmation, use exponential backoff:

```typescript
async function pollPaymentStatus(paymentId: string) {
  const maxAttempts = 30;
  let attempt = 0;
  let delay = 1000; // Start at 1 second

  while (attempt < maxAttempts) {
    const response = await api.get(`/payments/intents/${paymentId}`);
    const status = response.data.status;

    if (['Completed', 'Confirmed'].includes(status)) {
      return { success: true, data: response.data };
    }

    if (['Failed', 'Expired', 'Cancelled'].includes(status)) {
      return { success: false, data: response.data };
    }

    // Still processing, wait and retry
    await new Promise(resolve => setTimeout(resolve, delay));
    delay = Math.min(delay * 1.5, 5000); // Max 5 seconds
    attempt++;
  }

  return { success: false, error: 'Timeout' };
}
```

---

## UX Guidelines

1. **Never show crypto jargon** to users
   - Say "Network fee" not "gas fee"
   - Say "USD" not "USDC"
   - Don't show SOL amounts

2. **Amount display**
   - Always show amounts in USD format: `$25.00`
   - Include fee in total: `$25.01 total ($25.00 + $0.01 fee)`

3. **Status messages**
   - "Sending..." → "Confirming..." → "Sent!"
   - Never say "Waiting for blockchain confirmations"

4. **Error messages**
   - Use friendly language
   - "Payment couldn't be completed" not "Transaction failed"

---

## Contact

For backend issues or API questions, contact the backend team.

**Backend Status:**
- Devnet: Ready for testing
- Mainnet: Pending production deployment

**Last Updated:** December 26, 2024
