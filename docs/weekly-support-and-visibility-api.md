# Weekly Support & Bird Visibility API

## Overview

Two features implemented:

1. **Weekly Support** - Non-custodial recurring donations where users subscribe to weekly bird support, receive reminders, and approve payments with 1-click in Phantom wallet.

2. **Bird Visibility** - Allow bird owners to hide/unpublish birds from public listings (draft mode).

---

## Database Migrations

Run these before deploying:

```bash
psql -f Database/migrations/add_bird_is_public.sql
psql -f Database/migrations/weekly_support_subscriptions.sql
```

---

## Bird Visibility API

### Update Bird Visibility

Hide or show a bird from public listings.

```
PATCH /api/birds/{id}/visibility
Authorization: Bearer <token>
Content-Type: application/json
```

**Request:**
```json
{
  "isPublic": false
}
```

| Field | Type | Description |
|-------|------|-------------|
| `isPublic` | boolean | `true` = visible to all, `false` = hidden/draft |

**Response 200:**
```json
{
  "success": true,
  "isPublic": false,
  "message": "This bird is now hidden from public"
}
```

**Errors:**
- `403 Forbidden` - Not the bird owner
- `404 Not Found` - Bird not found

### Visibility Behavior

| Scenario | Behavior |
|----------|----------|
| Hidden bird in `GET /api/birds` | Not included in listing |
| Hidden bird in `GET /api/birds/{id}` | Returns 404 for non-owners |
| Owner views hidden bird | Returns full profile with `isPublic: false` |
| `isPublic` field in response | Only returned to owner, `null` for others |

---

## Weekly Support API

### Create Subscription

Subscribe to weekly support for a bird.

```
POST /api/weekly-support/subscriptions
Authorization: Bearer <token>
Content-Type: application/json
```

**Request:**
```json
{
  "birdId": "uuid",
  "amountUsdc": 1.00,
  "wihngoSupportAmount": 0.00,
  "dayOfWeek": 0,
  "preferredHour": 10
}
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `birdId` | uuid | required | Bird to support |
| `amountUsdc` | decimal | 1.00 | Weekly amount to bird owner |
| `wihngoSupportAmount` | decimal | 0.00 | Optional platform tip |
| `dayOfWeek` | int | 0 | 0=Sunday, 6=Saturday |
| `preferredHour` | int | 10 | UTC hour (0-23) for reminders |

**Response 201:**
```json
{
  "subscriptionId": "uuid",
  "birdId": "uuid",
  "birdName": "Tweety",
  "birdImageUrl": "https://...",
  "birdSpecies": "Canary",
  "recipientUserId": "uuid",
  "recipientName": "John",
  "amountUsdc": 1.00,
  "wihngoSupportAmount": 0.00,
  "totalAmount": 1.00,
  "currency": "USDC",
  "status": "active",
  "dayOfWeek": 0,
  "dayOfWeekName": "Sunday",
  "preferredHour": 10,
  "nextReminderAt": "2026-01-12T10:00:00Z",
  "totalPaymentsCount": 0,
  "createdAt": "2026-01-06T12:00:00Z"
}
```

---

### List Subscriptions

Get all weekly support subscriptions for current user.

```
GET /api/weekly-support/subscriptions
Authorization: Bearer <token>
```

**Response 200:**
```json
[
  {
    "subscriptionId": "uuid",
    "birdId": "uuid",
    "birdName": "Tweety",
    "birdImageUrl": "https://...",
    "amountUsdc": 1.00,
    "status": "active",
    "nextReminderAt": "2026-01-12T10:00:00Z",
    "totalPaymentsCount": 5
  }
]
```

---

### Get Subscription Details

```
GET /api/weekly-support/subscriptions/{id}
Authorization: Bearer <token>
```

**Response 200:** Full `WeeklySupportSubscriptionResponse` object

**Response 404:** Subscription not found or not owned by user

---

### Update Subscription

```
PUT /api/weekly-support/subscriptions/{id}
Authorization: Bearer <token>
Content-Type: application/json
```

**Request (all fields optional):**
```json
{
  "amountUsdc": 2.00,
  "wihngoSupportAmount": 0.05,
  "dayOfWeek": 1,
  "preferredHour": 9
}
```

---

### Pause Subscription

Stop sending reminders temporarily.

```
POST /api/weekly-support/subscriptions/{id}/pause
Authorization: Bearer <token>
```

**Response 204:** No content

---

### Resume Subscription

Resume a paused subscription.

```
POST /api/weekly-support/subscriptions/{id}/resume
Authorization: Bearer <token>
```

**Response 204:** No content

---

### Cancel Subscription

Permanently cancel a subscription.

```
DELETE /api/weekly-support/subscriptions/{id}
Authorization: Bearer <token>
```

**Response 204:** No content

---

### Get Pending Approvals

Get weekly payments waiting for user approval.

```
GET /api/weekly-support/pending
Authorization: Bearer <token>
```

**Response 200:**
```json
[
  {
    "paymentId": "uuid",
    "subscriptionId": "uuid",
    "birdId": "uuid",
    "birdName": "Tweety",
    "birdImageUrl": "https://...",
    "recipientName": "John",
    "amountUsdc": 1.00,
    "wihngoSupportAmount": 0.00,
    "totalAmount": 1.00,
    "weekStartDate": "2026-01-05",
    "weekEndDate": "2026-01-11",
    "status": "reminder_sent",
    "reminderSentAt": "2026-01-05T10:00:00Z",
    "expiresAt": "2026-01-12T10:00:00Z",
    "isExpired": false,
    "approveDeepLink": "/weekly-support/approve/uuid"
  }
]
```

---

### Approve Payment (1-Click)

Create a pre-filled support intent for quick approval.

```
POST /api/weekly-support/approve
Authorization: Bearer <token>
Content-Type: application/json
```

**Request:**
```json
{
  "paymentId": "uuid",
  "idempotencyKey": "optional-string"
}
```

**Response 200:**
```json
{
  "intentId": "uuid",
  "paymentId": "uuid",
  "birdWalletAddress": "So1ana...",
  "wihngoWalletAddress": "So1ana...",
  "amountUsdc": 1.00,
  "wihngoSupportAmount": 0.00,
  "totalAmount": 1.00,
  "serializedTransaction": "base64-encoded-unsigned-tx",
  "expiresAt": "2026-01-06T13:00:00Z",
  "wasAlreadyCreated": false
}
```

**Frontend Flow:**
1. Call this endpoint to get `serializedTransaction`
2. Decode base64 and sign with Phantom wallet
3. Submit signed transaction via `POST /api/weekly-support/submit`

---

### Submit Signed Transaction

Submit the signed Solana transaction after Phantom approval.

```
POST /api/weekly-support/submit
Authorization: Bearer <token>
Content-Type: application/json
```

**Request:**
```json
{
  "intentId": "uuid",
  "signedTransaction": "base64-encoded-signed-tx",
  "idempotencyKey": "optional-string"
}
```

**Response 200:**
```json
{
  "success": true,
  "signature": "solana-tx-signature",
  "status": "processing"
}
```

---

### Get User Summary

Get weekly support statistics for current user.

```
GET /api/weekly-support/summary
Authorization: Bearer <token>
```

**Response 200:**
```json
{
  "activeSubscriptions": 3,
  "pausedSubscriptions": 1,
  "weeklyTotalUsdc": 4.00,
  "lifetimeTotalPaidUsdc": 52.00,
  "pendingApprovals": 2
}
```

---

### Get Payment History

```
GET /api/weekly-support/subscriptions/{id}/payments?limit=20
Authorization: Bearer <token>
```

**Response 200:**
```json
[
  {
    "paymentId": "uuid",
    "subscriptionId": "uuid",
    "birdId": "uuid",
    "birdName": "Tweety",
    "weekStartDate": "2025-12-29",
    "weekEndDate": "2026-01-04",
    "amountUsdc": 1.00,
    "totalAmount": 1.00,
    "status": "completed",
    "completedAt": "2025-12-30T14:23:00Z",
    "createdAt": "2025-12-29T10:00:00Z"
  }
]
```

---

## Payment Status Values

| Status | Description |
|--------|-------------|
| `pending_reminder` | Waiting for reminder time |
| `reminder_sent` | Reminder sent, awaiting approval |
| `intent_created` | User clicked approve, waiting for signature |
| `completed` | Payment confirmed on-chain |
| `expired` | User didn't approve within 7 days |
| `skipped` | Skipped (e.g., recipient has no wallet) |

---

## Subscription Status Values

| Status | Description |
|--------|-------------|
| `active` | Receiving weekly reminders |
| `paused` | Reminders paused (manual or auto after 3 misses) |
| `cancelled` | Permanently cancelled |

---

## Notification Types

New notification types for weekly support:

| Type | Description |
|------|-------------|
| `WeeklySupportReminder` | Time to approve weekly payment |
| `WeeklySupportCompleted` | Payment confirmed |
| `WeeklySupportMissed` | Payment expired without approval |
| `WeeklySupportSubscribed` | New subscription created |

---

## Background Jobs

| Job | Schedule | Purpose |
|-----|----------|---------|
| `weekly-support-reminders` | Every 15 min | Send due reminders |
| `expire-weekly-reminders` | Every hour | Expire 7-day-old payments |

---

## Files Created/Modified

### New Files
- `Database/migrations/weekly_support_subscriptions.sql`
- `Database/migrations/add_bird_is_public.sql`
- `Models/Entities/WeeklySupportSubscription.cs`
- `Models/Entities/WeeklySupportPayment.cs`
- `Dtos/WeeklySupportDtos.cs`
- `Services/Interfaces/IWeeklySupportService.cs`
- `Services/WeeklySupportService.cs`
- `BackgroundJobs/WeeklySupportReminderJob.cs`
- `Controllers/WeeklySupportController.cs`

### Modified Files
- `Models/Bird.cs` - Added `IsPublic` field
- `Models/Enums/NotificationType.cs` - Added 4 notification types
- `Dtos/BirdCreateDto.cs` - Added `BirdVisibilityDto`
- `Dtos/BirdProfileDto.cs` - Added `IsPublic?` field
- `Controllers/BirdsController.cs` - Added visibility endpoint + filtering
- `Program.cs` - Registered services and Hangfire jobs
