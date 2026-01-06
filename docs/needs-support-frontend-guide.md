# Birds Need Support - Frontend Implementation Guide

## Overview

This feature allows users to see birds that need support and help them through a 2-round weekly cycle. Bird owners can opt-in their birds to appear in this list.

---

## User Flows

### Flow 1: Supporter Views "Birds Need Support" Page

```
User opens "Birds Need Support" page
    ↓
App calls GET /api/needs-support/birds
    ↓
If allRoundsComplete = false:
    → Display birds list with support buttons
    → Show round progress (e.g., "Round 1 of 2")
    ↓
If allRoundsComplete = true:
    → Display thankYouMessage
    → Hide birds list
    → Show "Come back Sunday!" message
```

### Flow 2: Supporter Supports a Bird

```
User taps "Support" on a bird
    ↓
App calls existing support intent flow
    ↓
On payment success, backend automatically:
    → Updates weekly tracking
    → Bird removed from current round list
    ↓
Refresh the birds list
```

### Flow 3: Owner Enables "Needs Support"

```
Owner views their bird profile
    ↓
Owner toggles "Needs Support" switch ON
    ↓
App calls PATCH /api/needs-support/birds/{birdId}
    Body: { "needsSupport": true }
    ↓
Bird now appears in "Birds Need Support" list
```

---

## Screens to Build

### 1. Birds Need Support List Page

**Route:** `/needs-support` or `/birds-need-support`

**API Call:** `GET /api/needs-support/birds`

**UI Elements:**

| Element | Source Field | Notes |
|---------|--------------|-------|
| Round indicator | `currentRound` / `totalRounds` | "Round 1 of 2" |
| Progress bar | `birdsSupportedThisRound` / `totalBirdsParticipating` | Visual progress |
| Birds remaining | `birdsRemainingThisRound` | "3 birds still need support" |
| Week dates | `weekStartDate` / `weekEndDate` | "This week: Jan 5 - Jan 11" |
| How it works | `howItWorks` | Expandable info section |
| Bird cards | `birds[]` | List of bird cards |
| Thank you message | `thankYouMessage` | When `allRoundsComplete = true` |

**Bird Card Layout:**
```
┌─────────────────────────────────┐
│  [Bird Image]                   │
│                                 │
│  Bird Name                      │
│  Species • Location             │
│  "Tagline here..."              │
│                                 │
│  Owner: Alice                   │
│  Supported: 1/2 this week       │
│                                 │
│  [ Support $1 ]                 │
└─────────────────────────────────┘
```

**States:**

1. **Loading** - Show skeleton cards
2. **Empty (no birds)** - "No birds need support right now"
3. **Birds available** - Show bird cards with support buttons
4. **All rounds complete** - Show thank you message, hide cards

### 2. Owner Bird Settings

**Location:** Bird edit/settings screen

**New Toggle:**
```
┌─────────────────────────────────┐
│  Needs Support                  │
│  ┌────┐                         │
│  │ ON │  Your bird will appear  │
│  └────┘  in "Birds Need Support"│
└─────────────────────────────────┘
```

**API Call:** `PATCH /api/needs-support/birds/{birdId}`

---

## API Integration

### Fetch Birds Needing Support

```typescript
interface BirdsNeedSupportResponse {
  currentRound: number | null;
  totalRounds: number;
  allRoundsComplete: boolean;
  thankYouMessage: string | null;
  howItWorks: string;
  birds: BirdNeedsSupportDto[];
  totalBirdsParticipating: number;
  birdsSupportedThisRound: number;
  birdsRemainingThisRound: number;
  weekStartDate: string;
  weekEndDate: string;
}

interface BirdNeedsSupportDto {
  birdId: string;
  name: string;
  species: string | null;
  tagline: string | null;
  imageUrl: string;
  location: string | null;
  ownerName: string;
  ownerId: string;
  timesSupportedThisWeek: number;
  lastSupportedAt: string | null;
  totalSupportCount: number;
}

// Fetch birds
const response = await fetch('/api/needs-support/birds');
const data: BirdsNeedSupportResponse = await response.json();
```

### Toggle Needs Support (Owner)

```typescript
interface SetNeedsSupportRequest {
  needsSupport: boolean;
}

// Enable needs support
await fetch(`/api/needs-support/birds/${birdId}`, {
  method: 'PATCH',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({ needsSupport: true })
});
```

### Check If Bird Can Be Supported

```typescript
// Before showing support button (optional optimization)
const response = await fetch(`/api/needs-support/birds/${birdId}/can-support`);
const { canReceiveSupport, message } = await response.json();

if (!canReceiveSupport) {
  // Show message: "This bird has received maximum support for this week"
}
```

---

## UI Copy & Messaging

### Round Progress
```
Round 1 of 2
Round 2 of 2 - Final round!
```

### Birds Remaining
```
5 birds still need support this round
1 bird still needs support this round
All birds supported! Starting Round 2...
```

### Thank You Message (when all complete)
Display the `thankYouMessage` from API. It includes:
- Congratulations
- Number of birds helped
- "Come back Sunday" reminder

### How It Works (expandable section)
Display the `howItWorks` field from API. Users can tap to expand/collapse.

### Empty States
```
// No birds opted in
"No birds need support right now. Check back later!"

// All rounds complete
[Display thankYouMessage from API]
```

---

## Refresh Strategy

1. **On page load** - Fetch fresh data
2. **After supporting a bird** - Refetch list (bird will be removed from current round)
3. **Pull-to-refresh** - Allow manual refresh
4. **No polling needed** - Data changes only on support actions

---

## Navigation

Add entry point to "Birds Need Support" page:

```
Home Screen
    ↓
[Birds Need Support] button/card
    ↓
/needs-support page
```

Suggested placement:
- Home screen card/banner
- Bottom navigation tab
- Discover/Explore section

---

## Design Considerations

### Visual Hierarchy
1. Round progress (top) - Users should know which round they're in
2. Bird cards (middle) - Main content
3. How it works (bottom) - Educational, collapsible

### Support Button States
- **Default:** "Support $1"
- **Loading:** Spinner
- **Success:** Checkmark, then remove card from list
- **Disabled:** When `canReceiveSupport = false`

### Progress Visualization
```
Round 1 of 2
[████████░░░░░░░░] 3/5 birds supported

Round 2 of 2
[████░░░░░░░░░░░░] 1/5 birds supported
```

### Thank You State
When `allRoundsComplete = true`:
- Show celebratory UI (confetti?)
- Display `thankYouMessage`
- Show countdown to Sunday reset (optional)

---

## Testing Checklist

- [ ] Birds list loads correctly
- [ ] Round indicator shows correct round (1 or 2)
- [ ] Progress updates after supporting a bird
- [ ] Bird disappears from list after support (current round)
- [ ] All birds reappear when Round 2 starts
- [ ] Thank you message shows when all rounds complete
- [ ] Owner can toggle needs_support on/off
- [ ] Empty state shows when no birds need support
- [ ] Pull-to-refresh works
- [ ] Deep link to specific bird works

---

## Error Handling

| Scenario | User Message |
|----------|--------------|
| Network error | "Couldn't load birds. Pull to retry." |
| Support failed | "Support failed. Please try again." |
| Toggle failed | "Couldn't update settings. Please try again." |
| Bird not found | "This bird is no longer available." |
