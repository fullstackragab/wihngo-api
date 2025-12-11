# Bird Video and Image Migration - Execution Guide

## Overview
This migration makes both **main image** and **main video** mandatory for all bird profiles.

## Changes Made

### Database Changes
- Added `video_url` column to `birds` table (VARCHAR(1000), NOT NULL)
- Made `image_url` column NOT NULL
- Added index on `video_url` for performance
- Existing birds will have empty string values for `video_url`

### Code Changes
- **Models/Bird.cs**: Added required `VideoUrl` property, made `ImageUrl` required
- **Dtos/BirdCreateDto.cs**: Added required `VideoUrl` and `ImageUrl` fields
- **Dtos/BirdSummaryDto.cs**: Added `VideoUrl` property
- **Dtos/BirdProfileDto.cs**: Added `VideoUrl` property
- **Controllers/BirdsController.cs**: Updated to handle `VideoUrl` in GET, POST, and PUT operations
- **Profiles/MappingProfile.cs**: Added `VideoUrl` mappings for AutoMapper

## Execution Instructions

### Option 1: Using psql (Command Line)

#### Windows PowerShell
```powershell
# Navigate to project directory
cd C:\.net\Wihngo

# Execute migration
$env:PGPASSWORD='postgres'; psql -h localhost -U postgres -d wihngo -f Database/migrations/add_bird_video_url.sql; $env:PGPASSWORD=$null
```

#### Linux/Mac Terminal
```bash
cd /path/to/Wihngo

PGPASSWORD='postgres' psql -h localhost -U postgres -d wihngo -f Database/migrations/add_bird_video_url.sql
```

### Option 2: Using pgAdmin (GUI)

1. Open **pgAdmin**
2. Connect to your PostgreSQL server
3. Right-click on **wihngo** database ? **Query Tool**
4. **File ? Open** ? Navigate to: `C:\.net\Wihngo\Database\migrations\add_bird_video_url.sql`
5. Click **Execute** (? button) or press **F5**
6. Check the Messages tab for success confirmation

### Option 3: Direct SQL Execution

If you prefer to execute directly, here's the SQL:

```sql
BEGIN;

ALTER TABLE birds 
ADD COLUMN IF NOT EXISTS video_url VARCHAR(1000);

UPDATE birds 
SET image_url = '' 
WHERE image_url IS NULL;

ALTER TABLE birds 
ALTER COLUMN image_url SET NOT NULL;

UPDATE birds 
SET video_url = '' 
WHERE video_url IS NULL;

ALTER TABLE birds 
ALTER COLUMN video_url SET NOT NULL;

CREATE INDEX IF NOT EXISTS idx_birds_video_url ON birds(video_url);

COMMIT;
```

## Verification

### 1. Check Column Was Added
```sql
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'birds' 
  AND column_name IN ('image_url', 'video_url');
```

**Expected Result:**
```
 column_name | data_type         | is_nullable
-------------+-------------------+-------------
 image_url   | character varying | NO
 video_url   | character varying | NO
```

### 2. Check Existing Birds
```sql
SELECT bird_id, name, 
       CASE WHEN image_url = '' THEN 'EMPTY' ELSE 'HAS VALUE' END as image_status,
       CASE WHEN video_url = '' THEN 'EMPTY' ELSE 'HAS VALUE' END as video_status
FROM birds;
```

### 3. Restart Application
```bash
# Stop the application (Ctrl+C if running in terminal)
# Or in Visual Studio: Stop Debugging (Shift+F5)

# Start it again
dotnet run
# Or in Visual Studio: Start (F5)
```

### 4. Test API

#### Test 1: Create a New Bird (Should Require Both)
```bash
curl -X POST "http://localhost:5000/api/birds" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Tweety",
    "species": "Canary",
    "tagline": "I tawt I taw a puddy tat!",
    "description": "A cute yellow bird",
    "imageUrl": "https://example.com/tweety.jpg",
    "videoUrl": "https://example.com/tweety.mp4"
  }'
```

**Expected Response:** `201 Created` with bird details

#### Test 2: Create Without Video (Should Fail)
```bash
curl -X POST "http://localhost:5000/api/birds" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Tweety",
    "species": "Canary",
    "imageUrl": "https://example.com/tweety.jpg"
  }'
```

**Expected Response:** `400 Bad Request` with validation error for `VideoUrl`

#### Test 3: Get Birds List
```bash
curl -X GET "http://localhost:5000/api/birds"
```

**Expected Response:** JSON array including `videoUrl` field for each bird

## Important Notes

### For Existing Birds
?? **All existing birds will have empty `video_url` values after migration!**

Bird owners need to update their birds to add video URLs. You have two options:

#### Option A: Enforce immediately (recommended for new features)
- Existing birds without videos won't display properly
- Owners must update via PUT endpoint to add videos

#### Option B: Allow graceful transition
- Modify frontend to show "Upload video" prompt for birds with empty `video_url`
- Allow birds to be displayed with placeholder video until owner uploads

### Update Existing Birds
```bash
# Update a bird to add video URL
curl -X PUT "http://localhost:5000/api/birds/{birdId}" \
  -H "Authorization: Bearer OWNER_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Existing Bird",
    "species": "Robin",
    "imageUrl": "https://example.com/existing.jpg",
    "videoUrl": "https://example.com/existing.mp4",
    "tagline": "Updated with video",
    "description": "Now has a video!"
  }'
```

## Rollback Instructions

If you need to rollback this migration:

```powershell
# Windows PowerShell
$env:PGPASSWORD='postgres'; psql -h localhost -U postgres -d wihngo -f Database/migrations/rollback_bird_video_url.sql; $env:PGPASSWORD=$null
```

Or execute `rollback_bird_video_url.sql` via pgAdmin.

## Troubleshooting

### Problem: "column already exists"
**Solution:** The column was already added. Check if migration ran before:
```sql
SELECT column_name FROM information_schema.columns 
WHERE table_name = 'birds' AND column_name = 'video_url';
```

### Problem: Application won't start
**Solution:** 
1. Check for compilation errors: `dotnet build`
2. Check database connection in appsettings.json
3. Verify migration completed: Check column exists in database

### Problem: Validation errors when creating birds
**Solution:** This is expected! Both `imageUrl` and `videoUrl` are now required in the request body.

## Success Indicators

? SQL script completes without errors  
? `video_url` column exists in `birds` table  
? Both `image_url` and `video_url` are NOT NULL  
? Index `idx_birds_video_url` created  
? Application compiles and starts  
? POST requests require both image and video URLs  
? GET requests return `videoUrl` in response  
? PUT requests can update `videoUrl`  

## Next Steps

1. **Update Frontend/Mobile Apps** to:
   - Add video upload UI for bird creation
   - Display video in bird profiles
   - Show "Add Video" prompt for existing birds without videos
   - Validate video URL before submission

2. **Notify Users** about the new requirement:
   - Send notification to bird owners
   - Add banner/message in app about video requirement
   - Provide video upload guidelines (format, size, duration)

3. **Consider Adding**:
   - Video format validation
   - Video thumbnail generation
   - Video processing/transcoding service
   - Maximum video size/duration limits

---

**Migration Created:** December 2024  
**Status:** Ready to Execute  
**Requires:** PostgreSQL database with existing `birds` table
