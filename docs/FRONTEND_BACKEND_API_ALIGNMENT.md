# Frontend-Backend API Alignment Analysis

**Date:** 2026-01-01
**Purpose:** Compare frontend expectations with current backend implementation to identify gaps and required changes.

---

## Executive Summary

The frontend has implemented a production-grade Phantom → USDC payment flow with session recovery, idempotency, and error handling. This document identifies mismatches between frontend expectations and current backend implementation.

**Key Issues Found:**
1. Endpoint naming mismatch (`/intents` vs `/transfers`)
2. Idempotency key is required on backend but optional on frontend
3. Status value naming differences (PascalCase vs lowercase)
4. Response field naming differences

---

## 1. Endpoint Naming Mismatch

| Frontend Expects | Current Backend | Status |
|------------------|-----------------|--------|
| `POST /support/intents` | `POST /support/transfers` | ❌ Mismatch |
| `POST /support/intents/{id}/submit` | `POST /support/transfers/{id}/submit` | ❌ Mismatch |
| `GET /support/intents/{id}` | `GET /support/transfers/{id}` | ❌ Mismatch |
| `POST /support/birds/preflight` | `POST /support/transfers/preflight` | ❌ Mismatch |
| `GET /wallets/{address}/on-chain-balance` | TBD | ⚠️ Verify |

### Action Required
Either:
- **Option A:** Rename backend endpoints from `/transfers` to `/intents`
- **Option B:** Frontend updates to use `/transfers`

---

## 2. Idempotency Key Implementation

### 2.1 Intent Creation (`POST /support/intents`)

| Aspect | Frontend Expects | Backend Current |
|--------|------------------|-----------------|
| Field name | `idempotencyKey` | ❌ Not implemented |
| Required? | Optional | N/A |
| Format | 32-char SHA-256 hash | N/A |
| Behavior | Return existing pending intent if key matches | N/A |

**Frontend generates key as:**
```typescript
// SHA-256 hash of "{userId}|{birdId}|{birdAmount}|{wihngoAmount}|{minuteBucket}"
// Example: "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6" (32 chars)
```

### 2.2 Submit Transaction (`POST /support/intents/{id}/submit`)

| Aspect | Frontend Expects | Backend Current |
|--------|------------------|-----------------|
| Field name | `idempotencyKey` | `idempotencyKey` ✅ |
| Required? | **Optional** | **Required** ❌ |
| Format | `{intentId}-{attemptNumber}-{timestamp}` | 8-64 chars |
| Min length | N/A | 8 chars |
| Max length | ~50 chars | 64 chars ✅ |

**Frontend generates key as:**
```typescript
// Format: "{intentId}-{attemptNumber}-{timestamp}"
// Example: "abc123-1-1705312345678"
```

### Action Required

1. **Make `idempotencyKey` optional on submit** (change from required)
2. **Add `idempotencyKey` support to intent creation**
3. Remove minimum length validation (or reduce to 1)

---

## 3. Status Value Mismatch

### 3.1 Payment Status Values

| Frontend Expects | Backend Returns | Action |
|------------------|-----------------|--------|
| `pending` | `pending` | ✅ Match |
| `signed` | `awaiting_signature` | ❌ Map or rename |
| `submitted` | `submitted` | ✅ Match |
| `confirmed` | `confirming` | ⚠️ Different meaning |
| `confirmed` | `confirmed` | ✅ Match |
| - | `completed` | Add to frontend |
| `failed` | `failed` | ✅ Match |
| `expired` | `expired` | ✅ Match |
| - | `cancelled` | Add to frontend |
| - | `timeout` | Add to frontend |

### 3.2 Submit Response Status (PascalCase vs lowercase)

| Frontend Expects | Backend Returns |
|------------------|-----------------|
| `Completed` | `completed` |
| `Confirming` | `confirming` |
| `Processing` | `submitted` |
| `Failed` | `failed` |

### Action Required

Either:
- **Option A:** Backend returns PascalCase status values
- **Option B:** Frontend handles case conversion

---

## 4. Response Format Differences

### 4.1 Submit Transaction Response

**Frontend Expects:**
```json
{
  "status": "Completed",
  "solanaSignature": "tx-signature...",
  "message": "Optional message"
}
```

**Backend Currently Returns:**
```json
{
  "paymentId": "uuid",
  "solanaSignature": "...",
  "status": "submitted",
  "errorMessage": "...",
  "wasAlreadySubmitted": true
}
```

| Field | Frontend | Backend | Action |
|-------|----------|---------|--------|
| `status` | PascalCase | lowercase | Align |
| `solanaSignature` | ✅ | ✅ | Match |
| `message` | Expected | `errorMessage` | Rename |
| `paymentId` | Not expected | Returned | Keep (extra info) |
| `wasAlreadySubmitted` | Not expected | Returned | Keep (extra info) |

### 4.2 Intent Status Response (`GET /support/intents/{id}`)

**Frontend Expects:**
```json
{
  "intentId": "uuid",
  "status": "pending",
  "solanaSignature": "...",
  "serializedTransaction": "...",
  "supportParams": {
    "birdId": "uuid",
    "birdAmount": 5.00,
    "wihngoAmount": 1.00
  }
}
```

**Verify:** Does backend return `supportParams` object?

---

## 5. Error Response Format

### Frontend Expects:
```json
{
  "error": "ERROR_CODE",
  "message": "Human readable message",
  "details": {}
}
```

### Error Codes Frontend Handles:

| Code | Description |
|------|-------------|
| `INSUFFICIENT_BALANCE` | User doesn't have enough USDC |
| `INSUFFICIENT_GAS` | User doesn't have enough SOL for fees |
| `INTENT_EXPIRED` | Intent has expired (timeout) |
| `TX_FAILED` | Transaction failed on Solana |
| `NETWORK_CONGESTION` | Solana network is busy |
| `BLOCKHASH_EXPIRED` | Transaction blockhash expired |
| `INVALID_TRANSACTION` | Transaction validation failed |

### Action Required
Verify backend returns these exact error codes in the `error` field.

---

## 6. Missing Backend Features

### 6.1 Idempotency on Intent Creation

Frontend sends `idempotencyKey` on `POST /support/intents` but backend doesn't handle it.

**Required Behavior:**
1. Store `idempotencyKey` with intent
2. If same key submitted again within window:
   - Return existing pending intent (don't create new one)
3. Key should be valid for ~1 minute (frontend cache duration)

### 6.2 On-Chain Balance Endpoint

**Endpoint:** `GET /wallets/{walletAddress}/on-chain-balance`

**Expected Response:**
```json
{
  "solBalance": 0.5,
  "usdcBalance": 100.00
}
```

**Note:** This should be a public endpoint (no auth required)

---

## 7. Recommended Changes

### Backend Changes (Minimal Disruption)

| Priority | Change | Files Affected | Status |
|----------|--------|----------------|--------|
| HIGH | Make `idempotencyKey` optional on submit | `Dtos/P2PPaymentDtos.cs` | ✅ DONE |
| HIGH | Add idempotency support to intent creation | `Services/SupportIntentService.cs`, `Dtos/SupportIntentDtos.cs` | ✅ DONE |
| HIGH | Add idempotency support to transaction submission | `Services/SupportIntentService.cs`, `Controllers/SupportController.cs` | ✅ DONE |
| MEDIUM | Rename `errorMessage` to `message` in response | `Dtos/P2PPaymentDtos.cs` | Pending |
| MEDIUM | Return PascalCase status or add mapping | `Services/P2PPaymentService.cs` | Pending |
| LOW | Add `supportParams` to intent status response | `Dtos/P2PPaymentDtos.cs` | Pending |
| LOW | Verify/add on-chain balance endpoint | `Controllers/WalletsController.cs` | Pending |

### Frontend Changes (If Backend Doesn't Change)

| Change | Description |
|--------|-------------|
| Case handling | Convert lowercase status to PascalCase |
| Field mapping | Map `errorMessage` to `message` |
| Endpoint URLs | Use `/transfers` instead of `/intents` |
| Required field | Send `idempotencyKey` on submit |

---

## 8. Questions Requiring Decision

### Q1: Endpoint Naming
Should backend rename `/support/transfers` to `/support/intents`?
- [ ] Yes, rename backend endpoints
- [x] No, frontend will use correct endpoints (`/support/intents` for bird support, `/support/transfers` for P2P)

**Note:** The backend already has both endpoints:
- `/api/support/intents/*` - Bird support flow (matches frontend expectation)
- `/api/support/transfers/*` - P2P transfer flow

### Q2: Idempotency on Submit
Should `idempotencyKey` be required or optional?
- [ ] Required (frontend must update)
- [x] Optional (backend updated) ✅ IMPLEMENTED

### Q3: Status Value Case
Who handles the case conversion?
- [ ] Backend returns PascalCase (`Completed`, `Failed`, etc.)
- [x] Frontend converts lowercase to PascalCase (recommended)

### Q4: Intent Creation Idempotency
Should backend implement idempotency on intent creation?
- [x] Yes, implement full idempotency ✅ IMPLEMENTED
- [ ] No, frontend will handle duplicates

---

## 9. Testing Scenarios

Once alignment is complete, test these scenarios:

### Happy Path
- [ ] Desktop Chrome → Full payment flow
- [ ] Mobile Safari → Deep link flow
- [ ] iOS PWA → Manual return flow

### Error Recovery
- [ ] User rejects wallet connection → Retry works
- [ ] User rejects transaction → Retry works
- [ ] Network error during preflight → Auto-retry (3 attempts)
- [ ] Network error during intent creation → Auto-retry (3 attempts)
- [ ] Intent expires → "Start Over" button works

### Idempotency
- [ ] Double-click submit button → Same result returned
- [ ] Page refresh during payment → Session recovery works
- [ ] Retry after error → Same intent used (within 1 minute)

---

## 10. Files Reference

### Backend Files (Already Modified)
| File | Changes Made |
|------|--------------|
| `Dtos/P2PPaymentDtos.cs` | Made `IdempotencyKey` optional, added `WasAlreadySubmitted` |
| `Dtos/SupportIntentDtos.cs` | Added `IdempotencyKey` to `CreateSupportIntentRequest` |
| `Models/Entities/P2PPayment.cs` | Added `IdempotencyKey`, `ErrorMessage`, `Timeout` status |
| `Models/Entities/SupportIntent.cs` | Added `IdempotencyKey`, `ErrorMessage` properties |
| `Services/P2PPaymentService.cs` | Idempotency check on submit, timeout handling |
| `Services/SupportIntentService.cs` | Idempotency check on create and submit |
| `Services/Interfaces/ISupportIntentService.cs` | Updated `SubmitSignedTransactionAsync` signature |
| `Controllers/SupportController.cs` | Pass idempotency key to service |
| `Database/migrations/add_solana_signature_constraint.sql` | Added `idempotency_key` column to p2p_payments |
| `Database/migrations/add_support_intent_idempotency.sql` | Added `idempotency_key` column to support_intents |

### Frontend Files (Per Guide)
| File | Purpose |
|------|---------|
| `src/lib/idempotency.ts` | Idempotency key generation & caching |
| `src/lib/retry.ts` | Retry utility with exponential backoff |
| `src/services/support.service.ts` | API calls with idempotency & retry |
| `src/services/session-recovery.service.ts` | Session recovery logic |
| `src/services/error-mapping.service.ts` | Error code to message mapping |

---

## Appendix: Current DTO Definitions

### SubmitTransactionRequest (Updated)
```csharp
public class SubmitTransactionRequest
{
    [Required]
    public Guid PaymentId { get; set; }

    [Required]
    public string SignedTransaction { get; set; }

    // ✅ NOW OPTIONAL - matches frontend expectation
    [StringLength(64, MinimumLength = 1)]
    public string? IdempotencyKey { get; set; }
}
```

### CreateSupportIntentRequest (Updated)
```csharp
public class CreateSupportIntentRequest
{
    [Required]
    public Guid BirdId { get; set; }

    [Required]
    [Range(0.01, 10000)]
    public decimal BirdAmount { get; set; }

    [Range(0, 10000)]
    public decimal WihngoSupportAmount { get; set; } = 0.05m;

    [Required]
    public string Currency { get; set; } = "USDC";

    // ✅ NEW - Optional idempotency key for duplicate prevention
    [StringLength(64, MinimumLength = 1)]
    public string? IdempotencyKey { get; set; }
}
```

### SubmitTransactionResponse (Current)
```csharp
public class SubmitTransactionResponse
{
    public Guid PaymentId { get; set; }
    public string? SolanaSignature { get; set; }
    public string Status { get; set; }  // lowercase
    public string? ErrorMessage { get; set; }  // frontend expects "message"
    public bool WasAlreadySubmitted { get; set; }  // ✅ Returns true if idempotent duplicate
}
```
