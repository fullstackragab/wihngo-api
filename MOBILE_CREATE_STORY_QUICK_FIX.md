# Quick Fix for create-story.tsx

## ?? Problem
The app is uploading and sending both image AND video to the API, causing a 400 error.

## ? Quick Fix (Copy-Paste Ready)

### Step 1: Add Media Type State

```typescript
// Add this near your other useState declarations
const [mediaType, setMediaType] = useState<'image' | 'video' | null>(null);
```

### Step 2: Update Media Removal

```typescript
// When removing image
const removeImage = () => {
  setImage(null);
  setMediaType(null);
};

// When removing video
const removeVideo = () => {
  setVideo(null);
  setMediaType(null);
};
```

### Step 3: Update Media Selection

```typescript
// When picking image
const pickImage = async () => {
  // ... existing code ...
  if (result && !result.canceled) {
    setImage(result.assets[0].uri);
    setMediaType('image');
    setVideo(null); // Clear video if exists
  }
};

// When picking video
const pickVideo = async () => {
  // ... existing code ...
  if (result && !result.canceled) {
    setVideo(result.assets[0].uri);
    setMediaType('video');
    setImage(null); // Clear image if exists
  }
};
```

### Step 4: Update Media Upload Logic

```typescript
const handleCreate = async () => {
  try {
    setLoading(true);

    // Upload ONLY the selected media type
    let imageS3Key: string | undefined;
    let videoS3Key: string | undefined;

    if (image && mediaType === 'image') {
      console.log('?? Uploading story image...');
      imageS3Key = await MediaService.uploadStoryImage(image, storyId);
      console.log('? Story image uploaded:', imageS3Key);
    } else if (video && mediaType === 'video') {
      console.log('?? Uploading story video...');
      videoS3Key = await MediaService.uploadStoryVideo(video, storyId);
      console.log('? Story video uploaded:', videoS3Key);
    }

    // Create story data with ONLY ONE media key
    const storyData = {
      birdIds: selectedBirds,
      content: content,
      mode: selectedMood || null,
      ...(imageS3Key && { imageS3Key }), // Only if exists
      ...(videoS3Key && { videoS3Key })  // Only if exists
    };

    console.log('?? Creating story with data:', storyData);

    const newStory = await StoryService.create(storyData);
    console.log('? Story created:', newStory);

    // Navigate back
    router.back();
  } catch (error) {
    console.error('Error creating story:', error);
    Alert.alert('Error', 'Failed to create story. Please try again.');
  } finally {
    setLoading(false);
  }
};
```

### Step 5: Update UI to Show Only One Button

```tsx
{/* Media Selection - Show only when no media selected */}
{!image && !video && (
  <View style={styles.mediaButtons}>
    <TouchableOpacity style={styles.mediaButton} onPress={pickImage}>
      <Text style={styles.mediaButtonText}>?? Add Photo</Text>
    </TouchableOpacity>
    <TouchableOpacity style={styles.mediaButton} onPress={pickVideo}>
      <Text style={styles.mediaButtonText}>?? Add Video</Text>
    </TouchableOpacity>
  </View>
)}

{/* Image Preview - Hide video button */}
{image && (
  <View style={styles.mediaPreview}>
    <Image source={{ uri: image }} style={styles.previewImage} />
    <TouchableOpacity style={styles.removeButton} onPress={removeImage}>
      <Text style={styles.removeButtonText}>Remove Photo</Text>
    </TouchableOpacity>
    <Text style={styles.hint}>To add video, remove this photo first</Text>
  </View>
)}

{/* Video Preview - Hide image button */}
{video && (
  <View style={styles.mediaPreview}>
    <Video 
      source={{ uri: video }} 
      style={styles.previewVideo}
      useNativeControls
      resizeMode="contain"
    />
    <TouchableOpacity style={styles.removeButton} onPress={removeVideo}>
      <Text style={styles.removeButtonText}>Remove Video</Text>
    </TouchableOpacity>
    <Text style={styles.hint}>To add photo, remove this video first</Text>
  </View>
)}
```

---

## ?? Test After Fix

1. Select photo ? Should disable/hide video button
2. Remove photo ? Should show both buttons again
3. Select video ? Should disable/hide photo button
4. Remove video ? Should show both buttons again
5. Create story with photo ? Should succeed
6. Create story with video ? Should succeed
7. Create story with no media ? Should succeed

---

## ?? If Still Getting Error

Check the console logs:
- Should NOT see both imageS3Key AND videoS3Key in "Creating story with data" log
- Should see ONLY ONE or NEITHER

If you still see both keys being sent, add this validation before API call:

```typescript
// Add before StoryService.create()
if (imageS3Key && videoS3Key) {
  Alert.alert('Error', 'Cannot send both image and video. This is a bug.');
  return;
}
```

---

**Time to Fix:** ~5 minutes  
**Lines to Change:** ~30 lines  
**Files Affected:** 1 file (`create-story.tsx`)
