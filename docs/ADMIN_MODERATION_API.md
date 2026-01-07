# Admin Moderation API Specification

Base URL: `/api/love-videos`

All admin endpoints require `Authorization: Bearer <token>` with admin privileges.

---

## 1. List Pending Love Videos

Get all love videos awaiting moderation.

### Request

```http
GET /api/love-videos/pending
Authorization: Bearer <admin_token>
```

### Response

```json
// 200 OK
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "youtubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
    "youtubeVideoId": "dQw4w9WgXcQ",
    "mediaUrl": null,
    "mediaType": null,
    "description": "Beautiful parrot enjoying morning sunshine",
    "status": "pending",
    "submittedByUserId": "123e4567-e89b-12d3-a456-426614174000",
    "createdAt": "2025-01-07T10:30:00Z",
    "rejectionReason": null
  }
]
```

### Error Responses

| Status | Error Code | Description |
|--------|------------|-------------|
| 401 | UNAUTHORIZED | Missing or invalid token |
| 403 | FORBIDDEN | User is not an admin |
| 500 | INTERNAL_ERROR | Server error |

---

## 2. Get Love Video for Moderation

Get full details of a love video (any status) for review.

### Request

```http
GET /api/love-videos/moderation/{id}
Authorization: Bearer <admin_token>
```

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | UUID | Love video ID |

### Response

```json
// 200 OK
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "youtubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
  "youtubeVideoId": "dQw4w9WgXcQ",
  "mediaUrl": null,
  "mediaType": null,
  "description": "Beautiful parrot enjoying morning sunshine",
  "status": "pending",
  "submittedByUserId": "123e4567-e89b-12d3-a456-426614174000",
  "createdAt": "2025-01-07T10:30:00Z",
  "rejectionReason": null
}
```

### Error Responses

| Status | Error Code | Description |
|--------|------------|-------------|
| 401 | UNAUTHORIZED | Missing or invalid token |
| 403 | FORBIDDEN | User is not an admin |
| 404 | LOVE_VIDEO_NOT_FOUND | Love video does not exist |
| 500 | INTERNAL_ERROR | Server error |

---

## 3. Approve Love Video

Approve a pending love video, making it publicly visible.

### Request

```http
POST /api/love-videos/{id}/approve
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "description": "Optional: Override the description"
}
```

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | UUID | Love video ID |

### Request Body (Optional)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| description | string | No | Override description (max 500 chars) |

### Response

```json
// 200 OK
{
  "success": true,
  "loveVideoId": "550e8400-e29b-41d4-a716-446655440000",
  "newStatus": "approved",
  "message": "Love video approved successfully"
}
```

### Error Responses

| Status | Error Code | Description |
|--------|------------|-------------|
| 400 | ALREADY_APPROVED | Video is already approved |
| 400 | NOT_PENDING | Video is not in pending status |
| 401 | UNAUTHORIZED | Missing or invalid token |
| 403 | FORBIDDEN | User is not an admin |
| 404 | LOVE_VIDEO_NOT_FOUND | Love video does not exist |
| 500 | INTERNAL_ERROR | Server error |

---

## 4. Reject Love Video

Reject a pending love video with a reason.

### Request

```http
POST /api/love-videos/{id}/reject
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "reason": "Content promotes specific bird fundraising"
}
```

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | UUID | Love video ID |

### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| reason | string | **Yes** | Rejection reason (max 500 chars) |

### Response

```json
// 200 OK
{
  "success": true,
  "loveVideoId": "550e8400-e29b-41d4-a716-446655440000",
  "newStatus": "rejected",
  "message": "Love video rejected"
}
```

### Common Rejection Reasons

| Reason Code | Description |
|-------------|-------------|
| ASKS_FOR_DONATIONS | Content asks for donations |
| OWNERSHIP_FRAMING | Uses "my bird" ownership framing |
| URGENCY_MANIPULATION | Uses urgency manipulation tactics |
| BIRD_SPECIFIC_FUNDRAISING | Directs funds to specific bird |
| PROMOTIONAL_CONTENT | Promotional or influencer content |
| CONTENT_VIOLATION | General content policy violation |

### Error Responses

| Status | Error Code | Description |
|--------|------------|-------------|
| 400 | ALREADY_REJECTED | Video is already rejected |
| 400 | NOT_PENDING | Video is not in pending status |
| 400 | - | Reason is required |
| 401 | UNAUTHORIZED | Missing or invalid token |
| 403 | FORBIDDEN | User is not an admin |
| 404 | LOVE_VIDEO_NOT_FOUND | Love video does not exist |
| 500 | INTERNAL_ERROR | Server error |

---

## 5. Hide Approved Love Video

Hide a previously approved love video (sets status to rejected).

### Request

```http
POST /api/love-videos/{id}/hide
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "reason": "Reported by users for inappropriate content"
}
```

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | UUID | Love video ID |

### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| reason | string | **Yes** | Reason for hiding (max 500 chars) |

### Response

```json
// 200 OK
{
  "success": true,
  "loveVideoId": "550e8400-e29b-41d4-a716-446655440000",
  "newStatus": "rejected",
  "message": "Love video hidden"
}
```

### Error Responses

| Status | Error Code | Description |
|--------|------------|-------------|
| 401 | UNAUTHORIZED | Missing or invalid token |
| 403 | FORBIDDEN | User is not an admin |
| 404 | LOVE_VIDEO_NOT_FOUND | Love video does not exist |
| 500 | INTERNAL_ERROR | Server error |

---

## Status Flow

```
                    ┌─────────────┐
                    │   pending   │
                    └──────┬──────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            │            ▼
       ┌──────────┐        │     ┌──────────┐
       │ approved │        │     │ rejected │
       └────┬─────┘        │     └──────────┘
            │              │
            │   /hide      │
            └──────────────┘
```

| Status | Publicly Visible | Description |
|--------|------------------|-------------|
| pending | No | Awaiting admin review |
| approved | **Yes** | Visible to all users |
| rejected | No | Hidden, never visible |

---

## Data Models

### LoveVideoModerationItem

```typescript
interface LoveVideoModerationItem {
  id: string;                    // UUID
  youtubeUrl: string | null;     // YouTube URL (if YouTube submission)
  youtubeVideoId: string | null; // Extracted video ID
  mediaUrl: string | null;       // Direct media URL (if media upload)
  mediaType: string | null;      // "image" | "video" (if media upload)
  description: string | null;    // User description (max 500 chars)
  status: "pending" | "approved" | "rejected";
  submittedByUserId: string;     // UUID of submitter
  createdAt: string;             // ISO 8601 timestamp
  rejectionReason: string | null;

  // AI Moderation Fields
  aiDecision: "auto_approve" | "needs_human_review" | "reject" | null;
  aiConfidence: number | null;   // 0.0 to 1.0
  aiFlags: string[] | null;      // ["safe", "spam", "off_topic", etc.]
  aiReasons: string[] | null;    // Human-readable reasons
}
```

### LoveVideoModerationResponse

```typescript
interface LoveVideoModerationResponse {
  success: boolean;
  loveVideoId: string;           // UUID
  newStatus: string;             // New status after action
  message: string | null;        // Human-readable message
  errorCode: string | null;      // Error code if failed
}
```

### ApproveLoveVideoRequest

```typescript
interface ApproveLoveVideoRequest {
  description?: string;          // Optional description override (max 500)
}
```

### RejectLoveVideoRequest

```typescript
interface RejectLoveVideoRequest {
  reason: string;                // Required (max 500 chars)
}
```

---

## Error Codes Reference

| Code | Description |
|------|-------------|
| UNAUTHORIZED | Authentication required or invalid token |
| LOVE_VIDEO_NOT_FOUND | Love video with given ID not found |
| ALREADY_APPROVED | Video is already in approved status |
| ALREADY_REJECTED | Video is already in rejected status |
| NOT_PENDING | Action requires pending status |
| INTERNAL_ERROR | Unexpected server error |

---

## Frontend Integration Notes

1. **List View**: Call `GET /pending` on page load, sort by `createdAt ASC` (oldest first)
2. **Detail View**: Call `GET /moderation/{id}` when admin clicks a row
3. **Approve**: Call `POST /{id}/approve`, refresh list on success
4. **Reject**: Require reason input, call `POST /{id}/reject`
5. **Confirmation**: Show modal before approve/reject actions
6. **Optimistic UI**: Update status locally, revert on error
