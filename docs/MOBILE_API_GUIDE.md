# Wihngo Mobile API - S3 Media Integration Guide

## ?? For Mobile Developers

This guide explains how to integrate with the Wihngo API's new S3-based media upload system for user profiles, bird profiles, and stories.

---

## ?? Authentication

All endpoints require JWT Bearer token authentication:

```
Authorization: Bearer {your-jwt-token}
```

Get your token from the login endpoint:
```
POST /api/auth/login
```

---

## ?? Media Upload Flow

All media uploads follow this 3-step process:

### **Step 1:** Request Pre-Signed Upload URL
### **Step 2:** Upload File Directly to S3
### **Step 3:** Update Resource with S3 Key

---

## ?? Step 1: Get Pre-Signed Upload URL

### Endpoint
```
POST /api/media/upload-url
```

### Request Headers
```
Authorization: Bearer {token}
Content-Type: application/json
```

### Request Body
```json
{
  "mediaType": "profile-image",
  "fileExtension": ".jpg",
  "relatedId": "optional-guid"
}
```

### Valid Media Types

| Media Type | Description | Requires relatedId? | Example Use Case |
|------------|-------------|---------------------|------------------|
| `profile-image` | User profile picture | No | Update user avatar |
| `story-image` | Story photo | Yes (storyId) | Add photo to story |
| `story-video` | Story video | No | Add video content |
| `bird-profile-image` | Bird profile photo | Yes (birdId) | Bird profile picture |
| `bird-video` | Bird video | Yes (birdId) | Bird showcase video |

### Valid File Extensions

**Images:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`  
**Videos:** `.mp4`, `.mov`, `.avi`, `.webm`

### Response
```json
{
  "uploadUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "s3Key": "users/profile-images/123e4567-e89b-12d3-a456-426614174000/a1b2c3d4.jpg",
  "expiresAt": "2024-01-15T10:40:00Z",
  "instructions": "Use PUT request to upload the file to the uploadUrl"
}
```

**?? Important:** The `uploadUrl` expires in **10 minutes**. Save the `s3Key` for Step 3.

---

## ?? Step 2: Upload to S3

### Endpoint
```
PUT {uploadUrl from Step 1}
```

### Request Headers
```
Content-Type: image/jpeg  // or appropriate MIME type
```

### Request Body
Binary file data (raw image/video bytes)

### Response
- **200 OK** - Upload successful
- **403 Forbidden** - URL expired or invalid
- **400 Bad Request** - Invalid file format

---

## ?? Step 3: Update Resource with S3 Key

After successful upload to S3, update your resource with the S3 key from Step 1.

---

## ?? User Profile Management

### Update User Profile

#### Endpoint
```
PUT /api/users/profile
```

#### Request Headers
```
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "name": "John Doe",
  "profileImageS3Key": "users/profile-images/123e4567-e89b-12d3-a456-426614174000/a1b2c3d4.jpg",
  "bio": "Bird enthusiast and nature lover"
}
```

**Field Details:**
- `name` (optional): Max 200 characters
- `profileImageS3Key` (optional): S3 key from Step 1
- `bio` (optional): Max 2000 characters

**Note:** At least one field must be provided.

#### Response
```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe",
  "email": "john@example.com",
  "profileImageS3Key": "users/profile-images/123e4567-e89b-12d3-a456-426614174000/a1b2c3d4.jpg",
  "profileImageUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "bio": "Bird enthusiast and nature lover",
  "emailConfirmed": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Note:** `profileImageUrl` is a pre-signed URL that expires in **10 minutes**. Cache it for display but refresh when expired.

---

### Get User Profile

#### Endpoint
```
GET /api/users/profile
```

#### Request Headers
```
Authorization: Bearer {token}
```

#### Response
Same as update profile response above.

---

## ?? Bird Profile Management

### Create Bird

#### Endpoint
```
POST /api/birds
```

#### Request Headers
```
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "name": "Ruby",
  "species": "Anna's Hummingbird",
  "tagline": "Fast and fearless",
  "description": "A beautiful ruby-throated hummingbird",
  "imageS3Key": "birds/profile-images/bird-guid/uuid.jpg",
  "videoS3Key": "birds/videos/bird-guid/uuid.mp4"
}
```

**Note:** You must upload the image and video to S3 first (Steps 1-2) before creating the bird.

#### Response
```json
{
  "birdId": "bird-guid",
  "name": "Ruby",
  "species": "Anna's Hummingbird",
  "imageS3Key": "birds/profile-images/bird-guid/uuid.jpg",
  "imageUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "videoS3Key": "birds/videos/bird-guid/uuid.mp4",
  "videoUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "tagline": "Fast and fearless",
  "lovedBy": 0,
  "supportedBy": 0,
  "ownerId": "user-guid",
  "isLoved": false
}
```

---

### Update Bird

#### Endpoint
```
PUT /api/birds/{birdId}
```

#### Request Headers
```
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "name": "Ruby Updated",
  "species": "Anna's Hummingbird",
  "tagline": "Fast and fearless",
  "description": "Updated description",
  "imageS3Key": "birds/profile-images/bird-guid/new-uuid.jpg",
  "videoS3Key": "birds/videos/bird-guid/new-uuid.mp4"
}
```

**Note:** Old media files are automatically deleted when you update with new S3 keys.

---

### Get Bird Profile

#### Endpoint
```
GET /api/birds/{birdId}
```

#### Response
```json
{
  "commonName": "Ruby",
  "scientificName": "Anna's Hummingbird",
  "emoji": "??",
  "tagline": "Fast and fearless",
  "description": "A beautiful ruby-throated hummingbird",
  "imageS3Key": "birds/profile-images/bird-guid/uuid.jpg",
  "imageUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "videoS3Key": "birds/videos/bird-guid/uuid.mp4",
  "videoUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "personality": ["Fearless", "Territorial"],
  "conservation": {
    "status": "Least Concern",
    "needs": "Native plant gardens"
  },
  "funFacts": ["Can fly 60 mph in dives"],
  "lovedBy": 42,
  "supportedBy": 15,
  "owner": {
    "userId": "user-guid",
    "name": "John Doe"
  },
  "isLoved": false
}
```

---

### List All Birds

#### Endpoint
```
GET /api/birds
```

#### Response
```json
[
  {
    "birdId": "bird-guid",
    "name": "Ruby",
    "species": "Anna's Hummingbird",
    "imageS3Key": "birds/profile-images/bird-guid/uuid.jpg",
    "imageUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
    "videoS3Key": "birds/videos/bird-guid/uuid.mp4",
    "videoUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
    "tagline": "Fast and fearless",
    "lovedBy": 42,
    "supportedBy": 15,
    "ownerId": "user-guid",
    "isLoved": false
  }
]
```

---

## ?? Story Management

### Create Story

#### Endpoint
```
POST /api/stories
```

#### Request Headers
```
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "birdId": "bird-guid",
  "content": "Ruby visited my feeder today and put on quite a show!",
  "imageS3Key": "users/stories/user-guid/story-guid/uuid.jpg"
}
```

**Note:** `imageS3Key` is optional. Stories can be text-only.

#### Response
```json
{
  "storyId": "story-guid",
  "content": "Ruby visited my feeder today and put on quite a show!",
  "imageS3Key": "users/stories/user-guid/story-guid/uuid.jpg",
  "imageUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "createdAt": "2024-01-15T10:30:00Z",
  "bird": {
    "birdId": "bird-guid",
    "name": "Ruby",
    "species": "Anna's Hummingbird",
    "imageS3Key": "birds/profile-images/bird-guid/uuid.jpg",
    "imageUrl": "https://...",
    "videoS3Key": "birds/videos/bird-guid/uuid.mp4",
    "videoUrl": "https://...",
    "tagline": "Fast and fearless",
    "lovedBy": 42,
    "supportedBy": 15,
    "ownerId": "user-guid",
    "isLoved": false
  },
  "author": {
    "userId": "user-guid",
    "name": "John Doe"
  }
}
```

---

### Update Story

#### Endpoint
```
PUT /api/stories/{storyId}
```

#### Request Headers
```
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "content": "Updated story content",
  "imageS3Key": "users/stories/user-guid/story-guid/new-uuid.jpg"
}
```

**Options:**
- Omit `imageS3Key` to keep existing image
- Set `imageS3Key` to empty string `""` to remove image
- Provide new `imageS3Key` to replace image (old one is deleted)

---

### Get Story

#### Endpoint
```
GET /api/stories/{storyId}
```

#### Response
Same as create story response.

---

### List Stories (Paginated)

#### Endpoint
```
GET /api/stories?page=1&pageSize=10
```

#### Query Parameters
- `page` (default: 1): Page number
- `pageSize` (default: 10): Items per page

#### Response
```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 50,
  "items": [
    {
      "storyId": "story-guid",
      "title": "Ruby visited my feeder...",
      "bird": "Ruby",
      "date": "January 15, 2024",
      "preview": "Ruby visited my feeder today and put on quite a show!...",
      "imageS3Key": "users/stories/user-guid/story-guid/uuid.jpg",
      "imageUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/..."
    }
  ]
}
```

---

## ??? Delete Media

### Endpoint
```
DELETE /api/media?s3Key=users/profile-images/user-guid/uuid.jpg
```

### Request Headers
```
Authorization: Bearer {token}
```

### Response
```json
{
  "message": "File deleted successfully"
}
```

**Note:** You can only delete your own files. The API validates ownership.

---

## ?? Additional Endpoints

### Check if Media Exists

#### Endpoint
```
GET /api/media/exists?s3Key=users/profile-images/user-guid/uuid.jpg
```

#### Request Headers
```
Authorization: Bearer {token}
```

#### Response
```json
{
  "exists": true
}
```

---

### Generate Download URL

If you have an S3 key and need a fresh download URL:

#### Endpoint
```
POST /api/media/download-url
```

#### Request Headers
```
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "s3Key": "users/profile-images/user-guid/uuid.jpg"
}
```

#### Response
```json
{
  "downloadUrl": "https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com/...",
  "expiresAt": "2024-01-15T10:40:00Z"
}
```

---

## ?? React Native / Expo Implementation Examples

### User Profile Image Upload

```javascript
import * as ImagePicker from 'expo-image-picker';

async function uploadProfileImage(authToken) {
  try {
    // Step 1: Pick image
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (result.canceled) return;

    const imageUri = result.assets[0].uri;
    const fileExtension = imageUri.split('.').pop();

    // Step 2: Get upload URL
    const uploadUrlResponse = await fetch('https://api.wihngo.com/api/media/upload-url', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        mediaType: 'profile-image',
        fileExtension: `.${fileExtension}`,
      }),
    });

    const { uploadUrl, s3Key } = await uploadUrlResponse.json();

    // Step 3: Upload to S3
    const fileBlob = await fetch(imageUri).then(r => r.blob());
    
    const uploadResponse = await fetch(uploadUrl, {
      method: 'PUT',
      headers: {
        'Content-Type': `image/${fileExtension}`,
      },
      body: fileBlob,
    });

    if (!uploadResponse.ok) {
      throw new Error('Failed to upload to S3');
    }

    // Step 4: Update profile
    const updateResponse = await fetch('https://api.wihngo.com/api/users/profile', {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        profileImageS3Key: s3Key,
      }),
    });

    const updatedProfile = await updateResponse.json();
    console.log('Profile updated:', updatedProfile);
    
    return updatedProfile;
  } catch (error) {
    console.error('Upload failed:', error);
    throw error;
  }
}
```

---

### Display Image with Auto-Refresh

```javascript
import { useState, useEffect } from 'react';
import { Image } from 'react-native';

function ProfileImage({ s3Key, authToken }) {
  const [imageUrl, setImageUrl] = useState(null);
  const [expiration, setExpiration] = useState(null);

  useEffect(() => {
    loadImage();
  }, [s3Key]);

  useEffect(() => {
    if (!expiration) return;

    const now = new Date();
    const expiresAt = new Date(expiration);
    const timeUntilExpiry = expiresAt - now;

    // Refresh 1 minute before expiration
    const refreshTime = Math.max(0, timeUntilExpiry - 60000);

    const timer = setTimeout(loadImage, refreshTime);
    return () => clearTimeout(timer);
  }, [expiration]);

  async function loadImage() {
    if (!s3Key) return;

    try {
      const response = await fetch('https://api.wihngo.com/api/media/download-url', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ s3Key }),
      });

      const { downloadUrl, expiresAt } = await response.json();
      setImageUrl(downloadUrl);
      setExpiration(expiresAt);
    } catch (error) {
      console.error('Failed to load image:', error);
    }
  }

  if (!imageUrl) return null;

  return (
    <Image
      source={{ uri: imageUrl }}
      style={{ width: 100, height: 100, borderRadius: 50 }}
    />
  );
}
```

---

### Bird Creation with Media

```javascript
async function createBirdWithMedia(authToken, birdData, imageUri, videoUri) {
  try {
    // Upload image
    const imageExt = imageUri.split('.').pop();
    const imageUploadResponse = await fetch('https://api.wihngo.com/api/media/upload-url', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        mediaType: 'bird-profile-image',
        fileExtension: `.${imageExt}`,
        relatedId: null, // Will be set after bird creation
      }),
    });

    const { uploadUrl: imageUploadUrl, s3Key: imageS3Key } = await imageUploadResponse.json();

    // Upload image to S3
    const imageBlob = await fetch(imageUri).then(r => r.blob());
    await fetch(imageUploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': `image/${imageExt}` },
      body: imageBlob,
    });

    // Upload video
    const videoExt = videoUri.split('.').pop();
    const videoUploadResponse = await fetch('https://api.wihngo.com/api/media/upload-url', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        mediaType: 'bird-video',
        fileExtension: `.${videoExt}`,
        relatedId: null,
      }),
    });

    const { uploadUrl: videoUploadUrl, s3Key: videoS3Key } = await videoUploadResponse.json();

    // Upload video to S3
    const videoBlob = await fetch(videoUri).then(r => r.blob());
    await fetch(videoUploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': `video/${videoExt}` },
      body: videoBlob,
    });

    // Create bird
    const createBirdResponse = await fetch('https://api.wihngo.com/api/birds', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        name: birdData.name,
        species: birdData.species,
        tagline: birdData.tagline,
        description: birdData.description,
        imageS3Key: imageS3Key,
        videoS3Key: videoS3Key,
      }),
    });

    const bird = await createBirdResponse.json();
    console.log('Bird created:', bird);
    
    return bird;
  } catch (error) {
    console.error('Bird creation failed:', error);
    throw error;
  }
}
```

---

## ?? Error Handling

### Common Error Responses

#### 400 Bad Request
```json
{
  "message": "Profile image file not found in S3. Please upload the file first."
}
```

#### 401 Unauthorized
```json
{
  "message": "Invalid authentication token"
}
```

#### 403 Forbidden
```json
{
  "message": "You don't have permission to delete this file"
}
```

#### 404 Not Found
```json
{
  "message": "Bird not found"
}
```

#### 500 Internal Server Error
```json
{
  "message": "Failed to update profile. Please try again."
}
```

---

## ?? Security Notes

1. **Pre-signed URLs expire in 10 minutes** - Don't cache upload URLs
2. **Download URLs expire in 10 minutes** - Implement auto-refresh for display
3. **Always use HTTPS** - Never downgrade to HTTP
4. **Validate file types** - Only upload supported extensions
5. **Users can only delete their own files** - API enforces ownership
6. **Keep JWT tokens secure** - Store in secure storage (Keychain/Keystore)

---

## ?? S3 Folder Structure

```
amzn-s3-wihngo-bucket/
??? users/
?   ??? profile-images/{userId}/{uuid}.jpg
?   ??? stories/{userId}/{storyId}/{uuid}.jpg
?   ??? videos/{userId}/{uuid}.mp4
??? birds/
    ??? profile-images/{birdId}/{uuid}.jpg
    ??? videos/{birdId}/{uuid}.mp4
```

---

## ?? Quick Start Checklist

- [ ] Get JWT token from `/api/auth/login`
- [ ] Request upload URL from `/api/media/upload-url`
- [ ] Upload file to S3 using the pre-signed URL (PUT request)
- [ ] Update resource with S3 key
- [ ] Display images using `imageUrl` from API responses
- [ ] Implement URL refresh before 10-minute expiration
- [ ] Handle errors gracefully with retry logic

---

## ?? Support

For API issues or questions:
- Backend Repository: https://github.com/fullstackragab/wihngo-api
- Report issues via GitHub Issues
- Check API logs for detailed error messages

---

## ?? API Base URLs

**Production:** `https://api.wihngo.com`  
**Development:** `https://dev-api.wihngo.com` (if applicable)

---

## ?? Version

**API Version:** 1.0  
**Last Updated:** January 2024  
**S3 Integration:** Active

---

## ?? Additional Resources

- AWS S3 Documentation: https://docs.aws.amazon.com/s3/
- Expo ImagePicker: https://docs.expo.dev/versions/latest/sdk/imagepicker/
- React Native Image: https://reactnative.dev/docs/image

---

**Happy Coding! ??**
