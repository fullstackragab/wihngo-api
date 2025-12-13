# AWS S3 Media Management

## Overview
This application uses AWS S3 for managing all media uploads and downloads, including:
- User profile images
- Story images and videos
- Bird profile images and videos

## Configuration

### appsettings.json
Add the following configuration to your `appsettings.json`:

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

### Environment Variables
Alternatively, you can use environment variables:
- `AWS__AccessKeyId`
- `AWS__SecretAccessKey`
- `AWS__BucketName`
- `AWS__Region`

## Folder Structure in S3

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

## API Endpoints

### 1. Generate Upload URL
Generate a pre-signed URL for uploading media to S3.

**Endpoint:** `POST /api/media/upload-url`

**Authorization:** Bearer token required

**Request Body:**
```json
{
  "mediaType": "profile-image",
  "fileExtension": ".jpg",
  "relatedId": "optional-guid-for-stories-and-birds"
}
```

**Valid Media Types:**
- `profile-image` - User profile image
- `story-image` - Story image (requires `relatedId` as `storyId`)
- `story-video` - Story video
- `bird-profile-image` - Bird profile image (requires `relatedId` as `birdId`)
- `bird-video` - Bird video (requires `relatedId` as `birdId`)

**Valid File Extensions:**
- Images: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`
- Videos: `.mp4`, `.mov`, `.avi`, `.webm`

**Response:**
```json
{
  "uploadUrl": "https://s3.amazonaws.com/...",
  "s3Key": "users/profile-images/{userId}/{uuid}.jpg",
  "expiresAt": "2024-01-15T10:30:00Z",
  "instructions": "Use PUT request to upload the file to the uploadUrl"
}
```

### 2. Generate Download URL
Generate a pre-signed URL for downloading/viewing media from S3.

**Endpoint:** `POST /api/media/download-url`

**Authorization:** Bearer token required

**Request Body:**
```json
{
  "s3Key": "users/profile-images/{userId}/{uuid}.jpg"
}
```

**Response:**
```json
{
  "downloadUrl": "https://s3.amazonaws.com/...",
  "expiresAt": "2024-01-15T10:30:00Z"
}
```

### 3. Delete Media
Delete a media file from S3.

**Endpoint:** `DELETE /api/media?s3Key=users/profile-images/{userId}/{uuid}.jpg`

**Authorization:** Bearer token required

**Response:**
```json
{
  "message": "File deleted successfully"
}
```

### 4. Check File Exists
Check if a media file exists in S3.

**Endpoint:** `GET /api/media/exists?s3Key=users/profile-images/{userId}/{uuid}.jpg`

**Authorization:** Bearer token required

**Response:**
```json
{
  "exists": true
}
```

## Usage Flow

### Upload Flow (React Native/Expo App)

```javascript
// 1. Request upload URL from backend
const response = await fetch('https://your-api.com/api/media/upload-url', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    mediaType: 'profile-image',
    fileExtension: '.jpg'
  })
});

const { uploadUrl, s3Key, expiresAt } = await response.json();

// 2. Upload file directly to S3 using PUT
const file = await pickImage(); // Your file picker logic
const uploadResponse = await fetch(uploadUrl, {
  method: 'PUT',
  headers: {
    'Content-Type': 'image/jpeg'
  },
  body: file
});

if (uploadResponse.ok) {
  // 3. Save s3Key to your database (User.ProfileImage field)
  await updateUserProfile({ profileImage: s3Key });
}
```

### Download/View Flow

```javascript
// 1. Request download URL from backend
const response = await fetch('https://your-api.com/api/media/download-url', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    s3Key: user.profileImage // Retrieved from database
  })
});

const { downloadUrl } = await response.json();

// 2. Use the download URL in your Image component
<Image source={{ uri: downloadUrl }} />
```

## Security Features

1. **Pre-signed URLs**: All URLs expire after 10 minutes (configurable)
2. **Authentication**: All endpoints require JWT bearer token
3. **User Validation**: Users can only delete files that belong to them
4. **Content-Type Validation**: Only allowed file types can be uploaded

## AWS IAM Configuration

### Required IAM Policy for wihngo-media-signer user:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:GetObjectMetadata",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::amzn-s3-wihngo-bucket",
        "arn:aws:s3:::amzn-s3-wihngo-bucket/*"
      ]
    }
  ]
}
```

## Testing

### Test Upload URL Generation
```bash
curl -X POST https://your-api.com/api/media/upload-url \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "mediaType": "profile-image",
    "fileExtension": ".jpg"
  }'
```

### Test File Upload to S3
```bash
curl -X PUT "PRESIGNED_UPLOAD_URL" \
  -H "Content-Type: image/jpeg" \
  --data-binary @image.jpg
```

## Database Updates

Store the S3 keys in your database:

- **User.ProfileImage**: `users/profile-images/{userId}/{uuid}.jpg`
- **Story.ImageUrl**: `users/stories/{userId}/{storyId}/{uuid}.jpg`
- **Bird.ImageUrl**: `birds/profile-images/{birdId}/{uuid}.jpg`
- **Bird.VideoUrl**: `birds/videos/{birdId}/{uuid}.mp4`

## Error Handling

The API returns standard HTTP status codes:
- `200 OK` - Success
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Missing or invalid JWT token
- `403 Forbidden` - User doesn't have permission to delete file
- `404 Not Found` - File doesn't exist
- `500 Internal Server Error` - Server error

## Performance Considerations

1. **Direct Upload**: Files are uploaded directly from client to S3, reducing server load
2. **Pre-signed URLs**: No need to proxy files through the backend
3. **Expiration**: URLs expire automatically for security
4. **CDN**: Consider adding CloudFront CDN for better performance

## Monitoring

The application logs all S3 operations with these log levels:
- Information: Successful operations
- Warning: Security violations (attempting to delete other users' files)
- Error: Failed operations with details

Check logs for:
- `Wihngo.Services.S3Service`
- `Wihngo.Controllers.MediaController`
