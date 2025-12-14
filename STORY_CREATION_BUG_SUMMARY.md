# Story Creation Bug - Root Cause & Solution

## ?? Issue

**Error:** 400 Bad Request when creating story  
**Message:** "Story can have either an image or a video, not both"

## ?? Root Cause

The mobile app (`create-story.tsx`) is:
1. Allowing user to select BOTH photo AND video
2. Uploading BOTH to S3
3. Sending BOTH keys to API

```javascript
// Current behavior - WRONG ?
{
  imageS3Key: "users/stories/.../image.png",
  videoS3Key: "users/videos/.../video.mp4"  // Both sent!
}
```

The API (correctly) rejects this because stories can only have **ONE** media type.

## ? Solution

**Mobile app must enforce: ONE media type only**

### UI Changes Required

**Before (Current - Wrong):**
- User can select photo
- User can ALSO select video
- Both get uploaded and sent

**After (Correct):**
- User selects photo ? Video button disabled/hidden
- User selects video ? Photo button disabled/hidden  
- Only ONE media type uploaded and sent

### Code Changes Required

**File:** `C:\expo\wihngo\app\create-story.tsx`

**Changes:**
1. Add `mediaType` state to track which type user selected
2. Clear opposite media when user selects one type
3. Only upload the selected media type
4. Only send ONE media key in API request
5. Update UI to hide opposite button when media selected

**Detailed fix:** See `MOBILE_CREATE_STORY_QUICK_FIX.md`

## ?? Current Flow vs. Correct Flow

### Current Flow ?
```
User picks photo ? image stored
User picks video ? video ALSO stored  
Upload image ? imageS3Key
Upload video ? videoS3Key
API call ? { imageS3Key, videoS3Key }  // ERROR!
```

### Correct Flow ?
```
User picks photo ? image stored, video cleared
Upload image ? imageS3Key
API call ? { imageS3Key }  // SUCCESS!

OR

User picks video ? video stored, image cleared
Upload video ? videoS3Key
API call ? { videoS3Key }  // SUCCESS!
```

## ?? Expected API Requests

### With Photo Only ?
```json
POST /api/stories
{
  "birdIds": ["uuid1", "uuid2"],
  "content": "Story text",
  "mode": "NewBeginning",
  "imageS3Key": "users/stories/.../image.png"
}
```

### With Video Only ?
```json
POST /api/stories
{
  "birdIds": ["uuid1", "uuid2"],
  "content": "Story text",
  "mode": "NewBeginning",
  "videoS3Key": "users/stories/.../video.mp4"
}
```

### With No Media ?
```json
POST /api/stories
{
  "birdIds": ["uuid1", "uuid2"],
  "content": "Story text",
  "mode": "NewBeginning"
}
```

### With Both Media ?
```json
POST /api/stories
{
  "birdIds": ["uuid1", "uuid2"],
  "content": "Story text",
  "mode": "NewBeginning",
  "imageS3Key": "users/stories/.../image.png",
  "videoS3Key": "users/stories/.../video.mp4"  // ? ERROR
}

Response: 400 Bad Request
{
  "message": "Story can have either an image or a video, not both"
}
```

## ?? Backend Validation (Already Implemented)

The API has validation that:
- ? Rejects requests with both imageS3Key and videoS3Key
- ? Returns clear error message
- ? Accepts one or neither media key

**This validation is correct and should NOT be changed.**

## ?? Mobile Team Action Items

- [ ] Update `create-story.tsx` to enforce one media type only
- [ ] Add `mediaType` state
- [ ] Clear opposite media when user selects one
- [ ] Only upload selected media type
- [ ] Only send one media key to API
- [ ] Update UI to show/hide buttons based on selection
- [ ] Test all scenarios (photo only, video only, no media)
- [ ] Verify 400 error no longer occurs

## ?? Estimated Fix Time

- **Time:** 15-30 minutes
- **Complexity:** Low (UI logic update)
- **Files:** 1 file (`create-story.tsx`)
- **Lines:** ~30-40 lines changed

## ?? Questions?

If you need help implementing this fix:
1. Check `MOBILE_CREATE_STORY_QUICK_FIX.md` for copy-paste code
2. Check `URGENT_MOBILE_BOTH_MEDIA_BUG.md` for detailed explanation
3. Check `MOBILE_STORY_API_GUIDE.md` for full API documentation

---

**Status:** ?? Awaiting Mobile Team Fix  
**Priority:** ?? HIGH (Blocking story creation)  
**Root Cause:** Mobile app UI/logic issue  
**API Status:** ? Working correctly
