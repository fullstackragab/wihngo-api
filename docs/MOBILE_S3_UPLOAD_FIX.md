# S3 Upload 403 Error - Fix Guide for Mobile Team

## ?? Issue: "S3 upload failed: 403"

This error occurs when uploading to the S3 pre-signed URL. Here's how to fix it.

---

## ? Backend Fixes (Already Applied)

### 1. S3 Service Updated
- Removed `ContentType` from pre-signed URL generation
- This allows flexible Content-Type headers from the mobile app

### 2. MediaController Enhanced
- Now returns recommended `Content-Type` in the response

---

## ?? S3 Bucket CORS Configuration (Required!)

**IMPORTANT:** You MUST configure CORS on your S3 bucket first!

### AWS Console Steps:

1. Go to **S3 Console**: https://s3.console.aws.amazon.com
2. Click bucket: `amzn-s3-wihngo-bucket`
3. Go to **"Permissions"** tab
4. Scroll to **"Cross-origin resource sharing (CORS)"**
5. Click **"Edit"**
6. Paste this configuration:

```json
[
    {
        "AllowedHeaders": [
            "*"
        ],
        "AllowedMethods": [
            "GET",
            "PUT",
            "POST",
            "DELETE",
            "HEAD"
        ],
        "AllowedOrigins": [
            "*"
        ],
        "ExposeHeaders": [
            "ETag",
            "x-amz-server-side-encryption",
            "x-amz-request-id",
            "x-amz-id-2"
        ],
        "MaxAgeSeconds": 3000
    }
]
```

7. Click **"Save changes"**

**?? Without CORS configuration, ALL uploads will fail with 403!**

---

## ?? Mobile App Fix (React Native/Expo)

### Updated Upload Code

Replace your current S3 upload code with this:

```typescript
// services/media.service.ts

interface UploadResponse {
  uploadUrl: string;
  s3Key: string;
  expiresAt: string;
  instructions: string; // Contains recommended Content-Type
}

export async function uploadImageToS3(
  imageUri: string,
  mediaType: 'profile-image' | 'story-image' | 'bird-profile-image',
  relatedId?: string
): Promise<string> {
  try {
    console.log('?? Starting S3 upload...');
    
    // Step 1: Get upload URL from backend
    console.log('?? Step 1: Getting upload URL...');
    const fileExtension = imageUri.split('.').pop() || 'jpg';
    
    const uploadUrlResponse = await apiHelper.post<UploadResponse>(
      '/api/media/upload-url',
      {
        mediaType,
        fileExtension: `.${fileExtension}`,
        relatedId: relatedId || null,
      }
    );

    const { uploadUrl, s3Key, instructions } = uploadUrlResponse;
    console.log('? Got upload URL:', uploadUrl);
    console.log('?? Instructions:', instructions);

    // Step 2: Prepare the file for upload
    console.log('?? Step 2: Preparing file...');
    
    // Get file info
    const response = await fetch(imageUri);
    const blob = await response.blob();
    
    // Determine Content-Type based on file extension
    const contentType = getContentType(fileExtension);
    console.log('?? Content-Type:', contentType);
    console.log('?? Blob size:', blob.size, 'bytes');

    // Step 3: Upload directly to S3
    console.log('?? Step 3: Uploading to S3...');
    
    // CRITICAL: Content-Type MUST match the file extension you sent to the backend
    // The backend generates the signature with this exact Content-Type
    const s3Response = await fetch(uploadUrl, {
      method: 'PUT',
      headers: {
        'Content-Type': contentType, // MUST match! Don't add other headers!
      },
      body: blob,
    });

    console.log('?? S3 Response Status:', s3Response.status);

    if (!s3Response.ok) {
      const errorText = await s3Response.text();
      console.error('? S3 upload failed:', {
        status: s3Response.status,
        statusText: s3Response.statusText,
        body: errorText,
      });
      throw new Error(`S3 upload failed: ${s3Response.status} - ${errorText}`);
    }

    console.log('? Upload successful!');
    return s3Key;
  } catch (error) {
    console.error('? Error uploading to S3:', error);
    throw error;
  }
}

function getContentType(extension: string): string {
  const ext = extension.toLowerCase().replace('.', '');
  
  const contentTypes: Record<string, string> = {
    'jpg': 'image/jpeg',
    'jpeg': 'image/jpeg',
    'png': 'image/png',
    'gif': 'image/gif',
    'webp': 'image/webp',
    'mp4': 'video/mp4',
    'mov': 'video/quicktime',
    'avi': 'video/x-msvideo',
    'webm': 'video/webm',
  };

  return contentTypes[ext] || 'application/octet-stream';
}
```

**?? IMPORTANT:** The `Content-Type` you send MUST exactly match what the backend expects based on the file extension you provided!

---

## ?? Common Issues & Solutions

### Issue 1: Still Getting 403 After CORS Configuration
**Cause:** CORS changes can take a few minutes to propagate  
**Solution:** 
- Wait 2-3 minutes
- Clear mobile app cache
- Try again

### Issue 2: 403 with "SignatureDoesNotMatch" in S3 response
**Cause:** Extra headers or modified body in the request  
**Solution:**
- Only set `Content-Type` header
- Don't add `Authorization`, `x-amz-*`, or any other headers
- Don't modify the blob/body after fetching it

### Issue 3: Works in Development, Fails in Production
**Cause:** Different app origins  
**Solution:**
- Update CORS `AllowedOrigins` to include production domain
- Or keep `"*"` for all origins (less secure but works everywhere)

---

## ?? Testing the Fix

### Test 1: Verify CORS is Applied

```bash
# Test if CORS headers are returned
curl -X OPTIONS https://amzn-s3-wihngo-bucket.s3.us-east-1.amazonaws.com \
  -H "Origin: http://localhost:8081" \
  -H "Access-Control-Request-Method: PUT" \
  -v
```

Look for these headers in the response:
- `Access-Control-Allow-Origin: *`
- `Access-Control-Allow-Methods: GET, PUT, POST, DELETE, HEAD`

### Test 2: Test Upload with cURL

```bash
# 1. Get upload URL from your API
curl -X POST https://your-api.com/api/media/upload-url \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "mediaType": "profile-image",
    "fileExtension": ".jpg"
  }'

# 2. Upload to S3 (use the uploadUrl from step 1)
curl -X PUT "PASTE_UPLOAD_URL_HERE" \
  -H "Content-Type: image/jpeg" \
  --data-binary @test-image.jpg
```

### Test 3: Test in Expo App

```typescript
// Add this test function
async function testS3Upload() {
  try {
    // Pick a test image
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      allowsEditing: true,
      quality: 0.8,
    });

    if (result.canceled) return;

    console.log('?? Testing S3 upload...');
    const s3Key = await uploadImageToS3(
      result.assets[0].uri,
      'profile-image'
    );
    
    console.log('? Test passed! S3 Key:', s3Key);
    Alert.alert('Success', `Upload successful!\nS3 Key: ${s3Key}`);
  } catch (error) {
    console.error('? Test failed:', error);
    Alert.alert('Test Failed', String(error));
  }
}
```

---

## ?? Checklist

Before testing:
- [ ] CORS configured on S3 bucket
- [ ] Backend updated (S3Service and MediaController)
- [ ] Backend restarted
- [ ] Mobile upload code updated
- [ ] Content-Type header set correctly
- [ ] No extra headers added to S3 upload request

After upload:
- [ ] Check backend logs for "Generated upload URL"
- [ ] Check mobile logs for S3 response status
- [ ] Verify file appears in S3 bucket
- [ ] Test download URL generation

---

## ?? Security Notes

### Current CORS Configuration (Development)
```json
"AllowedOrigins": ["*"]  // Allows all origins
```

### Production CORS Configuration (Recommended)
```json
"AllowedOrigins": [
  "https://your-production-domain.com",
  "https://www.your-production-domain.com"
]
```

Update this when you move to production!

---

## ?? Expected Upload Flow

```
Mobile App
   ?
1. POST /api/media/upload-url
   ? { uploadUrl, s3Key, instructions }
   ?
2. PUT {uploadUrl}
   Headers: { Content-Type: image/jpeg }
   Body: image blob
   ?
3. S3 responds with 200 OK
   ?
4. PUT /api/users/profile
   Body: { profileImageS3Key: s3Key }
   ?
5. Backend verifies file exists in S3
   ?
6. Profile updated with new image
```

---

## ?? Still Not Working?

### Check S3 Bucket Policy

Make sure your bucket policy allows PutObject:

1. Go to S3 Console
2. Click bucket: `amzn-s3-wihngo-bucket`
3. Go to **"Permissions"** tab
4. Check **"Bucket policy"**
5. Ensure it allows `s3:PutObject` for your IAM user

Example bucket policy:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "AWS": "arn:aws:iam::YOUR_ACCOUNT_ID:user/wihngo-media-signer"
      },
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::amzn-s3-wihngo-bucket/*"
    }
  ]
}
```

### Enable S3 Server Access Logging

To debug 403 errors:

1. Go to S3 bucket properties
2. Enable **"Server access logging"**
3. Check logs for detailed error information

---

## ?? Need Help?

If the issue persists:
1. Share the full error response from S3
2. Check S3 server logs (if enabled)
3. Verify IAM user permissions
4. Test with AWS CLI to isolate the issue

```bash
# Test with AWS CLI
aws s3 presign s3://amzn-s3-wihngo-bucket/test.jpg \
  --expires-in 600

# Try uploading with the generated URL
curl -X PUT "PRESIGNED_URL" \
  -H "Content-Type: image/jpeg" \
  --data-binary @test.jpg
```

Good luck! ??
