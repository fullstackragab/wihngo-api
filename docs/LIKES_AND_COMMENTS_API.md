# Like and Comment System API Documentation

## Overview

This document provides comprehensive documentation for the Like and Comment system endpoints. The system allows users to:
- Like stories
- Comment on stories
- Reply to comments (nested comments)
- Like comments

All authenticated endpoints require a valid JWT token in the `Authorization` header:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Table of Contents

1. [Story Likes](#story-likes)
2. [Comments](#comments)
3. [Comment Likes](#comment-likes)
4. [Database Schema](#database-schema)
5. [Error Responses](#error-responses)

---

## Story Likes

### 1. Get Story Likes

Retrieve all likes for a specific story with pagination.

**Endpoint:** `GET /api/likes/story/{storyId}`

**Authentication:** Optional (public endpoint)

**Parameters:**
- `storyId` (path, required): UUID of the story
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Items per page (default: 50, max: 100)

**Response:** `200 OK`
```json
{
  "page": 1,
  "pageSize": 50,
  "totalCount": 125,
  "items": [
    {
      "likeId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "userId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "userName": "John Doe",
      "userProfileImage": "users/profile123.jpg",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

### 2. Like a Story

Add a like to a story.

**Endpoint:** `POST /api/likes/story`

**Authentication:** Required

**Request Body:**
```json
{
  "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901"
}
```

**Response:** `201 Created`
```json
{
  "likeId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "userId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "userName": "John Doe",
  "userProfileImage": "users/profile123.jpg",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Story not found
- `409 Conflict`: User has already liked this story

---

### 3. Unlike a Story

Remove a like from a story.

**Endpoint:** `DELETE /api/likes/story/{storyId}`

**Authentication:** Required

**Parameters:**
- `storyId` (path, required): UUID of the story

**Response:** `204 No Content`

**Error Responses:**
- `404 Not Found`: Like not found or story not found

---

### 4. Check Story Like Status

Check if the current user has liked a specific story.

**Endpoint:** `GET /api/likes/story/{storyId}/check`

**Authentication:** Required

**Parameters:**
- `storyId` (path, required): UUID of the story

**Response:** `200 OK`

If liked:
```json
{
  "isLiked": true,
  "likeId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

If not liked:
```json
{
  "isLiked": false,
  "likeId": null,
  "createdAt": null
}
```

---

### 5. Get My Liked Stories

Retrieve all stories liked by the current user.

**Endpoint:** `GET /api/likes/my-likes`

**Authentication:** Required

**Parameters:**
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Items per page (default: 20, max: 100)

**Response:** `200 OK`
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 45,
  "items": [
    {
      "likeId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "userId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "userName": "John Doe",
      "userProfileImage": "users/profile123.jpg",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

## Comments

### 1. Get Story Comments

Retrieve all top-level comments for a story (does not include replies).

**Endpoint:** `GET /api/comments/story/{storyId}`

**Authentication:** Optional

**Parameters:**
- `storyId` (path, required): UUID of the story
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Items per page (default: 20, max: 100)

**Response:** `200 OK`
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 87,
  "items": [
    {
      "commentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
      "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "userId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "userName": "Jane Smith",
      "userProfileImage": "users/profile456.jpg",
      "content": "This is such a heartwarming story! ??",
      "createdAt": "2024-01-15T11:00:00Z",
      "updatedAt": null,
      "parentCommentId": null,
      "likeCount": 15,
      "isLikedByCurrentUser": true,
      "replyCount": 3
    }
  ]
}
```

---

### 2. Get Single Comment with Replies

Retrieve a single comment with all its nested replies.

**Endpoint:** `GET /api/comments/{commentId}`

**Authentication:** Optional

**Parameters:**
- `commentId` (path, required): UUID of the comment

**Response:** `200 OK`
```json
{
  "commentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
  "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "userId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "userName": "Jane Smith",
  "userProfileImage": "users/profile456.jpg",
  "content": "This is such a heartwarming story! ??",
  "createdAt": "2024-01-15T11:00:00Z",
  "updatedAt": null,
  "parentCommentId": null,
  "likeCount": 15,
  "isLikedByCurrentUser": true,
  "replies": [
    {
      "commentId": "e5f6a7b8-c9d0-1234-ef56-789abcdef012",
      "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "userId": "f6a7b8c9-d0e1-2345-f678-9abcdef01234",
      "userName": "Bob Johnson",
      "userProfileImage": "users/profile789.jpg",
      "content": "I agree! The bird is adorable!",
      "createdAt": "2024-01-15T11:15:00Z",
      "updatedAt": null,
      "parentCommentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
      "likeCount": 5,
      "isLikedByCurrentUser": false,
      "replyCount": 0
    }
  ]
}
```

---

### 3. Get Comment Replies

Retrieve replies for a specific comment with pagination.

**Endpoint:** `GET /api/comments/{commentId}/replies`

**Authentication:** Optional

**Parameters:**
- `commentId` (path, required): UUID of the parent comment
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Items per page (default: 20, max: 100)

**Response:** `200 OK`
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 12,
  "items": [
    {
      "commentId": "e5f6a7b8-c9d0-1234-ef56-789abcdef012",
      "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "userId": "f6a7b8c9-d0e1-2345-f678-9abcdef01234",
      "userName": "Bob Johnson",
      "userProfileImage": "users/profile789.jpg",
      "content": "I agree! The bird is adorable!",
      "createdAt": "2024-01-15T11:15:00Z",
      "updatedAt": null,
      "parentCommentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
      "likeCount": 5,
      "isLikedByCurrentUser": false,
      "replyCount": 0
    }
  ]
}
```

---

### 4. Create a Comment

Create a new comment on a story or reply to an existing comment.

**Endpoint:** `POST /api/comments`

**Authentication:** Required

**Request Body (Top-level comment):**
```json
{
  "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "content": "This is such a heartwarming story! ??",
  "parentCommentId": null
}
```

**Request Body (Reply to comment):**
```json
{
  "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "content": "I agree! The bird is adorable!",
  "parentCommentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0"
}
```

**Validation Rules:**
- `storyId`: Required, must be a valid story UUID
- `content`: Required, max 2000 characters, cannot be empty or whitespace
- `parentCommentId`: Optional, if provided must be a valid comment UUID belonging to the same story

**Response:** `201 Created`
```json
{
  "commentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
  "storyId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "userId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "userName": "Jane Smith",
  "userProfileImage": "users/profile456.jpg",
  "content": "This is such a heartwarming story! ??",
  "createdAt": "2024-01-15T11:00:00Z",
  "updatedAt": null,
  "parentCommentId": null,
  "likeCount": 0,
  "isLikedByCurrentUser": false,
  "replyCount": 0
}
```

**Error Responses:**
- `400 Bad Request`: Invalid request data or parent comment doesn't belong to the story
- `404 Not Found`: Story not found or parent comment not found

**Notifications:**
- If top-level comment: Story author receives a notification (unless commenting on own story)
- If reply: Parent comment author receives a notification (unless replying to self)

---

### 5. Update a Comment

Edit an existing comment's content.

**Endpoint:** `PUT /api/comments/{commentId}`

**Authentication:** Required (must be comment owner)

**Parameters:**
- `commentId` (path, required): UUID of the comment

**Request Body:**
```json
{
  "content": "Updated comment text with new thoughts! ??"
}
```

**Validation Rules:**
- `content`: Required, max 2000 characters, cannot be empty or whitespace
- Only the comment owner can update their comment

**Response:** `204 No Content`

**Error Responses:**
- `400 Bad Request`: Invalid request data
- `403 Forbidden`: User is not the comment owner
- `404 Not Found`: Comment not found

---

### 6. Delete a Comment

Delete a comment and all its replies (cascade delete).

**Endpoint:** `DELETE /api/comments/{commentId}`

**Authentication:** Required (must be comment owner)

**Parameters:**
- `commentId` (path, required): UUID of the comment

**Response:** `204 No Content`

**Error Responses:**
- `403 Forbidden`: User is not the comment owner
- `404 Not Found`: Comment not found

**Note:** Deleting a comment will also delete all replies to that comment.

---

## Comment Likes

### 1. Get Comment Likes

Retrieve all likes for a specific comment.

**Endpoint:** `GET /api/comments/{commentId}/likes`

**Authentication:** Optional

**Parameters:**
- `commentId` (path, required): UUID of the comment
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Items per page (default: 50, max: 100)

**Response:** `200 OK`
```json
{
  "page": 1,
  "pageSize": 50,
  "totalCount": 28,
  "items": [
    {
      "likeId": "f6a7b8c9-d0e1-2345-f678-9abcdef01234",
      "commentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
      "userId": "a7b8c9d0-e1f2-3456-789a-bcdef0123456",
      "userName": "Alice Brown",
      "userProfileImage": "users/profile111.jpg",
      "createdAt": "2024-01-15T11:30:00Z"
    }
  ]
}
```

---

### 2. Like a Comment

Add a like to a comment.

**Endpoint:** `POST /api/comments/{commentId}/like`

**Authentication:** Required

**Parameters:**
- `commentId` (path, required): UUID of the comment

**Response:** `201 Created`
```json
{
  "likeId": "f6a7b8c9-d0e1-2345-f678-9abcdef01234",
  "commentId": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
  "userId": "a7b8c9d0-e1f2-3456-789a-bcdef0123456",
  "userName": "Alice Brown",
  "userProfileImage": "users/profile111.jpg",
  "createdAt": "2024-01-15T11:30:00Z"
}
```

**Error Responses:**
- `404 Not Found`: Comment not found
- `409 Conflict`: User has already liked this comment

**Notifications:**
- Comment author receives a notification (unless liking own comment)

---

### 3. Unlike a Comment

Remove a like from a comment.

**Endpoint:** `DELETE /api/comments/{commentId}/like`

**Authentication:** Required

**Parameters:**
- `commentId` (path, required): UUID of the comment

**Response:** `204 No Content`

**Error Responses:**
- `404 Not Found`: Like not found or comment not found

---

## Database Schema

### Tables

#### `story_likes`
Stores likes on stories.

| Column | Type | Description |
|--------|------|-------------|
| `like_id` | UUID | Primary key |
| `story_id` | UUID | Foreign key to stories table |
| `user_id` | UUID | Foreign key to users table |
| `created_at` | TIMESTAMP | When the like was created |

**Indexes:**
- Primary key on `like_id`
- Unique constraint on `(story_id, user_id)` - prevents duplicate likes
- Index on `story_id` for efficient queries
- Index on `user_id` for user-specific queries
- Index on `created_at` DESC for chronological ordering

---

#### `comments`
Stores comments on stories with nested reply support.

| Column | Type | Description |
|--------|------|-------------|
| `comment_id` | UUID | Primary key |
| `story_id` | UUID | Foreign key to stories table |
| `user_id` | UUID | Foreign key to users table |
| `content` | TEXT | Comment text (max 2000 chars) |
| `created_at` | TIMESTAMP | When the comment was created |
| `updated_at` | TIMESTAMP | When the comment was last edited (nullable) |
| `parent_comment_id` | UUID | Foreign key to parent comment (nullable) |
| `like_count` | INTEGER | Cached count of likes |

**Indexes:**
- Primary key on `comment_id`
- Index on `story_id` for story-specific queries
- Index on `user_id` for user-specific queries
- Index on `parent_comment_id` for fetching replies
- Composite index on `(story_id, created_at)` for paginated story comments
- Index on `created_at` DESC for chronological ordering

**Constraints:**
- Content cannot be empty or whitespace
- Content max length: 2000 characters

---

#### `comment_likes`
Stores likes on comments.

| Column | Type | Description |
|--------|------|-------------|
| `like_id` | UUID | Primary key |
| `comment_id` | UUID | Foreign key to comments table |
| `user_id` | UUID | Foreign key to users table |
| `created_at` | TIMESTAMP | When the like was created |

**Indexes:**
- Primary key on `like_id`
- Unique constraint on `(comment_id, user_id)` - prevents duplicate likes
- Index on `comment_id` for efficient queries
- Index on `user_id` for user-specific queries
- Index on `created_at` DESC for chronological ordering

---

### Database Triggers

The system uses PostgreSQL triggers to maintain cached counts:

1. **Story Like Count Triggers:**
   - `trg_increment_story_like_count`: Increments `stories.like_count` when a like is added
   - `trg_decrement_story_like_count`: Decrements `stories.like_count` when a like is removed

2. **Story Comment Count Triggers:**
   - `trg_increment_story_comment_count`: Increments `stories.comment_count` for top-level comments
   - `trg_decrement_story_comment_count`: Decrements `stories.comment_count` for top-level comments

3. **Comment Like Count Triggers:**
   - `trg_increment_comment_like_count`: Increments `comments.like_count` when a like is added
   - `trg_decrement_comment_like_count`: Decrements `comments.like_count` when a like is removed

---

### New Story Columns

The following columns have been added to the `stories` table:

| Column | Type | Description |
|--------|------|-------------|
| `like_count` | INTEGER | Cached count of story likes (default: 0) |
| `comment_count` | INTEGER | Cached count of top-level comments (default: 0) |

**Note:** Only top-level comments are counted in `comment_count`. Replies are not included in this count.

---

## Error Responses

All endpoints follow consistent error response formats:

### 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Content": ["The Content field is required."]
  }
}
```

### 401 Unauthorized
```json
{
  "message": "Authentication required",
  "code": "UNAUTHORIZED"
}
```

### 403 Forbidden
```json
{
  "message": "You do not have permission to perform this action"
}
```

### 404 Not Found
```json
{
  "message": "Story not found"
}
```

### 409 Conflict
```json
{
  "message": "You have already liked this story",
  "likeId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### 500 Internal Server Error
```json
{
  "message": "An unexpected error occurred. Please try again."
}
```

---

## Integration Examples

### Example 1: Display Story with Like/Comment Stats

```javascript
// Fetch story details
const response = await fetch(`/api/stories/${storyId}`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
const story = await response.json();

// Story object now includes:
// - story.likeCount: Total number of likes
// - story.commentCount: Total number of top-level comments

// Check if current user liked the story
const likeCheckResponse = await fetch(`/api/likes/story/${storyId}/check`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
const likeStatus = await likeCheckResponse.json();

// Display UI:
// - Show like count
// - Show comment count
// - Show filled/outlined heart based on likeStatus.isLiked
```

---

### Example 2: Toggle Story Like

```javascript
async function toggleStoryLike(storyId, isCurrentlyLiked) {
  if (isCurrentlyLiked) {
    // Unlike
    await fetch(`/api/likes/story/${storyId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
  } else {
    // Like
    await fetch(`/api/likes/story`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ storyId })
    });
  }
  
  // Refresh UI
  updateStoryDisplay();
}
```

---

### Example 3: Display Comments with Nested Replies

```javascript
// Fetch top-level comments
const commentsResponse = await fetch(
  `/api/comments/story/${storyId}?page=1&pageSize=20`,
  {
    headers: {
      'Authorization': `Bearer ${token}` // Optional
    }
  }
);
const commentsData = await commentsResponse.json();

// For each comment, you can:
// 1. Display the comment
// 2. Show comment.replyCount
// 3. Load replies on demand using:
//    GET /api/comments/{commentId}/replies
// OR
// 4. Fetch full comment with replies:
//    GET /api/comments/{commentId}
```

---

### Example 4: Post a Comment

```javascript
// Top-level comment
async function postComment(storyId, content) {
  const response = await fetch('/api/comments', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      storyId,
      content,
      parentCommentId: null
    })
  });
  
  if (response.ok) {
    const newComment = await response.json();
    // Add comment to UI
    displayComment(newComment);
  }
}

// Reply to a comment
async function replyToComment(storyId, parentCommentId, content) {
  const response = await fetch('/api/comments', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      storyId,
      content,
      parentCommentId
    })
  });
  
  if (response.ok) {
    const newReply = await response.json();
    // Add reply to parent comment in UI
    displayReply(parentCommentId, newReply);
  }
}
```

---

### Example 5: Edit/Delete Comment

```javascript
// Edit comment
async function editComment(commentId, newContent) {
  const response = await fetch(`/api/comments/${commentId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      content: newContent
    })
  });
  
  if (response.ok) {
    // Update UI
    updateCommentDisplay(commentId, newContent);
  }
}

// Delete comment
async function deleteComment(commentId) {
  const response = await fetch(`/api/comments/${commentId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (response.ok) {
    // Remove comment from UI
    removeCommentFromDisplay(commentId);
  }
}
```

---

### Example 6: Like/Unlike Comment

```javascript
async function toggleCommentLike(commentId, isCurrentlyLiked) {
  if (isCurrentlyLiked) {
    // Unlike
    await fetch(`/api/comments/${commentId}/like`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
  } else {
    // Like
    await fetch(`/api/comments/${commentId}/like`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
  }
  
  // Refresh comment display
  refreshComment(commentId);
}
```

---

## Best Practices

### 1. Pagination
Always use pagination for lists to avoid loading too much data:
- Default page size is reasonable (20-50 items)
- Don't exceed max page size limits
- Show "Load More" or pagination controls in your UI

### 2. Optimistic UI Updates
For better UX, update the UI immediately and rollback if the API call fails:

```javascript
// Optimistic like
setIsLiked(true);
setLikeCount(likeCount + 1);

try {
  await likeStory(storyId);
} catch (error) {
  // Rollback on error
  setIsLiked(false);
  setLikeCount(likeCount - 1);
  showError('Failed to like story');
}
```

### 3. Real-time Updates
Consider implementing WebSocket or polling for real-time like/comment updates in active feeds.

### 4. Caching
Use cached counts (`like_count`, `comment_count`) instead of fetching full lists for display:
- Stories endpoint returns these counts
- Only fetch detailed lists when user explicitly requests them

### 5. Nested Comments
For deeply nested comment threads:
- Load only 2-3 levels initially
- Provide "Show more replies" button for deeper nesting
- Consider flattening very deep threads in UI

### 6. Content Validation
Client-side validation before API calls:
- Check content length (max 2000 characters)
- Trim whitespace
- Prevent empty submissions

### 7. Error Handling
Handle all error cases:
- Network errors
- Authentication expiration (401)
- Validation errors (400)
- Permission errors (403)
- Not found errors (404)
- Conflict errors (409)

---

## Rate Limiting

Be mindful of API rate limits:
- Implement debouncing for like/unlike actions
- Cache results where appropriate
- Use pagination to reduce data transfer
- Implement request batching if supported

---

## Support

For issues or questions, please contact the backend team or refer to the main API documentation.

**Last Updated:** January 2024
**API Version:** 1.0
