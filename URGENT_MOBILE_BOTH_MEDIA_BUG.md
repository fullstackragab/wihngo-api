# ?? URGENT: Mobile App Bug - Sending Both Image and Video

## ? Current Problem

The mobile app is uploading **both** image and video, then sending both keys to the API:

```javascript
// ? WRONG - Both are being sent
{
  "birdIds": [...],
  "content": "...",
  "mode": "NewBeginning",
  "imageS3Key": "users/stories/.../image.png",  // ?
  "videoS3Key": "users/videos/.../video.mp4"     // ?
}
```

**API Response:**
```
400 Bad Request
{
  "message": "Story can have either an image or a video, not both"
}
```

---

## ? Solution

The mobile app must send **ONLY ONE** media type:

### Option 1: Image Only ?
```javascript
{
  "birdIds": [...],
  "content": "...",
  "mode": "NewBeginning",
  "imageS3Key": "users/stories/.../image.png"
  // videoS3Key is NOT included
}
```

### Option 2: Video Only ?
```javascript
{
  "birdIds": [...],
  "content": "...",
  "mode": "NewBeginning",
  "videoS3Key": "users/stories/.../video.mp4"
  // imageS3Key is NOT included
}
```

### Option 3: No Media ?
```javascript
{
  "birdIds": [...],
  "content": "...",
  "mode": "NewBeginning"
  // Neither media key is included
}
```

---

## ?? Code Fix Required

### File: `C:\expo\wihngo\app\create-story.tsx`

**Current Issue (Lines ~195-219):**
The code is uploading BOTH image and video, then including both in the request.

**Fix Option 1: UI Enforcement (RECOMMENDED)**

```typescript
// Add state to track which media type user selected
const [mediaType, setMediaType] = useState<'image' | 'video' | null>(null);

// In your media selection UI
{!image && !video && (
  <>
    <Button 
      title="?? Add Photo" 
      onPress={() => {
        setMediaType('image');
        pickImage();
      }}
    />
    <Button 
      title="?? Add Video" 
      onPress={() => {
        setMediaType('video');
        pickVideo();
      }}
    />
  </>
)}

{image && (
  <View>
    <Image source={{ uri: image }} />
    <Button 
      title="Remove" 
      onPress={() => {
        setImage(null);
        setMediaType(null);
      }}
    />
    {/* Don't show "Add Video" button when image is selected */}
  </View>
)}

{video && (
  <View>
    <Video source={{ uri: video }} />
    <Button 
      title="Remove" 
      onPress={() => {
        setVideo(null);
        setMediaType(null);
      }}
    />
    {/* Don't show "Add Photo" button when video is selected */}
  </View>
)}

// When creating story
const handleCreate = async () => {
  let imageS3Key = null;
  let videoS3Key = null;

  // Upload ONLY the selected media type
  if (image && mediaType === 'image') {
    imageS3Key = await uploadStoryImage(image);
  } else if (video && mediaType === 'video') {
    videoS3Key = await uploadStoryVideo(video);
  }

  // Create story with ONLY ONE media key
  const storyData = {
    birdIds: selectedBirds,
    content: content,
    mode: selectedMood,
    ...(imageS3Key && { imageS3Key }), // Only include if exists
    ...(videoS3Key && { videoS3Key })  // Only include if exists
  };

  await StoryService.create(storyData);
};
```

**Fix Option 2: Validation Before Submit**

```typescript
const handleCreate = async () => {
  // Validate only one media type
  if (image && video) {
    Alert.alert(
      'Error',
      'Story can have either a photo or a video, not both. Please remove one.'
    );
    return;
  }

  let imageS3Key = null;
  let videoS3Key = null;

  if (image) {
    imageS3Key = await uploadStoryImage(image);
  } else if (video) {
    videoS3Key = await uploadStoryVideo(video);
  }

  const storyData = {
    birdIds: selectedBirds,
    content: content,
    mode: selectedMood,
    ...(imageS3Key && { imageS3Key }),
    ...(videoS3Key && { videoS3Key })
  };

  await StoryService.create(storyData);
};
```

---

## ?? Recommended UI Flow

### Before Selecting Media
```
???????????????????????????????
?  [ ?? Add Photo ]            ?
?  [ ?? Add Video ]            ?
???????????????????????????????
```

### After Selecting Photo
```
???????????????????????????????
?  [Image Preview]             ?
?  [Remove Photo]              ?
?                              ?
?  ?? To add video, remove    ?
?     this photo first         ?
???????????????????????????????
```

### After Selecting Video
```
???????????????????????????????
?  [Video Preview]             ?
?  [Remove Video]              ?
?                              ?
?  ?? To add photo, remove    ?
?     this video first         ?
???????????????????????????????
```

---

## ?? Quick Checklist

- [ ] Remove ability to select both photo and video
- [ ] Show only "Add Photo" OR "Add Video" buttons at a time
- [ ] When photo selected, hide/disable video button
- [ ] When video selected, hide/disable photo button
- [ ] Only upload the selected media type
- [ ] Only send ONE of imageS3Key or videoS3Key in API request
- [ ] Add validation before submit
- [ ] Test creating story with photo only
- [ ] Test creating story with video only
- [ ] Test creating story with no media
- [ ] Verify error no longer occurs

---

## ?? Additional Issue Found

The video S3 key path is:
```
users/videos/d734e5ca-5690-.../video.mp4
```

But it should be:
```
users/stories/d734e5ca-5690-.../video.mp4
```

**Fix Required:**
When requesting upload URL for story video, use `mediaType='story-video'`, which should generate the correct path pattern.

Check `MediaService.uploadStoryVideo()` to ensure it's using the correct media type.

---

## ? Expected Behavior After Fix

1. User can select **either** photo **or** video (not both)
2. UI prevents selecting both at the same time
3. API request includes only one media key
4. Story creation succeeds with 201 status
5. No validation errors

---

**Priority:** ?? HIGH - Blocking story creation  
**Status:** ?? Requires Mobile App Fix  
**ETA:** ~15-30 minutes to implement
