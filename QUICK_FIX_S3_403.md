# ?? Quick Fix: S3 Upload 403 Error

## ? TL;DR

**Problem:** Upload to S3 returns 403  
**Cause:** Missing CORS configuration  
**Fix:** 2 steps below

---

## Step 1: Configure S3 CORS (AWS Console)

1. Open: https://s3.console.aws.amazon.com
2. Click: `amzn-s3-wihngo-bucket`
3. Tab: **Permissions**
4. Section: **Cross-origin resource sharing (CORS)**
5. Click: **Edit**
6. Paste & Save:

```json
[{"AllowedHeaders":["*"],"AllowedMethods":["GET","PUT","POST","DELETE","HEAD"],"AllowedOrigins":["*"],"ExposeHeaders":["ETag"],"MaxAgeSeconds":3000}]
```

---

## Step 2: Update Mobile Upload Code

### ? Correct Upload Code

```typescript
const s3Response = await fetch(uploadUrl, {
  method: 'PUT',
  headers: {
    'Content-Type': 'image/jpeg', // Match file type
  },
  body: blob, // Don't modify after fetch
});
```

### ? Common Mistakes

```typescript
// DON'T add Authorization header
headers: {
  'Authorization': 'Bearer token', // ? Causes 403
  'Content-Type': 'image/jpeg',
}

// DON'T add x-amz-* headers
headers: {
  'x-amz-acl': 'public-read', // ? Causes signature mismatch
  'Content-Type': 'image/jpeg',
}

// DON'T use wrong Content-Type
headers: {
  'Content-Type': 'application/json', // ? Must match actual file
}
```

---

## ?? Test It

```typescript
// 1. Get upload URL
const { uploadUrl, s3Key } = await apiHelper.post('/api/media/upload-url', {
  mediaType: 'profile-image',
  fileExtension: '.jpg'
});

// 2. Upload to S3
const blob = await (await fetch(imageUri)).blob();
const response = await fetch(uploadUrl, {
  method: 'PUT',
  headers: { 'Content-Type': 'image/jpeg' },
  body: blob
});

console.log(response.status); // Should be 200
```

---

## ? Checklist

- [ ] CORS added to S3 bucket
- [ ] Only `Content-Type` header in upload request
- [ ] No `Authorization` or `x-amz-*` headers
- [ ] Content-Type matches file type
- [ ] Backend restarted

---

## ?? Still 403?

1. Wait 2-3 minutes (CORS propagation)
2. Check S3 bucket name: `amzn-s3-wihngo-bucket`
3. Verify region: `us-east-1`
4. Check IAM permissions (see full guide)

---

?? **Full Guide:** See `MOBILE_S3_UPLOAD_FIX.md`
