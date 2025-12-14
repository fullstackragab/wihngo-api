# Story API - Quick Reference Card

## ?? Base URL
```
https://localhost:7297/api
```

---

## ?? Endpoints Overview

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/stories` | ? | Get all stories (paginated) |
| `GET` | `/stories/{id}` | ? | Get story by ID |
| `GET` | `/stories/user/{userId}` | ? | Get user's stories (paginated) |
| `GET` | `/stories/my-stories` | ? | Get current user's stories |
| `POST` | `/stories` | ? | Create new story |
| `PUT` | `/stories/{id}` | ? | Update story |
| `DELETE` | `/stories/{id}` | ? | Delete story |

---

## ?? Authentication

```
Authorization: Bearer {jwt_token}
```

---

## ?? Common Request Examples

### Get Stories (Paginated)
```bash
GET /api/stories?page=1&pageSize=10
```

### Create Story
```json
POST /api/stories
Authorization: Bearer {token}

{
  "content": "My bird's story...",
  "imageS3Key": "stories/abc123.jpg",
  "mode": "FunnyMoment",
  "birdIds": ["bird-guid-1", "bird-guid-2"]
}
```

### Update Story (Partial)
```json
PUT /api/stories/{id}
Authorization: Bearer {token}

{
  "content": "Updated content"
}
```

---

## ?? Response Formats

### Story List Response
```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 45,
  "items": [...]
}
```

### Story Summary Item
```json
{
  "storyId": "guid",
  "birds": ["Bird1", "Bird2"],
  "mode": "FunnyMoment",
  "date": "December 25, 2024",
  "preview": "Story preview...",
  "imageUrl": "https://s3.../presigned-url",
  "videoUrl": null
}
```

### Story Detail
```json
{
  "storyId": "guid",
  "content": "Full content",
  "mode": "LoveAndBond",
  "imageUrl": "https://...",
  "videoUrl": null,
  "createdAt": "2024-12-25T10:30:00Z",
  "author": {
    "userId": "guid",
    "name": "Alice"
  },
  "birds": [...]
}
```

---

## ?? Story Modes

| Value | Display |
|-------|---------|
| `LoveAndBond` | ?? Love & Bond |
| `NewBeginning` | ?? New Beginning |
| `ProgressAndWins` | ?? Progress & Wins |
| `FunnyMoment` | ?? Funny Moment |
| `PeacefulMoment` | ??? Peaceful Moment |
| `LossAndMemory` | ??? Loss & Memory |
| `CareAndHealth` | ?? Care & Health |
| `DailyLife` | ?? Daily Life |

**Note:** Mode is optional (can be `null`)

---

## ??? Media Guidelines

### Images
- Max: **5MB**
- Formats: `.jpg`, `.png`, `.webp`
- Aspect: **4:5** (Instagram-style)

### Videos
- Max: **200MB** / **60 seconds**
- Formats: `.mp4`, `.mov`, `.m4v`
- Resolution: **720x1280** (9:16 vertical)

### Important Rules
- ? Image **OR** Video (not both)
- ? Upload to S3 first, then use S3 key
- ? Pre-signed URLs expire in 10 minutes

---

## ?? Media Upload Flow

### 1. Get Upload URL
```bash
POST /api/media/upload-url
Authorization: Bearer {token}

{
  "fileName": "photo.jpg",
  "contentType": "image/jpeg",
  "category": "story"
}
```

### 2. Upload to S3
```bash
PUT {uploadUrl from step 1}
Content-Type: image/jpeg
Body: [binary file]
```

### 3. Create Story
```bash
POST /api/stories
{
  "imageS3Key": "{s3Key from step 1}",
  "content": "...",
  "birdIds": [...]
}
```

---

## ? Validation Rules

### Create/Update Story
- ? `content` required, max 5000 chars
- ? At least 1 bird required
- ? Only image **OR** video (not both)
- ? Media must exist in S3 first
- ? Mode is optional

---

## ?? Important Notes

### Media Handling
- Setting new image **removes** existing video
- Setting new video **removes** existing image
- To remove media: send empty string `""`
- Old media is **deleted** from S3 automatically

### Pagination
- Calculate `hasMore`: `(page * pageSize) < totalCount`
- Pages are 1-based (not 0-based)
- Stories ordered newest first

### Permissions
- Only author can update/delete story
- Anyone can view stories (no auth needed)

---

## ?? Not Yet Implemented

### Social Features
- ? Likes (count, isLiked, like/unlike)
- ? Comments (count, list, CRUD)
- ? User avatars

### Workarounds
- Show placeholder UI for likes/comments
- Display user initials instead of avatar
- Wait for backend implementation

---

## ? Common Errors

| Code | Meaning | Solution |
|------|---------|----------|
| `400` | Validation error | Check request format |
| `401` | Not authenticated | Add/refresh auth token |
| `403` | Not authorized | User doesn't own resource |
| `404` | Not found | Check story/bird ID |
| `500` | Server error | Retry or contact backend |

---

## ?? Test Credentials

```
Email: alice@example.com
Password: Password123!
```

---

## ?? Full Documentation

See [MOBILE_STORY_API_IMPLEMENTATION_GUIDE.md](./MOBILE_STORY_API_IMPLEMENTATION_GUIDE.md)

---

## ?? Pro Tips

1. **Cache S3 URLs** - They expire in 10 min
2. **Implement retry logic** - For 5xx errors
3. **Validate before upload** - File size/format
4. **Show loading states** - Especially for video
5. **Handle token expiry** - Auto-refresh flow
6. **Debounce API calls** - Avoid spam
7. **Prefetch next page** - Better UX

---

**Last Updated:** December 25, 2024
