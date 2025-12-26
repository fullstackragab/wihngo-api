# Story Edit Endpoint Fix - Title vs Content Clarification

## Problem Identified

The story edit endpoint was confusing because:
1. **Stories don't have a separate `Title` field** in the database
2. The `Title` seen in story lists is **auto-generated** from the first 30 characters of `Content`
3. Users might expect to edit "title" and "content" separately, but they're actually the same field

## Database Schema

The `Story` model only has these content-related fields:

```csharp
public class Story
{
    public Guid StoryId { get; set; }
    public Guid BirdId { get; set; }
    public Guid AuthorId { get; set; }
    
    // Content - This is the full story text
    // The "title" is derived from the first 30 chars of this field
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;
    
    // Optional image
    [MaxLength(1000)]
    public string? ImageUrl { get; set; }  // S3 key
    
    public DateTime CreatedAt { get; set; }
    public bool IsHighlighted { get; set; }
    public int? HighlightOrder { get; set; }
}
```

## How Title is Generated

Looking at the `StoriesController` GET endpoints:

```csharp
var dto = new StorySummaryDto
{
    StoryId = story.StoryId,
    // Title is truncated content (first 30 chars)
    Title = story.Content.Length > 30 
        ? story.Content.Substring(0, 30) + "..." 
        : story.Content,
    Bird = story.Bird?.Name ?? string.Empty,
    Date = story.CreatedAt.ToString("MMMM d, yyyy"),
    // Preview is truncated content (first 140 chars)
    Preview = story.Content.Length > 140 
        ? story.Content.Substring(0, 140) + "..." 
        : story.Content,
    ImageS3Key = story.ImageUrl
};
```

## Edit Story Endpoint

### Request
```http
PUT /api/stories/{storyId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "Full story content here...",  // This updates BOTH title and content
  "imageS3Key": "users/stories/.../image.jpg"  // Optional
}
```

### Fields

| Field | Required | Description |
|-------|----------|-------------|
| `content` | Optional* | The full story text (max 5000 chars). The first 30 chars become the title |
| `imageS3Key` | Optional | S3 key for the story image. Set to `null` or `""` to remove image |

*At least one field must be provided

### Behavior

#### Content Update
```json
{
  "content": "My Amazing Bird Story - Today I saw the most beautiful cardinal..."
}
```
Result:
- `Title` (auto-generated): "My Amazing Bird Story - Today..."
- `Preview` (auto-generated): First 140 chars
- `Content` (stored): Full text

#### Image Update
```json
{
  "imageS3Key": "users/stories/123/456/image.jpg"
}
```
- Validates image exists in S3
- Deletes old image if one exists
- Updates to new image

#### Image Removal
```json
{
  "imageS3Key": ""
}
```
- Deletes current image from S3
- Sets image to null

#### Both Updates
```json
{
  "content": "Updated story content...",
  "imageS3Key": "users/stories/123/456/new-image.jpg"
}
```

## Changes Made

### 1. Enhanced Logging in `StoriesController.Put()`
Added comprehensive logging to track:
- Request received with content length
- Current story state
- Content updates
- Image updates/removals
- Success/failure outcomes

### 2. Improved Validation
- Added check for empty content strings
- Better error messages for S3 image validation
- Clear logging of ownership validation

### 3. Enabled Logging in `Program.cs`
```csharp
builder.Logging.AddFilter("Wihngo.Controllers.StoriesController", LogLevel.Information);
```

## Testing the Edit Endpoint

### PowerShell Test Script

```powershell
# 1. Login first
$loginBody = @{ 
    email = "alice@example.com"
    password = "Password123!" 
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod `
    -Uri "https://localhost:7297/api/auth/login" `
    -Method POST `
    -Body $loginBody `
    -ContentType "application/json"

$token = $loginResponse.token
Write-Host "? Logged in" -ForegroundColor Green

# 2. Get a story to edit
$stories = Invoke-RestMethod `
    -Uri "https://localhost:7297/api/stories/my-stories" `
    -Headers @{ "Authorization" = "Bearer $token" }

$storyId = $stories.items[0].storyId
Write-Host "?? Editing story: $storyId" -ForegroundColor Cyan

# 3. Edit story content
$updateBody = @{
    content = "My Updated Story Title and Full Content - This is the complete story text that I want to save..."
} | ConvertTo-Json

try {
    Invoke-RestMethod `
        -Uri "https://localhost:7297/api/stories/$storyId" `
        -Method PUT `
        -Headers @{ 
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        } `
        -Body $updateBody
    Write-Host "? Story updated successfully!" -ForegroundColor Green
} catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Get updated story
$updated = Invoke-RestMethod `
    -Uri "https://localhost:7297/api/stories/$storyId" `
    -Headers @{ "Authorization" = "Bearer $token" }

Write-Host "`n?? Updated Story:" -ForegroundColor Cyan
Write-Host "Content: $($updated.content.Substring(0, [Math]::Min(100, $updated.content.Length)))..." -ForegroundColor White
Write-Host "Created: $($updated.createdAt)" -ForegroundColor Gray
```

### cURL Test

```bash
# Login
curl -X POST https://localhost:7297/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"Password123!"}'

# Edit story (replace {token} and {storyId})
curl -X PUT https://localhost:7297/api/stories/{storyId} \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "My Updated Story - Full content here..."
  }'
```

## Mobile App Guidance

### Current Mobile App Behavior
If the mobile app is showing separate "Title" and "Content" fields, this is misleading because:
- There's only ONE field in the database: `Content`
- The "title" is auto-generated from `Content`

### Recommended Mobile App Changes

**Option 1: Single Content Field (Recommended)**
```typescript
// Show one rich text editor for the full story
<TextArea 
  label="Story Content"
  placeholder="Write your story here... The first 30 characters will be used as the title in lists."
  value={content}
  onChange={setContent}
  maxLength={5000}
/>
```

**Option 2: Separate Title + Content (If Needed)**
If you want to maintain separate UI fields:

```typescript
// Combine title and content before sending to API
const handleSave = async () => {
  // Combine title and content with a separator
  const fullContent = title.trim() + (title.trim() && content.trim() ? ' - ' : '') + content.trim();
  
  await api.put(`/api/stories/${storyId}`, {
    content: fullContent  // Send as single content field
  });
};

// When loading, split content if needed
const loadStory = async () => {
  const story = await api.get(`/api/stories/${storyId}`);
  
  // Try to split at first dash or use first 30 chars as title
  const separatorIndex = story.content.indexOf(' - ');
  if (separatorIndex > 0 && separatorIndex < 50) {
    setTitle(story.content.substring(0, separatorIndex));
    setContent(story.content.substring(separatorIndex + 3));
  } else {
    setTitle('');
    setContent(story.content);
  }
};
```

## Diagnostic Logs to Check

When editing a story, you should now see logs like:

```
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Edit story request for a1b2c3d4-...
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      DTO Content: True, Length: 125
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      DTO ImageS3Key: NULL
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Current story content length: 89
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Updating story content from 89 to 125 chars
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Story updated successfully: a1b2c3d4-..., Content length: 125
```

## Common Issues & Solutions

### Issue 1: "Title not updating"
**Cause**: Mobile app sending "title" field instead of "content"
**Solution**: Mobile app must send the full text in the `content` field

### Issue 2: "Content gets truncated to 30 chars"
**Cause**: Mobile app only sending truncated title
**Solution**: Send full story text in `content`, not just the title

### Issue 3: "Empty content error"
**Cause**: Sending `content: ""` or `content: "   "`
**Solution**: Content must not be empty if provided

### Issue 4: "Image not showing after edit"
**Cause**: Image S3 key was accidentally cleared
**Solution**: Only send `imageS3Key` if you want to change the image. Omit it to keep existing image.

## Summary

- ? **Added detailed logging** to track story edit requests
- ? **Improved validation** with better error messages
- ? **Clarified data model** - Stories have ONE content field, not separate title/content
- ? **Documented behavior** - Title is auto-generated from first 30 chars of content
- ?? **Mobile app may need updates** - Should send full content, not separate title

The edit endpoint is now working correctly with proper logging to diagnose any issues. The key insight is that **there is no separate title field** - it's derived from the content.
