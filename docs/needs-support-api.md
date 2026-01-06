# Birds Need Support API Specification

## Overview

The 2-round weekly support system allows bird owners to mark their birds as "needing support". Users can support each bird up to 2 times per week. The cycle resets every Sunday.

### How The 2-Round System Works

| Round | Condition | What Users See |
|-------|-----------|----------------|
| **1** | Start of week | All birds with `needs_support=true` |
| **2** | All birds supported 1x | All birds appear again |
| **Done** | All birds supported 2x | Thank you message, empty list |

**Reset:** Every Sunday at midnight UTC

---

## Endpoints

### 1. Get Birds Needing Support

```
GET /api/needs-support/birds
```

**Authentication:** Not required (public)

**Description:** Returns birds that need support in the current round. This is the main endpoint for the "birds need support" page.

**Response:** `200 OK`

```json
{
  "currentRound": 1,
  "totalRounds": 2,
  "allRoundsComplete": false,
  "thankYouMessage": null,
  "howItWorks": "Wihngo's Weekly Support System:\n\nEvery week, you can help support birds in need through 2 rounds of giving.\n\nRound 1: All birds marked as 'needs support' are shown. Support any bird you'd like!\n\nRound 2: Once every bird has received support once, they all appear again for a second chance to help.\n\nAfter 2 rounds: When all birds have been supported twice this week, you'll see a thank you message. The cycle resets every Sunday!\n\nYour support goes directly to bird owners to help care for their feathered friends.",
  "birds": [
    {
      "birdId": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Koko",
      "species": "Cockatiel",
      "tagline": "A friendly bird who loves seeds",
      "imageUrl": "https://cdn.wihngo.com/birds/koko.jpg",
      "location": "California",
      "ownerName": "Alice",
      "ownerId": "550e8400-e29b-41d4-a716-446655440001",
      "timesSupportedThisWeek": 0,
      "lastSupportedAt": null,
      "totalSupportCount": 42
    }
  ],
  "totalBirdsParticipating": 5,
  "birdsSupportedThisRound": 2,
  "birdsRemainingThisRound": 3,
  "weekStartDate": "2026-01-04T00:00:00Z",
  "weekEndDate": "2026-01-11T00:00:00Z"
}
```

**When all rounds complete:**

```json
{
  "currentRound": null,
  "allRoundsComplete": true,
  "thankYouMessage": "Thank you for your amazing support!\n\nAll 5 birds have received their weekly support (2 times each)!\n\nThe community has come together to help every bird in need this week. Your generosity makes a real difference in the lives of these birds and their caretakers.\n\nThe support cycle will reset on Sunday, and you'll be able to help again!",
  "howItWorks": "...",
  "birds": [],
  "totalBirdsParticipating": 5,
  "birdsSupportedThisRound": 5,
  "birdsRemainingThisRound": 0,
  "weekStartDate": "2026-01-04T00:00:00Z",
  "weekEndDate": "2026-01-11T00:00:00Z"
}
```

---

### 2. Get Weekly Support Statistics

```
GET /api/needs-support/stats
```

**Authentication:** Not required

**Description:** Returns overall weekly support statistics.

**Response:** `200 OK`

```json
{
  "weekStartDate": "2026-01-04T00:00:00Z",
  "weekEndDate": "2026-01-11T00:00:00Z",
  "currentRound": 2,
  "totalBirdsNeedingSupport": 5,
  "birdsFullySupported": 1,
  "totalSupportsThisWeek": 8,
  "allRoundsComplete": false
}
```

---

### 3. Get Bird Weekly Progress

```
GET /api/needs-support/birds/{birdId}/progress
```

**Authentication:** Not required

**Description:** Returns weekly support progress for a specific bird.

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| birdId | UUID | The bird's unique identifier |

**Response:** `200 OK`

```json
{
  "birdId": "550e8400-e29b-41d4-a716-446655440000",
  "birdName": "Koko",
  "timesSupportedThisWeek": 1,
  "maxTimesPerWeek": 2,
  "fullySupportedThisWeek": false,
  "lastSupportedAt": "2026-01-05T14:30:00Z",
  "weekStartDate": "2026-01-04T00:00:00Z",
  "weekEndDate": "2026-01-11T00:00:00Z"
}
```

**Response:** `404 Not Found`

```json
{
  "message": "Bird not found"
}
```

---

### 4. Set Bird Needs Support (Owner Only)

```
PATCH /api/needs-support/birds/{birdId}
```

**Authentication:** Required (JWT Bearer)

**Description:** Allows bird owners to mark their bird as needing support (or remove from the list).

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| birdId | UUID | The bird's unique identifier |

**Request Body:**

```json
{
  "needsSupport": true
}
```

**Response:** `200 OK`

```json
{
  "message": "Your bird is now visible in the 'birds need support' list",
  "birdId": "550e8400-e29b-41d4-a716-446655440000",
  "needsSupport": true
}
```

**When removing from list:**

```json
{
  "message": "Your bird has been removed from the 'birds need support' list",
  "birdId": "550e8400-e29b-41d4-a716-446655440000",
  "needsSupport": false
}
```

**Response:** `401 Unauthorized`

```json
{
  "message": "Invalid user token"
}
```

**Response:** `404 Not Found`

```json
{
  "message": "Bird not found or you don't own this bird"
}
```

---

### 5. Check If Bird Can Receive Support

```
GET /api/needs-support/birds/{birdId}/can-support
```

**Authentication:** Not required

**Description:** Checks if a bird can receive support in the current round. Useful for UI to show/hide support buttons.

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| birdId | UUID | The bird's unique identifier |

**Response:** `200 OK`

```json
{
  "birdId": "550e8400-e29b-41d4-a716-446655440000",
  "canReceiveSupport": true,
  "message": "This bird can receive support"
}
```

**When max reached:**

```json
{
  "birdId": "550e8400-e29b-41d4-a716-446655440000",
  "canReceiveSupport": false,
  "message": "This bird has received maximum support for this week (2 times)"
}
```

---

## Database Schema

### Migration File
`Database/migrations/add_birds_needs_support.sql`

### Birds Table Addition

```sql
ALTER TABLE birds ADD COLUMN needs_support BOOLEAN NOT NULL DEFAULT false;
```

### Weekly Support Tracking Table

```sql
CREATE TABLE weekly_bird_support_rounds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    week_start_date DATE NOT NULL,
    times_supported INTEGER NOT NULL DEFAULT 0,
    last_supported_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_bird_week UNIQUE (bird_id, week_start_date)
);
```

---

## Integration Points

### Payment Completion

When a support payment is confirmed, the system automatically records it for weekly tracking:

**File:** `Services/SupportIntentService.cs` - `ConfirmTransactionAsync()`

```csharp
// Record support for weekly rounds tracking (2-round system)
await _needsSupportService.RecordSupportReceivedAsync(intent.BirdId);
```

### Bird Profile DTO

The `BirdProfileDto` includes fields for owners:

```csharp
// Whether the bird is marked as "needs support"
public bool? NeedsSupport { get; set; }

// How many times this bird has been supported this week (0-2)
public int? TimesSupportedThisWeek { get; set; }
```

---

## Service Registration

**File:** `Program.cs`

```csharp
// Needs Support Service (2-Round Weekly Support System)
builder.Services.AddScoped<INeedsSupportService, NeedsSupportService>();
```

---

## Files Created/Modified

| Action | File |
|--------|------|
| CREATE | `Database/migrations/add_birds_needs_support.sql` |
| CREATE | `Models/Entities/WeeklyBirdSupportRound.cs` |
| CREATE | `Dtos/NeedsSupportDtos.cs` |
| CREATE | `Services/Interfaces/INeedsSupportService.cs` |
| CREATE | `Services/NeedsSupportService.cs` |
| CREATE | `Controllers/NeedsSupportController.cs` |
| MODIFY | `Models/Bird.cs` - Added `NeedsSupport` property |
| MODIFY | `Dtos/BirdProfileDto.cs` - Added `NeedsSupport` and `TimesSupportedThisWeek` |
| MODIFY | `Services/SupportIntentService.cs` - Integration with payment completion |
| MODIFY | `Program.cs` - Service registration |
