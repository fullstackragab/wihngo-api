# Story API Integration Guide for Mobile Team

## ?? Overview

This document describes the **updated Story API** with the following major changes:

### Key Changes
1. ? **Title field removed** - Stories no longer have a title field
2. ? **Multiple birds required** - Each story must tag at least 1 bird (can tag multiple)
3. ? **Story Mood optional** - Stories can have a mood/category (not required)
4. ? **Optional media** - Stories can include either ONE image OR ONE video (not both)

---

## ?? Story Moods (Optional)

Each story **can optionally** be tagged with one of these moods. **Never block posting if no mood is selected.**

| Mood | Value | Emoji | Description | Example |
|------|-------|-------|-------------|---------|
| Love & Bond | `LoveAndBond` | ?? | Affection, trust, cuddles, attachment | "She finally slept on my shoulder…" |
| New Beginning | `NewBeginning` | ?? | New bird, adoption, rescue, first day | "Welcome home, Sky ??" |
| Progress & Wins | `ProgressAndWins` | ?? | Training success, health improvement, milestones | "He learned to say his name today!" |
| Funny Moment | `FunnyMoment` | ?? | Silly behavior, unexpected actions | "He stole my keys again…" |
| Peaceful Moment | `PeacefulMoment` | ??? | Calm, beautiful, emotional silence | "Just watching him enjoy the sun…" |
| Loss & Memory | `LossAndMemory` | ?? | Passing away, remembrance, grief | "You'll always be with me…" |
| Care & Health | `CareAndHealth` | ?? | Vet visits, recovery, advice, awareness | "Post-surgery update…" |
| Daily Life | `DailyLife` | ?? | Normal routines, everyday moments | "Our usual morning together" |

---

## ?? UX Rules (VERY IMPORTANT)

### ? Make mood optional
- **Never block posting** if no mood is selected
- Default: No mood displayed (no pill/tag shown)
- Users can skip mood selection entirely

### ? One mood only
- Single selection keeps stories simple and scannable
- Radio button or single-select UI

### ? Emoji-first UI
- People choose faster with visuals than text
- Show large emojis with small labels
- Grid or horizontal scroll of emoji cards

### ? One Media Only
- Stories can have **ONE image OR ONE video** (not both)
- If user selects video, image option should be disabled/hidden
- If user selects image, video option should be disabled/hidden

### ? Display Format
Show mood as a **small pill or emoji tag** under bird name(s):

```
?? Luna · African Grey
?? Love & Bond
"Today she trusted me enough…"
[image or video player]
```

If no mood selected, just show:
```
?? Luna · African Grey
"Today she trusted me enough…"
[image or video player]
```

---

## ?? API Endpoints

### Base URL
```
https://your-api-domain.com/api/stories
```

---

## ?? GET All Stories

**Endpoint:** `GET /api/stories?page=1&pageSize=10`

**Response:**
```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "items": [
    {
      "storyId": "123e4567-e89b-12d3-a456-426614174000",
      "birds": ["Tweety", "Charlie"],
      "mode": "FunnyMoment",
      "date": "January 15, 2024",
      "preview": "Today my birds decided to play hide and seek...",
      "imageS3Key": "users/stories/user123/story456/image.jpg",
      "imageUrl": "https://s3.amazonaws.com/...",
      "videoS3Key": null,
      "videoUrl": null
    },
    {
      "storyId": "223e4567-e89b-12d3-a456-426614174001",
      "birds": ["Sunny"],
      "mode": null,
      "date": "January 14, 2024",
      "preview": "Just a normal day with my bird...",
      "imageS3Key": null,
      "imageUrl": null,
      "videoS3Key": "users/stories/user456/story789/video.mp4",
      "videoUrl": "https://s3.amazonaws.com/..."
    },
    {
      "storyId": "323e4567-e89b-12d3-a456-426614174002",
      "birds": ["Luna"],
      "mode": "DailyLife",
      "date": "January 13, 2024",
      "preview": "Another day...",
      "imageS3Key": null,
      "imageUrl": null,
      "videoS3Key": null,
      "videoUrl": null
    }
  ]
}
```

**Note:** `mode` can be `null` when no mood was selected. Only one of `imageS3Key` or `videoS3Key` will be present (not both).

---

## ?? GET Story by ID

**Endpoint:** `GET /api/stories/{storyId}`

**Response:**
```json
{
  "storyId": "123e4567-e89b-12d3-a456-426614174000",
  "content": "Today my birds decided to play hide and seek. Charlie hid behind the curtain and Tweety pretended not to see him. It was the cutest thing ever!",
  "mode": "FunnyMoment",
  "imageS3Key": "users/stories/user123/story456/image.jpg",
  "imageUrl": "https://s3.amazonaws.com/...",
  "videoS3Key": null,
  "videoUrl": null,
  "createdAt": "2024-01-15T10:30:00Z",
  "birds": [
    {
      "birdId": "bird-uuid-1",
      "name": "Tweety",
      "species": "Canary",
      "imageS3Key": "birds/tweety/profile.jpg",
      "imageUrl": "https://s3.amazonaws.com/...",
      "videoS3Key": null,
      "videoUrl": null,
      "tagline": "The singing sensation",
      "lovedBy": 150,
      "supportedBy": 45,
      "ownerId": "owner-uuid"
    }
  ],
  "author": {
    "userId": "user-uuid",
    "name": "John Doe"
  }
}
```

**Note:** `mode` can be `null` when no mood was selected. Only one of `imageS3Key` or `videoS3Key` will have a value.

---

## ?? CREATE Story

**Endpoint:** `POST /api/stories`

**Headers:**
```
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

**Request Body (with image):**
```json
{
  "birdIds": [
    "bird-uuid-1",
    "bird-uuid-2"
  ],
  "content": "Today my birds decided to play hide and seek. Charlie hid behind the curtain and Tweety pretended not to see him. It was the cutest thing ever!",
  "mode": "FunnyMoment",
  "imageS3Key": "users/stories/user123/story456/image.jpg"
}
```

**Request Body (with video):**
```json
{
  "birdIds": ["bird-uuid-1"],
  "content": "Watch this amazing moment!",
  "mode": "ProgressAndWins",
  "videoS3Key": "users/stories/user123/story456/video.mp4"
}
```

**Request Body (no media):**
```json
{
  "birdIds": ["bird-uuid-1"],
  "content": "Just a regular day with my bird.",
  "mode": null
}
```

**Field Requirements:**
- ? `birdIds` - **REQUIRED** - Array of at least 1 bird UUID
- ? `content` - **REQUIRED** - String (max 5000 chars)
- ? `mode` - **OPTIONAL** - One of the StoryMode enum values or `null`
- ? `imageS3Key` - **OPTIONAL** - S3 key from media upload (exclusive with videoS3Key)
- ? `videoS3Key` - **OPTIONAL** - S3 key from media upload (exclusive with imageS3Key)

**Important:** You can provide **either** `imageS3Key` **or** `videoS3Key`, but **not both**.

**Response:** Full story object (same as GET by ID)

**Status Codes:**
- `201 Created` - Story created successfully
- `400 Bad Request` - Validation error (missing required fields, invalid bird IDs, both image and video provided, etc.)
- `401 Unauthorized` - Invalid or missing token
- `404 Not Found` - One or more bird IDs not found

---

## ?? UPDATE Story

**Endpoint:** `PUT /api/stories/{storyId}`

**Headers:**
```
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

**Request Body (all fields optional):**
```json
{
  "birdIds": ["bird-uuid-1", "bird-uuid-3"],
  "content": "Updated content...",
  "mode": "PeacefulMoment",
  "imageS3Key": "users/stories/user123/story456/new-image.jpg"
}
```

**To switch from image to video:**
```json
{
  "imageS3Key": "",
  "videoS3Key": "users/stories/user123/story456/video.mp4"
}
```

**To remove all media:**
```json
{
  "imageS3Key": "",
  "videoS3Key": ""
}
```

**To remove mood:**
```json
{
  "mode": null
}
```

**Notes:**
- Only the story author can update
- To remove image/video, send empty string `""`
- To remove mood, send `null`
- To keep existing values, omit the field
- **Cannot have both image and video** - setting one will remove the other
- If you provide `birdIds`, it must contain at least 1 bird
- Old media files are automatically deleted when replaced

**Response:** `204 No Content`

**Status Codes:**
- `204 No Content` - Updated successfully
- `400 Bad Request` - Validation error (both image and video provided, etc.)
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - User is not the story author
- `404 Not Found` - Story or bird not found

---

## ??? DELETE Story

**Endpoint:** `DELETE /api/stories/{storyId}`

**Headers:**
```
Authorization: Bearer {your-jwt-token}
```

**Response:** `204 No Content`

**Notes:**
- Only the story author can delete
- Associated media (image or video) is automatically deleted from S3

---

## ?? Media Upload Flow

### Step 1: Request Upload URL

**Endpoint:** `GET /api/media/upload-url?mediaType=story-image&fileName=photo.jpg`

**Media Types:**
- `story-image` - For story images
- `story-video` - For story videos

**Response:**
```json
{
  "uploadUrl": "https://s3.amazonaws.com/...",
  "s3Key": "users/stories/user123/story456/abc-123.jpg",
  "expiresIn": 600
}
```

### Step 2: Upload File to S3

```javascript
// Upload directly to S3 using the pre-signed URL
const response = await fetch(uploadUrl, {
  method: 'PUT',
  body: fileBlob,
  headers: {
    'Content-Type': 'image/jpeg' // or 'video/mp4'
  }
});
```

### Step 3: Use S3 Key in Story

```javascript
// Use EITHER imageS3Key OR videoS3Key (not both)
{
  "imageS3Key": "users/stories/user123/story456/abc-123.jpg"
}

// OR

{
  "videoS3Key": "users/stories/user123/story456/xyz-789.mp4"
}
```

---

## ?? UI/UX Implementation Guide

### Story Creation Flow

1. **Bird Selection Screen** (Required)
   - Multi-select interface
   - Show user's birds
   - Minimum 1 required, display error if none selected
   - Visual indicator showing how many selected

2. **Mood Selection Screen** (Optional - Can Skip)
   - Grid of 8 moods with **large emojis** and small labels
   - Single selection (radio button behavior)
   - **"Skip" or "No Mood" button prominently displayed**
   - Show brief description on tap
   - Example layout:
   ```
   [?? Love & Bond]  [?? New Beginning]
   [?? Progress]      [?? Funny Moment]
   [??? Peaceful]     [?? Loss & Memory]
   [?? Care & Health] [?? Daily Life]
   
   [Skip / No Mood Selected]
   ```

3. **Content Entry Screen**
   - Large text area for content (required)
   - Character counter (max 5000)
   - **"Add Photo" button (optional)**
   - **"Add Video" button (optional)**
   - **IMPORTANT:** When user selects photo, disable/hide video button
   - **IMPORTANT:** When user selects video, disable/hide photo button
   - Preview selected media
   - **Show selected mood as small tag** (if any):
     ```
     Selected Mood: ?? Love & Bond [x remove]
     ```

4. **Media Upload**
   - Show upload progress
   - Allow removal/replacement
   - Validate file size/format before upload
   - **Enforce one media type only** - switching replaces current media

### Story Display

1. **Story List/Feed**
   - Show bird names (comma-separated)
   - **Only show mood pill/tag if mood exists**
   - Show content preview (140 chars)
   - Show thumbnail if image exists
   - Video icon/thumbnail if video exists
   - Tap to open full story
   
   **Example with mood and image:**
   ```
   ?? Luna, Charlie
   ?? Love & Bond
   "Today she trusted me enough…"
   [image thumbnail]
   ```
   
   **Example with video:**
   ```
   ?? Luna, Charlie
   "Today she trusted me enough…"
   [?? video thumbnail]
   ```

2. **Story Detail View**
   - Full content text
   - Bird avatars (tappable to bird profile)
   - **Mood indicator (only if mood exists)** - show as pill/badge
   - **If image exists:** full width, tappable for fullscreen
   - **If video exists:** inline player with controls
   - **Never show both** image and video
   - Author info
   - Created date
   - Edit/Delete buttons (if user is author)

---

## ?? Validation Rules

### Create Story
```javascript
{
  birdIds: {
    required: true,
    minLength: 1,
    type: "array<uuid>"
  },
  content: {
    required: true,
    maxLength: 5000,
    type: "string"
  },
  mode: {
    required: false, // OPTIONAL!
    enum: [
      "LoveAndBond",
      "NewBeginning", 
      "ProgressAndWins",
      "FunnyMoment",
      "PeacefulMoment",
      "LossAndMemory",
      "CareAndHealth",
      "DailyLife",
      null // null is valid (no mood)
    ]
  },
  imageS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    exclusive: "videoS3Key" // Cannot be provided with videoS3Key
  },
  videoS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    exclusive: "imageS3Key" // Cannot be provided with imageS3Key
  }
}
```

**Validation Rule:** If both `imageS3Key` and `videoS3Key` are provided, the API will return a 400 Bad Request error.

### Update Story
```javascript
{
  birdIds: {
    required: false,
    minLength: 1, // if provided
    type: "array<uuid>"
  },
  content: {
    required: false,
    maxLength: 5000,
    type: "string"
  },
  mode: {
    required: false,
    enum: [...same as create], // null to remove mood
    note: "null removes mood"
  },
  imageS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    note: "empty string removes image, setting this removes video"
  },
  videoS3Key: {
    required: false,
    maxLength: 1000,
    type: "string",
    note: "empty string removes video, setting this removes image"
  }
}
```

---

## ?? Error Handling

### Common Error Responses

**400 Bad Request - Missing Birds**
```json
{
  "message": "At least one bird must be selected"
}
```

**400 Bad Request - Invalid Birds**
```json
{
  "message": "Some birds not found",
  "missingBirdIds": ["uuid-1", "uuid-2"]
}
```

**400 Bad Request - Both Media Types**
```json
{
  "message": "Story can have either an image or a video, not both"
}
```

**400 Bad Request - Media Not Found**
```json
{
  "message": "Story image not found in S3. Please upload the file first."
}
```

**403 Forbidden**
```json
{
  "message": "User is not the story author"
}
```

---

## ?? Example Mobile Implementation (React Native)

### Create Story Screen

```typescript
interface StoryFormData {
  birdIds: string[];
  content: string;
  mode?: StoryMode | null; // OPTIONAL!
  imageS3Key?: string;
  videoS3Key?: string;
}

enum StoryMode {
  LoveAndBond = 'LoveAndBond',
  NewBeginning = 'NewBeginning',
  ProgressAndWins = 'ProgressAndWins',
  FunnyMoment = 'FunnyMoment',
  PeacefulMoment = 'PeacefulMoment',
  LossAndMemory = 'LossAndMemory',
  CareAndHealth = 'CareAndHealth',
  DailyLife = 'DailyLife'
}

const STORY_MOODS = [
  { 
    value: StoryMode.LoveAndBond, 
    label: 'Love & Bond', 
    emoji: '??',
    description: 'Affection, trust, cuddles, attachment',
    example: '"She finally slept on my shoulder…"'
  },
  { 
    value: StoryMode.NewBeginning, 
    label: 'New Beginning', 
    emoji: '??',
    description: 'New bird, adoption, rescue, first day',
    example: '"Welcome home, Sky ??"'
  },
  { 
    value: StoryMode.ProgressAndWins, 
    label: 'Progress & Wins', 
    emoji: '??',
    description: 'Training success, health improvement, milestones',
    example: '"He learned to say his name today!"'
  },
  { 
    value: StoryMode.FunnyMoment, 
    label: 'Funny Moment', 
    emoji: '??',
    description: 'Silly behavior, unexpected actions',
    example: '"He stole my keys again…"'
  },
  { 
    value: StoryMode.PeacefulMoment, 
    label: 'Peaceful Moment', 
    emoji: '???',
    description: 'Calm, beautiful, emotional silence',
    example: '"Just watching him enjoy the sun…"'
  },
  { 
    value: StoryMode.LossAndMemory, 
    label: 'Loss & Memory', 
    emoji: '??',
    description: 'Passing away, remembrance, grief',
    example: '"You\'ll always be with me…"'
  },
  { 
    value: StoryMode.CareAndHealth, 
    label: 'Care & Health', 
    emoji: '??',
    description: 'Vet visits, recovery, advice, awareness',
    example: '"Post-surgery update…"'
  },
  { 
    value: StoryMode.DailyLife, 
    label: 'Daily Life', 
    emoji: '??',
    description: 'Normal routines, everyday moments',
    example: '"Our usual morning together"'
  }
];

async function createStory(data: StoryFormData) {
  // Validate ONLY required fields
  if (data.birdIds.length === 0) {
    throw new Error('Please select at least one bird');
  }
  if (!data.content.trim()) {
    throw new Error('Please enter story content');
  }
  // NOTE: mode is optional - no validation needed!

  // Validate only one media type
  if (data.imageS3Key && data.videoS3Key) {
    throw new Error('Story can have either an image or a video, not both');
  }

  // Upload media if exists (only one will be set)
  if (imageUri) {
    const { s3Key } = await uploadMedia('story-image', imageUri);
    data.imageS3Key = s3Key;
    data.videoS3Key = undefined; // Ensure video is not set
  } else if (videoUri) {
    const { s3Key } = await uploadMedia('story-video', videoUri);
    data.videoS3Key = s3Key;
    data.imageS3Key = undefined; // Ensure image is not set
  }

  // Create story (mode can be null/undefined)
  const response = await fetch(`${API_BASE}/stories`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }

  return await response.json();
}

// Media selection component
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
        </View>
      )}
      
      {videoUri && (
        <View>
          <Video source={{ uri: videoUri }} style={styles.preview} />
          <Button title="Remove" onPress={onRemove} />
        </View>
      )}
    </View>
  );
}

// Display mood in story list/detail
function MoodBadge({ mood }: { mood?: StoryMode | null }) {
  if (!mood) return null; // Don't show anything if no mood
  
  const moodInfo = STORY_MOODS.find(m => m.value === mood);
  if (!moodInfo) return null;
  
  return (
    <View style={styles.moodBadge}>
      <Text style={styles.moodEmoji}>{moodInfo.emoji}</Text>
      <Text style={styles.moodLabel}>{moodInfo.label}</Text>
    </View>
  );
}

// Mood selection component
function MoodSelector({ 
  selectedMood, 
  onSelect 
}: { 
  selectedMood?: StoryMode | null; 
  onSelect: (mood: StoryMode | null) => void;
}) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Choose a mood (optional)</Text>
      <Text style={styles.subtitle}>Skip this step if you prefer</Text>
      
      <View style={styles.grid}>
        {STORY_MOODS.map(mood => (
          <TouchableOpacity
            key={mood.value}
            style={[
              styles.moodCard,
              selectedMood === mood.value && styles.moodCardSelected
            ]}
            onPress={() => onSelect(mood.value)}
          >
            <Text style={styles.moodCardEmoji}>{mood.emoji}</Text>
            <Text style={styles.moodCardLabel}>{mood.label}</Text>
          </TouchableOpacity>
        ))}
      </View>
      
      <Button 
        title="Skip / No Mood" 
        onPress={() => onSelect(null)}
        type="outline"
      />
    </View>
  );
}

async function uploadMedia(mediaType: string, fileUri: string) {
  // Step 1: Get upload URL
  const fileName = fileUri.split('/').pop();
  const uploadUrlResponse = await fetch(
    `${API_BASE}/media/upload-url?mediaType=${mediaType}&fileName=${fileName}`,
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  const { uploadUrl, s3Key } = await uploadUrlResponse.json();

  // Step 2: Upload file to S3
  const file = await fetch(fileUri);
  const blob = await file.blob();
  
  await fetch(uploadUrl, {
    method: 'PUT',
    body: blob,
    headers: {
      'Content-Type': blob.type
    }
  });

  return { s3Key };
}
```

---

## ?? Migration Guide

### Changes from Old API

| Old Field | New Field | Migration Notes |
|-----------|-----------|-----------------|
| `title` | ? Removed | No longer needed |
| `birdId` | `birdIds` | Now accepts array of bird IDs |
| N/A | `mode` | New **optional** field |
| `imageUrl` | `imageS3Key` | Now use S3 keys for upload |
| N/A | `videoS3Key` | New optional video support (exclusive with image) |
| `imageUrl` | `imageUrl` | Now in response only (pre-signed URL) |
| N/A | `videoUrl` | New in response (pre-signed URL) |

### Update Checklist

- [ ] Update story creation form to remove title field
- [ ] Update bird selection to support multiple birds (min 1)
- [ ] Add **optional** mood selection UI with 8 moods (emoji-first)
- [ ] Add "Skip" or "No Mood" button to mood selection
- [ ] Update media upload to use S3 keys
- [ ] Add optional video upload support
- [ ] **Enforce ONE media type only** - disable video when image selected, and vice versa
- [ ] Update story display to show multiple bird names
- [ ] Update story display to show mood pill/badge **only if mood exists**
- [ ] Update API calls to use new request/response structure
- [ ] Update validation logic (mood is now optional, only one media type allowed)
- [ ] Test create/update/delete flows with and without mood
- [ ] Test media upload/removal flows
- [ ] Test switching between image and video

---

## ?? Support

If you have questions or need clarification, please contact:
- Backend Team: backend@example.com
- API Documentation: https://your-api-domain.com/scalar/v1

---

**Last Updated:** January 2024  
**API Version:** 2.2 (One Media Type Only)
