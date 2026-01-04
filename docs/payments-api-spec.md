# Wihngo Payments API Specification

## Overview

The Wihngo Payments API provides a provider-agnostic payment system supporting USDC on Solana, with extensibility for Stripe and PayPal. The system handles bird support payments, P2P transfers, and on-chain deposits across multiple blockchains.

**Base URL:** `https://api.wihngo.com/api`

---

## Authentication

Most endpoints require JWT Bearer token authentication.

```
Authorization: Bearer <token>
```

Public endpoints are marked with `[Public]`.

---

## Payment Endpoints

### Get Payment Configuration

`[Public]` Returns Solana network configuration for payments.

```
GET /payments/config
```

**Response:**
```json
{
  "network": "mainnet-beta",
  "rpcUrl": "https://api.mainnet-beta.solana.com",
  "usdcMint": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
  "merchantWallet": "..."
}
```

---

### Create Payment Intent

`[Authenticated]` Creates a payment intent for bird support.

```
POST /payments/intents
```

**Request:**
```json
{
  "birdId": "uuid",
  "amountCents": 1000,
  "wihngoAmountCents": 100
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `birdId` | uuid | Yes | ID of the bird to support |
| `amountCents` | int | Yes | Amount in USD cents (min: 1) |
| `wihngoAmountCents` | int | No | Optional platform support amount in cents |

**Response:**
```json
{
  "paymentId": "uuid",
  "amountCents": 1000,
  "currency": "USD",
  "destinationWallet": "...",
  "tokenMint": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
  "expiresAt": "2024-01-15T12:00:00Z",
  "returnUrl": "https://wihngo.com/payment/success?id=..."
}
```

---

### Confirm Payment

`[Authenticated]` Confirms a payment with the on-chain transaction hash.

```
POST /payments/confirm
```

**Request:**
```json
{
  "paymentId": "uuid",
  "txHash": "5abc123..."
}
```

**Response:**
```json
{
  "paymentId": "uuid",
  "status": "Confirmed",
  "isSuccess": true,
  "failureReason": null,
  "confirmedAt": "2024-01-15T12:05:00Z"
}
```

**Possible Statuses:** `Pending`, `Confirmed`, `Failed`, `Expired`

**Failure Reasons:**
- `TRANSACTION_NOT_FOUND` - Transaction not found on-chain
- `INVALID_DESTINATION` - Wrong destination wallet
- `AMOUNT_MISMATCH` - Amount doesn't match intent
- `INVALID_TOKEN` - Wrong token (not USDC)
- `MEMO_MISMATCH` - Missing or invalid memo (replay protection)

---

### Get Payment Status

`[Public]` Returns detailed payment status.

```
GET /payments/{id}
```

**Response:**
```json
{
  "paymentId": "uuid",
  "status": "Confirmed",
  "purpose": "BirdSupport",
  "birdId": "uuid",
  "amountCents": 1000,
  "currency": "USD",
  "provider": "UsdcSolana",
  "createdAt": "2024-01-15T12:00:00Z",
  "confirmedAt": "2024-01-15T12:05:00Z"
}
```

---

### Get Intent Status (Simplified)

`[Public]` Returns simplified intent status for UI polling.

```
GET /payments/intents/{id}/status
```

**Response:**
```json
{
  "paymentId": "uuid",
  "status": "Pending",
  "birdId": "uuid",
  "birdName": "Blue Jay",
  "amountCents": 1000,
  "message": "Waiting for payment...",
  "claimRequired": false,
  "claimUrl": null
}
```

---

### Create Manual Payment (Anonymous)

`[Public]` Creates a payment intent for mobile/anonymous users using HD-derived addresses.

```
POST /payments/manual
```

**Request:**
```json
{
  "birdId": "uuid",
  "amountCents": 1000,
  "email": "user@example.com",
  "wihngoAmountCents": 100
}
```

**Response:**
```json
{
  "paymentId": "uuid",
  "amountCents": 1000,
  "currency": "USD",
  "network": "Solana",
  "destinationAddress": "...",
  "expiresAt": "2024-01-15T12:30:00Z",
  "claimUrl": "https://wihngo.com/claim?id=...",
  "message": "Send exactly 10.00 USDC to the address above"
}
```

---

### Claim Payment

`[Authenticated]` Claims a confirmed anonymous payment.

```
POST /payments/{id}/claim
```

**Response:**
```json
{
  "success": true,
  "paymentId": "uuid",
  "birdId": "uuid",
  "amountCents": 1000
}
```

---

## P2P Transfer Endpoints

### Preflight Transfer

`[Authenticated]` Validates a P2P transfer before creation.

```
POST /support/transfers/preflight
```

**Request:**
```json
{
  "recipientUserId": "uuid",
  "amountUsdc": 10.00,
  "memo": "Thanks for the support!"
}
```

**Response:**
```json
{
  "valid": true,
  "recipientName": "John Doe",
  "recipientWallet": "...",
  "estimatedFeeUsdc": 0.01,
  "gasSponsored": true,
  "warnings": []
}
```

**Validation Errors:**
- `RECIPIENT_NOT_FOUND` - User doesn't exist
- `NO_LINKED_WALLET` - Recipient has no linked wallet
- `AMOUNT_TOO_LOW` - Below minimum ($0.01)
- `AMOUNT_TOO_HIGH` - Above maximum ($10,000)
- `SELF_TRANSFER` - Cannot send to yourself

---

### Create Transfer Intent

`[Authenticated]` Creates a P2P transfer intent with unsigned transaction.

```
POST /support/transfers
```

**Request:**
```json
{
  "recipientUserId": "uuid",
  "amountUsdc": 10.00,
  "memo": "Thanks!",
  "idempotencyKey": "unique-key-123"
}
```

**Response:**
```json
{
  "paymentId": "uuid",
  "serializedTransaction": "base64...",
  "expiresAt": "2024-01-15T12:15:00Z",
  "feeUsdc": 0.01,
  "totalUsdc": 10.01,
  "gasSponsored": true
}
```

---

### Submit Signed Transaction

`[Authenticated]` Submits a signed transaction for broadcast.

```
POST /support/transfers/{id}/submit
```

**Request:**
```json
{
  "signedTransaction": "base64..."
}
```

**Response:**
```json
{
  "paymentId": "uuid",
  "status": "submitted",
  "solanaSignature": "5abc123...",
  "submittedAt": "2024-01-15T12:05:00Z"
}
```

---

### Get Transfer Status

`[Authenticated]` Returns transfer status and confirmation progress.

```
GET /support/transfers/{id}
```

**Response:**
```json
{
  "paymentId": "uuid",
  "status": "confirming",
  "amountUsdc": 10.00,
  "feeUsdc": 0.01,
  "recipientUserId": "uuid",
  "recipientName": "John Doe",
  "solanaSignature": "5abc123...",
  "confirmations": 15,
  "requiredConfirmations": 32,
  "createdAt": "2024-01-15T12:00:00Z",
  "submittedAt": "2024-01-15T12:05:00Z",
  "confirmedAt": null
}
```

**P2P Status Values:**
| Status | Description |
|--------|-------------|
| `pending` | Intent created, awaiting signature |
| `awaiting_signature` | Transaction built, user needs to sign |
| `submitted` | Transaction broadcast to network |
| `confirming` | Waiting for confirmations |
| `confirmed` | Required confirmations reached |
| `completed` | Transfer finalized |
| `failed` | Transaction failed |
| `expired` | Intent expired before submission |
| `cancelled` | User cancelled the transfer |
| `timeout` | Confirmation timeout |

---

### Cancel Transfer

`[Authenticated]` Cancels a pending transfer.

```
POST /support/transfers/{id}/cancel
```

**Response:**
```json
{
  "success": true,
  "paymentId": "uuid"
}
```

---

## On-Chain Deposit Endpoints

### Get Deposit History

`[Authenticated]` Returns paginated deposit history.

```
GET /deposits/onchain/history?page=1&pageSize=20
```

**Response:**
```json
{
  "items": [
    {
      "depositId": "uuid",
      "chain": "Solana",
      "token": "USDC",
      "amount": 100.00,
      "status": "Confirmed",
      "txHash": "...",
      "confirmations": 32,
      "createdAt": "2024-01-15T12:00:00Z",
      "confirmedAt": "2024-01-15T12:05:00Z"
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20
}
```

---

### Get Deposit Address

`[Authenticated]` Returns user's derived deposit address for a chain.

```
GET /deposits/onchain/address/{chain}
```

**Path Parameters:**
| Parameter | Values |
|-----------|--------|
| `chain` | `Solana`, `Ethereum`, `Polygon`, `Base`, `Stellar` |

**Response:**
```json
{
  "chain": "Solana",
  "address": "...",
  "derivationPath": "m/44'/501'/0'/0/123",
  "registeredAt": "2024-01-10T00:00:00Z"
}
```

---

### Register Deposit Address

`[Authenticated]` Registers a derived address for the user.

```
POST /deposits/onchain/address/register
```

**Request:**
```json
{
  "chain": "Solana"
}
```

**Response:**
```json
{
  "chain": "Solana",
  "address": "...",
  "registeredAt": "2024-01-15T12:00:00Z"
}
```

---

### Get Supported Tokens

`[Public]` Returns supported token configurations.

```
GET /deposits/onchain/tokens
```

**Response:**
```json
{
  "tokens": [
    {
      "token": "USDC",
      "chains": [
        {
          "chain": "Solana",
          "contractAddress": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
          "decimals": 6,
          "minDeposit": 1.00,
          "confirmations": 32
        },
        {
          "chain": "Ethereum",
          "contractAddress": "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48",
          "decimals": 6,
          "minDeposit": 10.00,
          "confirmations": 12
        }
      ]
    },
    {
      "token": "EURC",
      "chains": [...]
    }
  ]
}
```

---

### Get Pending Deposits

`[Authenticated]` Returns deposits awaiting confirmation.

```
GET /deposits/onchain/pending
```

**Response:**
```json
{
  "deposits": [
    {
      "depositId": "uuid",
      "chain": "Ethereum",
      "token": "USDC",
      "amount": 50.00,
      "confirmations": 5,
      "requiredConfirmations": 12,
      "txHash": "0x...",
      "createdAt": "2024-01-15T12:00:00Z"
    }
  ]
}
```

---

## Wallet Endpoints

### Link Wallet

`[Authenticated]` Links a Phantom wallet to the user's account.

```
POST /wallets/link
```

**Request:**
```json
{
  "publicKey": "...",
  "signature": "...",
  "message": "Link wallet to Wihngo: <nonce>"
}
```

**Response:**
```json
{
  "walletId": "uuid",
  "publicKey": "...",
  "linkedAt": "2024-01-15T12:00:00Z"
}
```

---

### Get Linked Wallets

`[Authenticated]` Returns all wallets linked to the account.

```
GET /wallets
```

**Response:**
```json
{
  "wallets": [
    {
      "walletId": "uuid",
      "publicKey": "...",
      "isPrimary": true,
      "linkedAt": "2024-01-15T12:00:00Z"
    }
  ]
}
```

---

## Payout Endpoints

### Get Payout Balance

`[Authenticated]` Returns available payout balance and earnings summary.

```
GET /payouts/balance
```

**Response:**
```json
{
  "availableBalanceCents": 5000,
  "pendingBalanceCents": 1000,
  "lifetimeEarningsCents": 25000,
  "currency": "USD"
}
```

---

### Get Payout Methods

`[Authenticated]` Returns configured payout methods.

```
GET /payouts/methods
```

**Response:**
```json
{
  "methods": [
    {
      "methodId": "uuid",
      "type": "UsdcSolana",
      "walletAddress": "...",
      "isDefault": true,
      "createdAt": "2024-01-10T00:00:00Z"
    }
  ]
}
```

---

## Data Models

### Payment Status

| Status | Description |
|--------|-------------|
| `Pending` | Payment created, awaiting transaction |
| `Confirmed` | Transaction verified on-chain |
| `Failed` | Transaction verification failed |
| `Expired` | Payment intent expired |
| `SweepEligible` | 14 days post-confirmation, eligible for sweep |
| `Swept` | Funds swept to treasury |

### Payment Purpose

| Purpose | Description |
|---------|-------------|
| `BirdSupport` | User supporting a bird |
| `Payout` | Platform paying out to bird owner |
| `Refund` | Refund to user |

### Payment Provider

| Provider | Description |
|----------|-------------|
| `UsdcSolana` | USDC on Solana via Phantom |
| `ManualUsdcSolana` | USDC on Solana via HD-derived address |
| `Stripe` | Stripe payments (planned) |
| `PayPal` | PayPal payments (planned) |

---

## Security

### Replay Protection

All USDC on Solana payments require a memo field containing the payment intent ID:

```
Memo format: wihngo:{paymentId}
```

Transactions without a valid memo are rejected.

### Idempotency

P2P transfers support idempotency keys to prevent duplicate submissions:

```json
{
  "idempotencyKey": "unique-client-generated-key"
}
```

### Amount Validation

- Exact amount matching required
- Minimum: $0.01 USD
- Maximum: $10,000 USD (P2P)

### Gas Sponsorship

P2P transfers support gas sponsorship for users with low SOL balance:

- Threshold: < 0.00001 SOL
- Flat fee: 0.01 USDC
- Sponsor wallet signs for gas

---

## Error Responses

All errors follow this format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable message",
    "details": {}
  }
}
```

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `UNAUTHORIZED` | 401 | Missing or invalid token |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `NOT_FOUND` | 404 | Resource not found |
| `VALIDATION_ERROR` | 400 | Invalid request data |
| `PAYMENT_EXPIRED` | 400 | Payment intent expired |
| `PAYMENT_ALREADY_CONFIRMED` | 409 | Payment already processed |
| `INSUFFICIENT_BALANCE` | 400 | Wallet has insufficient funds |
| `WALLET_NOT_LINKED` | 400 | User has no linked wallet |
| `RATE_LIMITED` | 429 | Too many requests |

---

## Rate Limits

| Endpoint | Limit |
|----------|-------|
| Payment creation | 10/minute |
| Payment confirmation | 30/minute |
| Status checks | 60/minute |
| P2P transfers | 5/minute |

---

## Webhooks (Planned)

Future webhook support for payment events:

- `payment.confirmed`
- `payment.failed`
- `payment.expired`
- `transfer.completed`
- `deposit.confirmed`
