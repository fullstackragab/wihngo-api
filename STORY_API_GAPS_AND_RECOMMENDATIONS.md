# Story API - Implementation Gaps & Recommendations

## ?? Summary

This document outlines the differences between the mobile team's requirements and the current backend implementation, along with recommendations for addressing these gaps.

---

## ? What's Fully Implemented

### Core Story Features
- ? **Pagination** - Fully working with `page`, `pageSize`, and `totalCount`
- ? **Story CRUD** - Create, Read, Update, Delete all implemented
- ? **Multiple Birds** - Stories can be associated with multiple birds
- ? **Story Modes** - All 8 mood types supported (optional field)
- ? **Media Support** - Image and video support with S3 pre-signed URLs
- ? **One Media Type Rule** - Backend enforces image OR video (not both)
- ? **Media Validation** - Checks if files exist in S3 before creating story
- ? **Author Validation** - Only story author can update/delete
- ? **User Stories** - Get stories by user ID and current user

### Data Quality
- ? Ordered by creation date (newest first)
- ? Pre-signed URLs with 10-minute expiration
- ? Content preview truncated to 140 characters
- ? Author information included (userId, name)
- ? Bird details included with each story

---

## ? What's Missing (Mobile Requirements)

### 1. Social Features (Critical Gap)

#### Likes/Reactions System
**Status:** ? Not Implemented

**Mobile Requirements:**
- `POST /api/stories/{storyId}/like` - Like a story
- `DELETE /api/stories/{storyId}/like` - Unlike a story
- `likes` (number) - Like count in responses
- `isLiked` (boolean) - Current user's like status

**Current Implementation:**
- No like endpoints exist
- No like count in responses
- No database tables for likes

**Impact:** High - Social engagement is core feature

---

#### Comments System
**Status:** ? Not Implemented

**Mobile Requirements:**
- `POST /api/stories/{storyId}/comments` - Add comment
- `GET /api/stories/{storyId}/comments` - Get comments
- `commentsCount` (number) - Comment count in responses
- `comments` array in story detail

**Current Implementation:**
- No comment endpoints exist
- No comment count in responses
- No database tables for comments

**Impact:** High - User interaction essential

---

### 2. User Profile Data (Medium Gap)

#### User Avatar
**Status:** ? Not Implemented

**Mobile Requirements:**
```json
{
  "userName": "string",
  "userId": "string",
  "userAvatar": "string (optional)"
}
```

**Current Implementation:**
```json
{
  "userId": "guid",
  "name": "string"
}
```

**Impact:** Medium - UX quality issue, not blocking

---

### 3. Response Format Differences (Low Gap)

#### Missing `hasMore` Field
**Status:** ?? Can Be Calculated Client-Side

**Mobile Requirements:**
```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10,
  "hasMore": true
}
```

**Current Implementation:**
```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10
}
```

**Calculation:**
```typescript
hasMore = (page * pageSize) < totalCount
```

**Impact:** Low - Easy workaround

---

#### Field Name Inconsistencies
**Status:** ?? Minor Naming Differences

| Mobile Expectation | Current Implementation | Impact |
|-------------------|------------------------|--------|
| `userName` | `name` (in Author object) | Low |
| `imageUrl` | ? `imageUrl` | None |
| `videoUrl` | ? `videoUrl` | None |
| `title` | ? Not supported | Low |

**Note:** Mobile docs show `title` as optional for backward compatibility - not needed for new app

---

## ?? Recommendations

### Priority 1: Social Features (Must Have)

#### Implement Likes System

**Database Changes:**
```sql
CREATE TABLE story_likes (
    story_like_id UUID PRIMARY KEY,
    story_id UUID NOT NULL REFERENCES stories(story_id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(story_id, user_id)
);

CREATE INDEX idx_story_likes_story ON story_likes(story_id);
CREATE INDEX idx_story_likes_user ON story_likes(user_id);
```

**New Endpoints:**
```
POST   /api/stories/{storyId}/like    (Toggle or add like)
DELETE /api/stories/{storyId}/like    (Remove like)
GET    /api/stories/{storyId}/likes   (Get list of likers - optional)
```

**Update DTOs:**
```csharp
// Add to StorySummaryDto and StoryReadDto
public int LikesCount { get; set; }
public bool IsLiked { get; set; }  // Requires authenticated user context
```

---

#### Implement Comments System

**Database Changes:**
```sql
CREATE TABLE story_comments (
    comment_id UUID PRIMARY KEY,
    story_id UUID NOT NULL REFERENCES stories(story_id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_story_comments_story ON story_comments(story_id);
CREATE INDEX idx_story_comments_user ON story_comments(user_id);
```

**New Endpoints:**
```
POST   /api/stories/{storyId}/comments         (Create comment)
GET    /api/stories/{storyId}/comments         (Get paginated comments)
PUT    /api/stories/{storyId}/comments/{id}    (Update own comment)
DELETE /api/stories/{storyId}/comments/{id}    (Delete own comment)
```

**Update DTOs:**
```csharp
// Add to StorySummaryDto and StoryReadDto
public int CommentsCount { get; set; }

// New DTO
public class StoryCommentDto
{
    public Guid CommentId { get; set; }
    public string Content { get; set; }
    public string UserName { get; set; }
    public Guid UserId { get; set; }
    public string? UserAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### Priority 2: User Profile Enhancements

#### Add User Avatar Support

**Database Changes:**
```sql
ALTER TABLE users ADD COLUMN avatar_url TEXT;
```

**Update DTOs:**
```csharp
public class UserSummaryDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }  // Add this
}
```

**Controller Changes:**
- Update all endpoints returning `UserSummaryDto` to include avatar
- Generate pre-signed URLs for avatars if they're S3 keys

---

### Priority 3: Response Format Improvements (Optional)

#### Add `hasMore` to PagedResult

**Update DTO:**
```csharp
public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public bool HasMore => (Page * PageSize) < TotalCount;  // Add computed property
    public IEnumerable<T> Items { get; set; } = new List<T>();
}
```

---

## ?? Implementation Roadmap

### Phase 1: Social Features (2-3 weeks)
1. **Week 1:**
   - Create database migrations for likes and comments
   - Implement likes endpoints
   - Update story DTOs to include like counts
   - Write unit tests

2. **Week 2:**
   - Implement comments CRUD endpoints
   - Add pagination to comments
   - Update story DTOs to include comment counts
   - Write unit tests

3. **Week 3:**
   - Integration testing
   - Performance optimization
   - Documentation updates

### Phase 2: User Profiles (1 week)
1. Add avatar column to users table
2. Update UserSummaryDto
3. Update all endpoints returning user data
4. Media upload support for avatars

### Phase 3: Polish (3-5 days)
1. Add `hasMore` computed property
2. Add any missing indexes
3. Performance testing
4. Final documentation

---

## ?? Testing Requirements

### Likes System Tests
- [ ] User can like a story
- [ ] User can unlike a story
- [ ] User cannot like the same story twice
- [ ] Like count updates correctly
- [ ] `isLiked` reflects current user's status
- [ ] Deleting a story deletes all likes
- [ ] Unauthorized users cannot like

### Comments System Tests
- [ ] User can create comment
- [ ] User can update own comment
- [ ] User can delete own comment
- [ ] User cannot update/delete others' comments
- [ ] Comments are paginated correctly
- [ ] Comment count updates correctly
- [ ] Deleting a story deletes all comments

---

## ?? Breaking Changes

**None** - All recommendations are additive and won't break existing mobile app functionality.

The mobile app can:
1. Continue using current endpoints as-is
2. Show placeholder UI for likes/comments
3. Implement new features when backend is ready

---

## ?? Quick Wins (Can Implement Now)

### 1. Add `hasMore` Property
- **Time:** 10 minutes
- **Impact:** Removes calculation from mobile
- **Risk:** None

### 2. Add User Avatar Field
- **Time:** 1 hour (DB + code)
- **Impact:** Better UX
- **Risk:** Low

### 3. Add Indexes
```sql
-- Improve story listing performance
CREATE INDEX idx_stories_created_at ON stories(created_at DESC);
CREATE INDEX idx_stories_author ON stories(author_id);
```

---

## ?? Next Steps

### For Backend Team
1. Review this document
2. Prioritize missing features
3. Create tickets for implementation
4. Estimate timeline
5. Communicate to mobile team

### For Mobile Team
1. Use current implementation guide
2. Show placeholders for likes/comments
3. Plan UI for these features
4. Wait for backend updates
5. Implement when endpoints are ready

---

## ?? Related Documents
- [MOBILE_STORY_API_IMPLEMENTATION_GUIDE.md](./MOBILE_STORY_API_IMPLEMENTATION_GUIDE.md) - Full API documentation
- Backend requirements from mobile team (in prompt)

---

## ?? Document Info

**Created:** December 25, 2024  
**Author:** Backend Team  
**Status:** Ready for Review  
**Next Review:** After Phase 1 completion
