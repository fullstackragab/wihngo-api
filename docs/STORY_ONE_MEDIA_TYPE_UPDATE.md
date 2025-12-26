# Story Media Update - One Media Type Only

## ? Changes Made

### Story Can Have ONE Image OR ONE Video (Not Both)

The story media handling has been updated to enforce **one media type only** per your requirement.

---

## ?? Key Changes

### 1. **One Media Type Rule** ?
- Stories can have **ONE image OR ONE video** (not both)
- When user selects image, video option should be disabled
- When user selects video, image option should be disabled
- Switching from image to video automatically deletes the image
- Switching from video to image automatically deletes the video

### 2. **API Validation** ?
- POST endpoint validates that both imageS3Key and videoS3Key are not provided
- Returns 400 Bad Request if both are provided
- Error message: "Story can have either an image or a video, not both"

### 3. **Automatic Cleanup** ?
- PUT endpoint handles media type switching
- Setting image removes video (if exists)
- Setting video removes image (if exists)
- Old media files are automatically deleted from S3

---

## ?? Technical Updates

### Files Changed

1. **Controllers\StoriesController.cs**
   - **POST endpoint:** Added validation to reject requests with both image and video
   - **PUT endpoint:** Auto-deletes opposite media type when switching
   - Logs all media changes for debugging

2. **MOBILE_STORY_API_GUIDE.md**
   - Complete rewrite to emphasize one media type rule
   - Updated all examples
   - Added MediaSelector component example
   - Updated validation rules
   - Added error response for both media types

---

## ?? Mobile Implementation

### Media Selection UI

```typescript
// Media selection component - only allows ONE media type
function MediaSelector({ 
  imageUri, 
  videoUri, 
  onImageSelect, 
  onVideoSelect,
  onRemove
}: MediaSelectorProps) {
  const hasMedia = imageUri || videoUri;
  
  return (
    <View style={styles.container}>
      {!hasMedia && (
        <>
          <Button 
            title="?? Add Photo" 
            onPress={onImageSelect}
          />
          <Button 
            title="?? Add Video" 
            onPress={onVideoSelect}
          />
        </>
      )}
      
      {imageUri && (
        <View>
          <Image source={{ uri: imageUri }} style={styles.preview} />
          <Button title="Remove" onPress={onRemove} />
          <Text style={styles.hint}>To add video, remove this image first</Text>
        </View>
      )}
      
      {videoUri && (
        <View>
          <Video source={{ uri: videoUri }} style={styles.preview} />
          <Button title="Remove" onPress={onRemove} />
          <Text style={styles.hint}>To add image, remove this video first</Text>
        </View>
      )}
    </View>
  );
}
```

### Validation Before Submit

```typescript
async function createStory(data: StoryFormData) {
  // ... other validations ...

  // Validate only one media type
  if (data.imageS3Key && data.videoS3Key) {
    throw new Error('Story can have either an image or a video, not both');
  }

  // Ensure only one is set
  if (imageUri) {
    const { s3Key } = await uploadMedia('story-image', imageUri);
    data.imageS3Key = s3Key;
    data.videoS3Key = undefined; // Ensure video is not set
  } else if (videoUri) {
    const { s3Key } = await uploadMedia('story-video', videoUri);
    data.videoS3Key = s3Key;
    data.imageS3Key = undefined; // Ensure image is not set
  }

  // ... create story ...
}
```

---

## ?? API Behavior

### Create Story

**Valid - Image only:**
```json
POST /api/stories
{
  "birdIds": ["uuid"],
  "content": "Story text",
  "mode": "FunnyMoment",
  "imageS3Key": "users/stories/.../image.jpg"
}
```

**Valid - Video only:**
```json
POST /api/stories
{
  "birdIds": ["uuid"],
  "content": "Story text",
  "mode": "FunnyMoment",
  "videoS3Key": "users/stories/.../video.mp4"
}
```

**Valid - No media:**
```json
POST /api/stories
{
  "birdIds": ["uuid"],
  "content": "Story text"
}
```

**Invalid - Both media types:**
```json
POST /api/stories
{
  "birdIds": ["uuid"],
  "content": "Story text",
  "imageS3Key": "users/stories/.../image.jpg",
  "videoS3Key": "users/stories/.../video.mp4"
}

// Response: 400 Bad Request
{
  "message": "Story can have either an image or a video, not both"
}
```

### Update Story - Switch Media Type

**Switch from image to video:**
```json
PUT /api/stories/{id}
{
  "imageS3Key": "",  // Remove image
  "videoS3Key": "users/stories/.../video.mp4"  // Add video
}

// Result:
// - Old image is deleted from S3
// - New video is set
```

**Switch from video to image:**
```json
PUT /api/stories/{id}
{
  "videoS3Key": "",  // Remove video
  "imageS3Key": "users/stories/.../image.jpg"  // Add image
}

// Result:
// - Old video is deleted from S3
// - New image is set
```

**Implicit switch (just set the new one):**
```json
PUT /api/stories/{id}
{
  "videoS3Key": "users/stories/.../video.mp4"
}

// Result:
// - If story had image, it's automatically deleted
// - New video is set
```

---

## ?? Validation Rules

### Create Story
```javascript
{
  imageS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    exclusive: "videoS3Key", // Cannot be provided with videoS3Key
    note: "Providing this rejects request if videoS3Key is also provided"
  },
  videoS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    exclusive: "imageS3Key", // Cannot be provided with imageS3Key
    note: "Providing this rejects request if imageS3Key is also provided"
  }
}
```

### Update Story
```javascript
{
  imageS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    note: "Setting this automatically removes video (if exists)"
  },
  videoS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    note: "Setting this automatically removes image (if exists)"
  }
}
```

---

## ?? UX Guidelines

### Story Creation

1. **Initial State:**
   ```
   [?? Add Photo] [?? Add Video]
   ```

2. **After selecting image:**
   ```
   [Image Preview]
   [Remove]
   
   Video option: DISABLED or HIDDEN
   Hint: "To add video, remove this image first"
   ```

3. **After selecting video:**
   ```
   [Video Preview with player]
   [Remove]
   
   Image option: DISABLED or HIDDEN
   Hint: "To add image, remove this video first"
   ```

### Story Display

**Feed view:**
- If image: Show thumbnail
- If video: Show video thumbnail with play icon
- Never show both

**Detail view:**
- If image: Full-width image, tappable for fullscreen
- If video: Inline video player with controls
- Never show both

---

## ? Testing Checklist

Mobile team should test:

- [ ] Create story with image only
- [ ] Create story with video only
- [ ] Create story with no media
- [ ] Attempt to create story with both (should fail with error)
- [ ] Edit story to add image (when had none)
- [ ] Edit story to add video (when had none)
- [ ] Edit story to switch from image to video
- [ ] Edit story to switch from video to image
- [ ] Edit story to remove image
- [ ] Edit story to remove video
- [ ] UI disables/hides video button when image selected
- [ ] UI disables/hides image button when video selected
- [ ] Feed displays correctly with images
- [ ] Feed displays correctly with videos
- [ ] Detail view plays videos correctly

---

## ?? Error Responses

### 400 Bad Request - Both Media Types
```json
{
  "message": "Story can have either an image or a video, not both"
}
```

This error occurs when:
- Creating a story with both imageS3Key and videoS3Key
- The mobile app should prevent this on the client side

---

## ?? Next Steps

1. **Stop running app** (if needed to rebuild)
2. **Rebuild:** `dotnet build`
3. **Test API:**
   - Create story with image
   - Create story with video
   - Try creating with both (should fail)
   - Test switching media types
4. **Update mobile app:**
   - Implement one media type UI
   - Add validation
   - Test all scenarios

---

## ?? Related Documentation

- `MOBILE_STORY_API_GUIDE.md` - Complete API guide (updated)
- `STORY_API_UPDATE_SUMMARY.md` - Original story changes
- `STORY_MOOD_UPDATE_SUMMARY.md` - Mood optional changes

---

**Updated:** January 2024  
**API Version:** 2.2  
**Status:** Code Complete - Rebuild Required ??

**Note:** Application is currently running. Stop the app and rebuild to apply these changes.
