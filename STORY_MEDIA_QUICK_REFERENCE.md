# Story Media - Quick Reference

## ? Rule: ONE Media Type Only

Stories can have **ONE image OR ONE video** (not both)

---

## ?? Mobile UI Rules

### ? Do This:

**When NO media selected:**
```
[ ?? Add Photo ]  [ ?? Add Video ]
```

**When IMAGE selected:**
```
[Image Preview]
[Remove]
(Hide or disable video button)
```

**When VIDEO selected:**
```
[Video Preview]
[Remove]
(Hide or disable image button)
```

### ? Don't Do This:

- Don't show both photo AND video buttons when media is selected
- Don't allow uploading both
- Don't show both image and video in story display

---

## ?? API Examples

### Create - Valid ?

```json
// Image only
{ "imageS3Key": "path/to/image.jpg" }

// Video only
{ "videoS3Key": "path/to/video.mp4" }

// No media
{ }
```

### Create - Invalid ?

```json
// BOTH - will fail with 400 error
{
  "imageS3Key": "path/to/image.jpg",
  "videoS3Key": "path/to/video.mp4"
}
```

### Update - Switch Media

```json
// Switch from image to video
PUT /api/stories/{id}
{
  "videoS3Key": "path/to/video.mp4"
}
// Result: Old image deleted, video set
```

---

## ?? Code Example

```typescript
function MediaSelector({ imageUri, videoUri, onImageSelect, onVideoSelect, onRemove }) {
  const hasMedia = imageUri || videoUri;
  
  return (
    <View>
      {!hasMedia && (
        <>
          <Button title="?? Add Photo" onPress={onImageSelect} />
          <Button title="?? Add Video" onPress={onVideoSelect} />
        </>
      )}
      
      {imageUri && (
        <>
          <Image source={{ uri: imageUri }} />
          <Button title="Remove" onPress={onRemove} />
        </>
      )}
      
      {videoUri && (
        <>
          <Video source={{ uri: videoUri }} />
          <Button title="Remove" onPress={onRemove} />
        </>
      )}
    </View>
  );
}

// Validation
function validateStory(data) {
  if (data.imageS3Key && data.videoS3Key) {
    throw new Error('Can only have image OR video, not both');
  }
}
```

---

## ?? Key Points

1. **One media type only** - never both
2. **Client-side prevention** - UI should disable opposite option
3. **API validation** - 400 error if both provided
4. **Auto-switching** - setting one removes the other
5. **Auto-cleanup** - old media deleted from S3

---

## ?? Testing Checklist

- [ ] Create with image ?
- [ ] Create with video ?
- [ ] Create with both ? (should fail)
- [ ] Switch image ? video
- [ ] Switch video ? image
- [ ] Remove media
- [ ] UI hides/disables opposite button

---

**Version:** 2.2 | **Status:** Ready (Rebuild Required)
