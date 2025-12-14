# Mobile Story API Implementation Guide

## ?? Overview

This document provides a complete guide for the mobile team on how to consume the Wihngo Story API. It includes implementation status, API endpoints, request/response formats, and important notes.

---

## ? Implementation Status

### ? Fully Implemented Features
- ? Get all stories with pagination
- ? Get story by ID with full details
- ? Get stories by user ID (paginated)
- ? Get current user's stories (paginated)
- ? Create story with validation
- ? Update story
- ? Delete story
- ? Story mood/mode support (8 types)
- ? One media type enforcement (image OR video, not both)
- ? Multiple birds per story
- ? Pre-signed S3 URLs for media

### ?? Features NOT Yet Implemented
- ? Likes/reactions system
- ? Comments system
- ? User avatars (only name is returned)
- ? `hasMore` field in pagination response

---

## ?? API Endpoints

### Base URL
```
Development: https://localhost:7297/api
Production: [Your production URL]
```

---

## 1?? Get All Stories (Paginated)

### Endpoint
```http
GET /api/stories?page={page}&pageSize={pageSize}
```

### Query Parameters
| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| `page` | integer | 1 | No | Page number (1-based) |
| `pageSize` | integer | 10 | No | Items per page |

### Response Format
```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 45,
  "items": [
    {
      "storyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "birds": ["Tweety", "Charlie"],
      "mode": "FunnyMoment",
      "date": "December 25, 2024",
      "preview": "This is a story preview truncated to 140 characters if the content is longer than that...",
      "imageS3Key": "stories/abc123.jpg",
      "imageUrl": "https://s3.amazonaws.com/presigned-url-for-image",
      "videoS3Key": null,
      "videoUrl": null
    }
  ]
}
```

### Important Notes
- ? Stories are ordered by `createdAt` descending (newest first)
- ? Pre-signed URLs expire in 10 minutes
- ? Only ONE of `imageUrl` OR `videoUrl` will be present
- ? `preview` is truncated to 140 characters
- ?? `hasMore` field is NOT included (calculate from `totalCount`)

### Calculating `hasMore`
```typescript
const hasMore = (page * pageSize) < totalCount;
```

---

## 2?? Get Story Detail

### Endpoint
```http
GET /api/stories/{storyId}
```

### Response Format
```json
{
  "storyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "Full story content here...",
  "mode": "LoveAndBond",
  "imageS3Key": "stories/abc123.jpg",
  "imageUrl": "https://s3.amazonaws.com/presigned-url-for-image",
  "videoS3Key": null,
  "videoUrl": null,
  "createdAt": "2024-12-25T10:30:00Z",
  "author": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Alice Johnson"
  },
  "birds": [
    {
      "birdId": "456e4567-e89b-12d3-a456-426614174001",
      "name": "Tweety",
      "species": "Canary",
      "imageS3Key": "birds/tweety123.jpg",
      "imageUrl": "https://s3.amazonaws.com/presigned-url-for-bird",
      "videoS3Key": null,
      "videoUrl": null,
      "tagline": "A sweet singing canary",
      "lovedBy": 42,
      "supportedBy": 15,
      "ownerId": "123e4567-e89b-12d3-a456-426614174000"
    }
  ]
}
```

### Important Notes
- ?? `likes`, `commentsCount`, `isLiked`, `comments` are NOT implemented yet
- ?? Author only includes `userId` and `name` (no avatar)
- ? Bird images and videos also have pre-signed URLs

---

## 3?? Get Stories by User

### Endpoint
```http
GET /api/stories/user/{userId}?page={page}&pageSize={pageSize}
```

### Path Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `userId` | GUID | Yes | User ID to get stories for |

### Query Parameters
Same as "Get All Stories"

### Response Format
Same as "Get All Stories"

---

## 4?? Get My Stories (Current User)

### Endpoint
```http
GET /api/stories/my-stories?page={page}&pageSize={pageSize}
```

### Headers
```
Authorization: Bearer {jwt_token}
```

### Response Format
Same as "Get All Stories"

### Error Responses
- `401 Unauthorized` - Missing or invalid auth token

---

## 5?? Create Story

### Endpoint
```http
POST /api/stories
```

### Headers
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Request Body
```json
{
  "content": "Story content here (required, max 5000 chars)",
  "imageS3Key": "stories/uploaded-image-key.jpg",
  "videoS3Key": null,
  "mode": "FunnyMoment",
  "birdIds": [
    "456e4567-e89b-12d3-a456-426614174001",
    "789e4567-e89b-12d3-a456-426614174002"
  ]
}
```

### Validation Rules
- ? `content` is **required** and cannot be empty
- ? `birdIds` must contain **at least 1 bird ID**
- ? Only ONE of `imageS3Key` OR `videoS3Key` should be provided (not both)
- ? `mode` is optional (can be null)
- ? Media files must be uploaded to S3 BEFORE creating story (see Media Upload section)

### Success Response (201 Created)
```json
{
  "storyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "Story content",
  "mode": "FunnyMoment",
  "imageS3Key": "stories/uploaded-image-key.jpg",
  "imageUrl": "https://s3.amazonaws.com/presigned-url",
  "videoS3Key": null,
  "videoUrl": null,
  "createdAt": "2024-12-25T10:30:00Z",
  "author": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Alice Johnson"
  },
  "birds": [...]
}
```

### Error Responses
```json
// 400 Bad Request - Both media types provided
{
  "message": "Story can have either an image or a video, not both"
}

// 400 Bad Request - No birds selected
{
  "message": "At least one bird must be selected"
}

// 400 Bad Request - Image not found in S3
{
  "message": "Story image not found in S3. Please upload the file first."
}

// 404 Not Found - Invalid bird IDs
{
  "message": "Some birds not found",
  "missingBirdIds": ["invalid-guid-1", "invalid-guid-2"]
}
```

---

## 6?? Update Story

### Endpoint
```http
PUT /api/stories/{storyId}
```

### Headers
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Request Body
```json
{
  "content": "Updated story content",
  "imageS3Key": "stories/new-image.jpg",
  "videoS3Key": null,
  "mode": "PeacefulMoment",
  "birdIds": [
    "456e4567-e89b-12d3-a456-426614174001"
  ]
}
```

### Important Notes
- ? All fields in the request body are **optional** (partial updates supported)
- ? If you provide a new `imageS3Key`, it will **delete the old image** from S3
- ? If you provide a new `videoS3Key`, it will **delete the old video** from S3
- ? Setting a new image will **automatically remove** any existing video
- ? Setting a new video will **automatically remove** any existing image
- ? To remove media: send empty string `""` for the key field
- ? Only the story author can update their story

### Success Response (204 No Content)
No body returned

### Error Responses
```json
// 401 Unauthorized
{
  "message": "Authentication required",
  "code": "UNAUTHORIZED"
}

// 403 Forbidden - Not the author
{
  "message": "Forbidden"
}

// 404 Not Found
{
  "message": "Story not found"
}

// 400 Bad Request
{
  "message": "Story content cannot be empty"
}
```

---

## 7?? Delete Story

### Endpoint
```http
DELETE /api/stories/{storyId}
```

### Headers
```
Authorization: Bearer {jwt_token}
```

### Success Response (204 No Content)
No body returned

### Important Notes
- ? Automatically deletes associated media files from S3
- ? Only the story author can delete their story

### Error Responses
- `401 Unauthorized` - Missing/invalid auth token
- `403 Forbidden` - User is not the story author
- `404 Not Found` - Story doesn't exist

---

## ?? Story Mood Types

The `mode` field supports the following values:

| Value | Display Name | Emoji |
|-------|-------------|-------|
| `LoveAndBond` | Love & Bond | ?? |
| `NewBeginning` | New Beginning | ?? |
| `ProgressAndWins` | Progress & Wins | ?? |
| `FunnyMoment` | Funny Moment | ?? |
| `PeacefulMoment` | Peaceful Moment | ??? |
| `LossAndMemory` | Loss & Memory | ??? |
| `CareAndHealth` | Care & Health | ?? |
| `DailyLife` | Daily Life | ?? |

**Note:** `mode` is optional and can be `null`

---

## ?? Media Upload Flow

### Step 1: Get Upload URL
```http
POST /api/media/upload-url
Content-Type: application/json
Authorization: Bearer {jwt_token}

{
  "fileName": "my-story-image.jpg",
  "contentType": "image/jpeg",
  "category": "story"
}
```

### Response
```json
{
  "uploadUrl": "https://s3.amazonaws.com/presigned-upload-url",
  "s3Key": "stories/abc123-my-story-image.jpg",
  "expiresInMinutes": 10
}
```

### Step 2: Upload File to S3
```http
PUT {uploadUrl}
Content-Type: {contentType}
Body: [binary file data]
```

### Step 3: Create Story with S3 Key
```http
POST /api/stories
{
  "content": "My story",
  "imageS3Key": "stories/abc123-my-story-image.jpg",
  "birdIds": [...]
}
```

---

## ?? Media File Guidelines

### Images
- **Max Size:** 5MB
- **Formats:** `.jpg`, `.png`, `.webp`
- **Recommended Aspect Ratio:** 4:5 (Instagram-style)

### Videos
- **Max Size:** 200MB
- **Max Duration:** 60 seconds
- **Formats:** `.mp4`, `.mov`, `.m4v`
- **Recommended Resolution:** 720p vertical (720x1280)
- **Aspect Ratio:** 9:16 (portrait/vertical)

### Important Notes
- ? Pre-signed URLs expire in 10 minutes - cache locally if needed
- ? Videos autoplay in feed when 60% visible (muted)
- ? Only one video plays at a time in feed
- ? Image serves as video thumbnail when video is not playing

---

## ?? Authentication

All authenticated endpoints require a JWT token in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Getting a Token
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

### Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2024-12-25T23:59:59Z",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Alice Johnson",
  "email": "user@example.com",
  "emailConfirmed": true
}
```

---

## ? Error Response Format

All errors follow this consistent format:

```json
{
  "message": "Error description",
  "code": "ERROR_CODE"
}
```

### Common HTTP Status Codes
- `200 OK` - Success
- `201 Created` - Resource created successfully
- `204 No Content` - Success with no response body
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Missing/invalid auth token
- `403 Forbidden` - No permission
- `404 Not Found` - Resource doesn't exist
- `500 Internal Server Error` - Server error

---

## ?? Pagination Implementation Example

### TypeScript/JavaScript
```typescript
interface PaginationState {
  page: number;
  pageSize: number;
  totalCount: number;
  hasMore: boolean;
}

function calculateHasMore(page: number, pageSize: number, totalCount: number): boolean {
  return (page * pageSize) < totalCount;
}

// Usage
const response = await fetch('/api/stories?page=1&pageSize=10');
const data = await response.json();

const pagination: PaginationState = {
  page: data.page,
  pageSize: data.pageSize,
  totalCount: data.totalCount,
  hasMore: calculateHasMore(data.page, data.pageSize, data.totalCount)
};
```

### Infinite Scroll Implementation
```typescript
async function loadMoreStories() {
  if (!pagination.hasMore || isLoading) return;
  
  setIsLoading(true);
  const nextPage = pagination.page + 1;
  
  const response = await fetch(`/api/stories?page=${nextPage}&pageSize=10`);
  const data = await response.json();
  
  setStories([...stories, ...data.items]);
  setPagination({
    page: data.page,
    pageSize: data.pageSize,
    totalCount: data.totalCount,
    hasMore: calculateHasMore(data.page, data.pageSize, data.totalCount)
  });
  
  setIsLoading(false);
}
```

---

## ?? Features Coming Soon

The following features are mentioned in the requirements but **NOT yet implemented**:

### Likes/Reactions System
- `POST /api/stories/{storyId}/like` - Not available
- `DELETE /api/stories/{storyId}/like` - Not available
- `likes` count in response - Returns 0
- `isLiked` boolean - Not included

### Comments System
- `POST /api/stories/{storyId}/comments` - Not available
- `GET /api/stories/{storyId}/comments` - Not available
- `commentsCount` in response - Returns 0
- `comments` array in story detail - Not included

### User Profiles
- User `avatar` field - Not available
- Only `userId` and `name` are returned

**Recommendation:** Display placeholders for these features in your UI with "Coming Soon" badges.

---

## ?? Testing

### Test Credentials (Development Only)
```
Email: alice@example.com
Password: Password123!
```

### Example API Calls

#### Get All Stories
```bash
curl -X GET "https://localhost:7297/api/stories?page=1&pageSize=10"
```

#### Get Story by ID
```bash
curl -X GET "https://localhost:7297/api/stories/{story-id}"
```

#### Create Story
```bash
curl -X POST "https://localhost:7297/api/stories" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Test story",
    "mode": "DailyLife",
    "birdIds": ["{bird-id}"]
  }'
```

---

## ?? Implementation Checklist for Mobile Team

- [ ] Implement pagination with infinite scroll
- [ ] Calculate `hasMore` from `page`, `pageSize`, and `totalCount`
- [ ] Cache pre-signed S3 URLs (10-minute expiration)
- [ ] Implement media upload flow (get URL ? upload ? use S3 key)
- [ ] Handle one media type rule (image OR video, not both)
- [ ] Display mood/mode icons with proper labels
- [ ] Show placeholders for likes/comments (coming soon)
- [ ] Implement video autoplay on 60% visibility
- [ ] Ensure only one video plays at a time
- [ ] Display user names (avatars coming later)
- [ ] Handle all error responses gracefully
- [ ] Implement pull-to-refresh (reset to page 1)
- [ ] Add file size/format validation before upload
- [ ] Test with expired tokens (auto-refresh flow)

---

## ?? Support & Questions

For questions or issues with the API:
1. Check this documentation first
2. Review error messages carefully
3. Contact the backend team with:
   - Endpoint being called
   - Request body/headers
   - Error response received
   - Expected behavior

---

## ?? Last Updated

**Date:** December 25, 2024  
**API Version:** 1.0  
**Document Version:** 1.0

---

## ?? Changelog

### Version 1.0 (December 25, 2024)
- Initial documentation
- All core story endpoints documented
- Media upload flow documented
- Known limitations identified
- Implementation examples added
