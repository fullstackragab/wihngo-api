# S3 Integration Migration Guide

## Overview
This guide explains the changes made to integrate AWS S3 for media storage across the Wihngo application. All media uploads (user profile images, bird images/videos, and story images) now use S3 with pre-signed URLs.

---

## Breaking Changes

### 1. User Profile API Changes

#### **PUT /api/users/profile** - Update User Profile
**OLD Request:**
```json
{
  "name": "John Doe",
  "profileImage": "https://example.com/image.jpg",
  "bio": "Bird lover"
}
```

**NEW Request:**
```json
{
  "name": "John Doe",
  "profileImageS3Key": "users/profile-images/{userId}/{uuid}.jpg",
  "bio": "Bird lover"
}
```

**Response Changes:**
```json
{
  "userId": "...",
  "name": "John Doe",
  "email": "john@example.com",
  "profileImageS3Key": "users/profile-images/{userId}/{uuid}.jpg",
  "profileImageUrl": "https://s3.amazonaws.com/...",  // Pre-signed URL (expires in 10 min)
  "bio": "Bird lover",
  "emailConfirmed": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### 2. Birds API Changes

#### **POST /api/birds** - Create Bird
**OLD Request:**
```json
{
  "name": "Ruby",
  "species": "Hummingbird",
  "tagline": "Fast and furious",
  "description": "A beautiful bird",
  "imageUrl": "https://example.com/bird.jpg",
  "videoUrl": "https://example.com/bird.mp4"
}
```

**NEW Request:**
```json
{
  "name": "Ruby",
  "species": "Hummingbird",
  "tagline": "Fast and furious",
  "description": "A beautiful bird",
  "imageS3Key": "birds/profile-images/{birdId}/{uuid}.jpg",
  "videoS3Key": "birds/videos/{birdId}/{uuid}.mp4"
}
```

#### **GET /api/birds** - List Birds
**Response Changes:**
```json
[
  {
    "birdId": "...",
    "name": "Ruby",
    "species": "Hummingbird",
    "imageS3Key": "birds/profile-images/{birdId}/{uuid}.jpg",
    "imageUrl": "https://s3.amazonaws.com/...",  // Pre-signed URL
    "videoS3Key": "birds/videos/{birdId}/{uuid}.mp4",
    "videoUrl": "https://s3.amazonaws.com/...",  // Pre-signed URL
    "tagline": "Fast and furious",
    "lovedBy": 10,
    "supportedBy": 5,
    "ownerId": "...",
    "isLoved": false
  }
]
```

#### **GET /api/birds/{id}** - Get Bird Profile
**Response Changes:**
- Added `imageS3Key` and `videoS3Key` fields
- `imageUrl` and `videoUrl` now contain pre-signed URLs (expire in 10 minutes)

### 3. Stories API Changes

#### **POST /api/stories** - Create Story
**OLD Request:**
```json
{
  "birdId": "...",
  "content": "Ruby visited the feeder today!",
  "imageUrl": "https://example.com/story.jpg"
}
```

**NEW Request:**
```json
{
  "birdId": "...",
  "content": "Ruby visited the feeder today!",
  "imageS3Key": "users/stories/{userId}/{storyId}/{uuid}.jpg"
}
```

#### **PUT /api/stories/{id}** - Update Story
**NEW Request:**
```json
{
  "content": "Updated story content",
  "imageS3Key": "users/stories/{userId}/{storyId}/{uuid}.jpg"
}
```
- Set `imageS3Key` to empty string to remove the image
- Omit `imageS3Key` to keep existing image

#### **GET /api/stories** - List Stories
**Response Changes:**
```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 50,
  "items": [
    {
      "storyId": "...",
      "title": "Ruby visited the feeder...",
      "bird": "Ruby",
      "date": "January 15, 2024",
      "preview": "Ruby visited the feeder today!...",
      "imageS3Key": "users/stories/{userId}/{storyId}/{uuid}.jpg",
      "imageUrl": "https://s3.amazonaws.com/..."  // Pre-signed URL
    }
  ]
}
```

---

## Migration Steps

### Step 1: Configure AWS Credentials

Add to your `appsettings.json`:
```json
{
  "AWS": {
    "AccessKeyId": "your-access-key-id",
    "SecretAccessKey": "your-secret-access-key",
    "BucketName": "amzn-s3-wihngo-bucket",
    "Region": "us-east-1",
    "PresignedUrlExpirationMinutes": 10
  }
}
```

Or set environment variables:
- `AWS__AccessKeyId`
- `AWS__SecretAccessKey`

### Step 2: Update Client Application Flow

#### New Upload Flow:
```javascript
// 1. Request upload URL from backend
const uploadResponse = await fetch('/api/media/upload-url', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    mediaType: 'profile-image', // or 'bird-profile-image', 'bird-video', 'story-image'
    fileExtension: '.jpg',
    relatedId: birdId // Required for bird/story media
  })
});

const { uploadUrl, s3Key } = await uploadResponse.json();

// 2. Upload directly to S3
const file = await pickImage();
await fetch(uploadUrl, {
  method: 'PUT',
  headers: { 'Content-Type': 'image/jpeg' },
  body: file
});

// 3. Update resource with S3 key
await fetch('/api/users/profile', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    profileImageS3Key: s3Key
  })
});
```

### Step 3: Data Migration (if you have existing data)

If you have existing media URLs stored in the database:

1. **Option A: Keep old URLs and gradually migrate**
   - Old URLs will continue to work
   - New uploads will use S3
   - Gradually migrate old media to S3

2. **Option B: Bulk migration**
   - Download all existing media from current storage
   - Upload to S3 using the media upload endpoints
   - Update database records with new S3 keys

**SQL Migration Example:**
```sql
-- Backup old URLs (if needed)
ALTER TABLE users ADD COLUMN profile_image_backup TEXT;
UPDATE users SET profile_image_backup = profile_image;

-- Note: You'll need to upload files to S3 first, then update these values
-- This is a template - actual S3 keys will be generated by the upload process
UPDATE users 
SET profile_image = 'users/profile-images/' || user_id || '/' || gen_random_uuid() || '.jpg'
WHERE profile_image IS NOT NULL;
```

---

## API Endpoints Reference

### Media Management

#### Generate Upload URL
```
POST /api/media/upload-url
Authorization: Bearer {token}

Request:
{
  "mediaType": "profile-image",
  "fileExtension": ".jpg",
  "relatedId": "optional-guid"
}

Response:
{
  "uploadUrl": "https://s3.amazonaws.com/...",
  "s3Key": "users/profile-images/{userId}/{uuid}.jpg",
  "expiresAt": "2024-01-15T10:40:00Z",
  "instructions": "Use PUT request to upload the file to the uploadUrl"
}
```

#### Generate Download URL
```
POST /api/media/download-url
Authorization: Bearer {token}

Request:
{
  "s3Key": "users/profile-images/{userId}/{uuid}.jpg"
}

Response:
{
  "downloadUrl": "https://s3.amazonaws.com/...",
  "expiresAt": "2024-01-15T10:40:00Z"
}
```

#### Delete Media
```
DELETE /api/media?s3Key=users/profile-images/{userId}/{uuid}.jpg
Authorization: Bearer {token}

Response:
{
  "message": "File deleted successfully"
}
```

### Valid Media Types

| Media Type | Description | Requires relatedId | S3 Path Pattern |
|------------|-------------|-------------------|------------------|
| `profile-image` | User profile image | No | `users/profile-images/{userId}/{uuid}.jpg` |
| `story-image` | Story image | Yes (storyId) | `users/stories/{userId}/{storyId}/{uuid}.jpg` |
| `story-video` | Story video | No | `users/videos/{userId}/{uuid}.mp4` |
| `bird-profile-image` | Bird profile image | Yes (birdId) | `birds/profile-images/{birdId}/{uuid}.jpg` |
| `bird-video` | Bird video | Yes (birdId) | `birds/videos/{birdId}/{uuid}.mp4` |

### Valid File Extensions

**Images:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`  
**Videos:** `.mp4`, `.mov`, `.avi`, `.webm`

---

## Security Notes

1. **Pre-signed URLs Expire:** All URLs expire after 10 minutes (configurable)
2. **Authentication Required:** All media endpoints require JWT bearer token
3. **User Validation:** Users can only delete their own files
4. **File Validation:** Files are verified to exist in S3 before updating database

---

## Error Handling

### Common Errors

#### 400 Bad Request - Invalid S3 Key
```json
{
  "message": "Invalid profile image S3 key. Must belong to your user account."
}
```

#### 400 Bad Request - File Not Found
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

---

## Testing

### Test Upload Flow
```bash
# 1. Get upload URL
curl -X POST https://api.wihngo.com/api/media/upload-url \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "mediaType": "profile-image",
    "fileExtension": ".jpg"
  }'

# 2. Upload to S3 (use the URL from step 1)
curl -X PUT "PRESIGNED_UPLOAD_URL" \
  -H "Content-Type: image/jpeg" \
  --data-binary @profile.jpg

# 3. Update user profile with S3 key
curl -X PUT https://api.wihngo.com/api/users/profile \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "profileImageS3Key": "users/profile-images/USER_ID/UUID.jpg"
  }'
```

---

## Performance Considerations

1. **Direct Upload:** Files go directly to S3, bypassing the backend server
2. **Pre-signed URLs:** No need to proxy files through the backend
3. **URL Caching:** Cache pre-signed URLs for up to 10 minutes on the client
4. **Bulk Operations:** Use the bulk URL generation method for lists

---

## Rollback Plan

If you need to rollback the S3 integration:

1. **Database:** Old URLs are not deleted, just replaced
2. **Code:** Keep old code in a branch
3. **Media:** Files in S3 remain accessible via their keys

To revert:
1. Restore old controller code
2. Update DTOs to use old field names
3. Keep S3 bucket for future use (don't delete uploaded files)

---

## Support

For issues or questions:
- Check logs: `Wihngo.Services.S3Service` and `Wihngo.Controllers.MediaController`
- Review AWS S3 bucket permissions
- Verify AWS credentials are correctly configured
- Ensure IAM user has correct permissions (see AWS_S3_MEDIA_GUIDE.md)
