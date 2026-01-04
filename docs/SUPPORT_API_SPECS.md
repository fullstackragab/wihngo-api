# Support API Specifications

**Base URL:** `/api/support`
**Auth:** All endpoints require `Authorization: Bearer <token>`

---

## Overview

There are two types of support:
1. **Bird Support** - Support a specific bird (funds go to bird owner + optional Wihngo tip)
2. **Wihngo Support** - Support the platform directly (no bird involved)

---

## 1. Support a Bird

### `POST /api/support/birds/preflight`

Preflight check before supporting a bird. Call this to verify balances and get recipient info.

**Request:**
```json
{
  "birdId": "uuid",
  "birdAmount": 5.00,
  "wihngoSupportAmount": 0.05
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `birdId` | uuid | Yes | The bird to support |
| `birdAmount` | decimal | Yes | Amount for bird owner ($0.01-$10,000) |
| `wihngoSupportAmount` | decimal | No | Optional platform tip (default $0.05, min $0.05 if > 0) |

**Response (200):**
```json
{
  "canSupport": true,
  "hasWallet": true,
  "usdcBalance": 100.00,
  "solBalance": 0.5,
  "birdAmount": 5.00,
  "wihngoSupportAmount": 0.05,
  "totalUsdcRequired": 5.05,
  "solRequired": 0.002,
  "errorCode": null,
  "message": null,
  "recipient": {
    "userId": "uuid",
    "name": "John",
    "walletAddress": "..."
  },
  "bird": {
    "birdId": "uuid",
    "name": "Chirpy",
    "imageUrl": "..."
  },
  "usdcMintAddress": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
  "wihngoWalletAddress": "..."
}
```

---

### `POST /api/support/intents`

Create a support intent for a bird. Returns an unsigned Solana transaction to sign.

**Request:**
```json
{
  "birdId": "uuid",
  "birdAmount": 5.00,
  "wihngoSupportAmount": 0.05,
  "currency": "USDC",
  "idempotencyKey": "sha256-hash"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `birdId` | uuid | Yes | The bird to support |
| `birdAmount` | decimal | Yes | Amount for bird owner ($0.01-$10,000). 100% goes to owner. |
| `wihngoSupportAmount` | decimal | No | Platform tip (default $0.05, min $0.05 if > 0, set 0 to skip) |
| `currency` | string | Yes | Only `"USDC"` supported |
| `idempotencyKey` | string | No | Prevent duplicate intents (1-64 chars) |

**Response (200):**
```json
{
  "intentId": "uuid",
  "birdId": "uuid",
  "birdName": "Chirpy",
  "recipientUserId": "uuid",
  "recipientName": "John",
  "birdWalletAddress": "...",
  "wihngoWalletAddress": "...",
  "birdAmount": 5.00,
  "wihngoSupportAmount": 0.05,
  "totalAmount": 5.05,
  "currency": "USDC",
  "usdcMintAddress": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
  "status": "pending",
  "serializedTransaction": "base64-encoded-unsigned-tx",
  "expiresAt": "2024-01-01T12:00:00Z",
  "createdAt": "2024-01-01T11:55:00Z"
}
```

---

## 2. Support Wihngo Platform Directly

### `POST /api/support/wihngo`

Support Wihngo platform directly without a specific bird.

**Request:**
```json
{
  "amount": 5.00
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `amount` | decimal | Yes | Amount in USDC ($0.05-$10,000) |

**Response (200):**
```json
{
  "intentId": "uuid",
  "amount": 5.00,
  "wihngoWalletAddress": "...",
  "usdcMintAddress": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
  "serializedTransaction": "base64-encoded-unsigned-tx",
  "status": "pending",
  "expiresAt": "2024-01-01T12:00:00Z",
  "createdAt": "2024-01-01T11:55:00Z"
}
```

---

## 3. Shared Endpoints

### `POST /api/support/intents/{intentId}/submit`

Submit a signed transaction after user signs in their wallet.

**Request:**
```json
{
  "paymentId": "uuid",
  "signedTransaction": "base64-encoded-signed-tx",
  "idempotencyKey": "unique-key"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `paymentId` | uuid | Yes | The intent/payment ID |
| `signedTransaction` | string | Yes | Base64 encoded signed transaction from wallet |
| `idempotencyKey` | string | No | Prevent duplicate submissions (1-64 chars) |

**Response (200):**
```json
{
  "paymentId": "uuid",
  "solanaSignature": "tx-signature-on-chain",
  "status": "processing",
  "errorMessage": null,
  "wasAlreadySubmitted": false
}
```

---

### `GET /api/support/intents/{intentId}`

Get the status of a support intent.

**Response (200):** Same as `SupportIntentResponse` from create intent.

---

### `POST /api/support/intents/{intentId}/cancel`

Cancel a pending support intent.

**Response (200):**
```json
{
  "success": true,
  "message": "Support intent cancelled"
}
```

**Response (400):**
```json
{
  "success": false,
  "errorCode": "INTERNAL_ERROR",
  "message": "Support intent cannot be cancelled. It may already be completed or cancelled."
}
```

---

### `GET /api/support/history`

Get the authenticated user's support history.

**Query Parameters:**
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |

**Response (200):**
```json
{
  "items": [ /* array of SupportIntentResponse */ ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

## Error Response Format

All endpoints return validation errors in this format:

```json
{
  "success": false,
  "errorCode": "ERROR_CODE",
  "message": "Human-readable message",
  "fieldErrors": {
    "fieldName": ["Error message 1", "Error message 2"]
  }
}
```

---

## Error Codes

| Code | Description |
|------|-------------|
| `BIRD_NOT_FOUND` | Bird doesn't exist |
| `BIRD_INACTIVE` | Bird is inactive or memorial |
| `SUPPORT_NOT_ENABLED` | Bird owner has disabled support for this bird |
| `CANNOT_SUPPORT_OWN_BIRD` | Users cannot support their own birds |
| `INVALID_AMOUNT` | Amount is out of valid range |
| `INVALID_CURRENCY` | Currency is not USDC |
| `WIHNGO_SUPPORT_TOO_LOW` | Wihngo support amount is > 0 but < $0.05 |
| `WALLET_REQUIRED` | User has no connected wallet |
| `INSUFFICIENT_USDC` | Not enough USDC balance |
| `INSUFFICIENT_SOL` | Not enough SOL for transaction fees |
| `RECIPIENT_NO_WALLET` | Bird owner has no wallet configured |
| `INTENT_EXPIRED` | Support intent has expired |
| `INTENT_NOT_FOUND` | Support intent not found |
| `INTENT_ALREADY_PROCESSED` | Intent already completed or cancelled |
| `TRANSACTION_FAILED` | On-chain transaction failed |
| `INTERNAL_ERROR` | Server error |

---

## Flow Diagram

### Bird Support Flow
```
1. POST /api/support/birds/preflight   → Check if user can support
2. POST /api/support/intents           → Create intent, get unsigned tx
3. User signs transaction in wallet
4. POST /api/support/intents/{id}/submit → Submit signed tx
5. GET /api/support/intents/{id}       → Poll for confirmation
```

### Wihngo Support Flow
```
1. POST /api/support/wihngo            → Create intent, get unsigned tx
2. User signs transaction in wallet
3. POST /api/support/intents/{id}/submit → Submit signed tx
4. GET /api/support/intents/{id}       → Poll for confirmation
```

---

## Intent Status Values

| Status | Description |
|--------|-------------|
| `pending` | Intent created, awaiting signature |
| `awaiting_signature` | Transaction built, waiting for wallet signature |
| `submitted` | Signed transaction submitted to Solana |
| `confirming` | Transaction on-chain, awaiting confirmations |
| `completed` | Transaction confirmed, support successful |
| `expired` | Intent expired before completion |
| `cancelled` | User cancelled the intent |
| `failed` | Transaction failed on-chain |
