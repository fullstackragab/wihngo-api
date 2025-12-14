# Story API Update - Summary of Changes

## ? Completed Changes

### 1. Database Schema Updates

#### New StoryBird Junction Table
- **File:** `Models\StoryBird.cs`
- Creates many-to-many relationship between Stories and Birds
- Each story can now tag multiple birds

#### Updated Story Model
- **File:** `Models\Story.cs`
- ? Removed: `BirdId` (single bird)
- ? Added: `StoryBirds` collection (multiple birds)
- ? Added: `Mode` property (required StoryMode enum)
- ? Added: `VideoUrl` property (optional)
- ImageUrl is now optional

#### New StoryMode Enum
- **File:** `Models\Enums\StoryMode.cs`
- 8 story modes with descriptions:
  - `LoveAndBond` - ?? Love & Bond
  - `NewBeginning` - ?? New Beginning
  - `ProgressAndWins` - ?? Progress & Wins
  - `FunnyMoment` - ?? Funny Moment
  - `PeacefulMoment` - ??? Peaceful Moment
  - `LossAndMemory` - ?? Loss & Memory
  - `CareAndHealth` - ?? Care & Health
  - `DailyLife` - ?? Daily Life

### 2. DTO Updates

#### StoryCreateDto
- **File:** `Dtos\StoryCreateDto.cs`
- ? Removed: `BirdId`
- ? Added: `BirdIds` (List<Guid>, required, min 1)
- ? Added: `Mode` (StoryMode, required)
- ? Added: `VideoS3Key` (optional)

#### StoryUpdateDto
- **File:** `Dtos\StoryUpdateDto.cs`
- ? Added: `BirdIds` (List<Guid>, optional, min 1 if provided)
- ? Added: `Mode` (StoryMode, optional)
- ? Added: `VideoS3Key` (optional)

#### StoryReadDto
- **File:** `Dtos\StoryReadDto.cs`
- ? Removed: `Bird` (single)
- ? Added: `Birds` (List<BirdSummaryDto>)
- ? Added: `Mode` (StoryMode)
- ? Added: `VideoS3Key` and `VideoUrl`

#### StorySummaryDto
- **File:** `Dtos\StorySummaryDto.cs`
- ? Removed: `Title` property
- ? Removed: `Bird` (single string)
- ? Added: `Birds` (List<string> - bird names)
- ? Added: `Mode` (StoryMode)
- ? Added: `VideoS3Key` and `VideoUrl`

### 3. API Endpoints Updated

#### POST /api/stories
- **File:** `Controllers\StoriesController.cs`
- Validates multiple bird IDs
- Validates story mode
- Supports optional image and video
- Creates StoryBird records for each selected bird
- Sends notifications to users who love the tagged birds

#### PUT /api/stories/{id}
- Supports updating multiple birds
- Supports updating mode
- Supports updating/removing image
- Supports updating/removing video
- Handles old media file deletion

#### GET /api/stories
- Returns multiple bird names per story
- Returns story mode
- Returns video URLs if present

#### GET /api/stories/{id}
- Returns full bird objects (not just names)
- Returns story mode
- Returns video with pre-signed URL

#### DELETE /api/stories/{id}
- Deletes both image and video from S3

#### PATCH /api/stories/{id}/highlight
- Updated to work with multiple birds
- Validates user owns at least one bird in story

### 4. Database Context Updates

- **File:** `Data\AppDbContext.cs`
- Added `StoryBirds` DbSet
- Configured StoryBird relationships
- Added table mapping for `story_birds`

### 5. AutoMapper Updates

- **File:** `Profiles\MappingProfile.cs`
- Removed old Story mappings
- Story DTOs now manually mapped in controller for better control

### 6. User Controller Fix

- **File:** `Controllers\UsersController.cs`
- Updated Profile endpoint to work with new Story structure
- Returns multiple bird names per story
- Returns story mode

## ?? Migration Requirements

**IMPORTANT:** Before running the application, you need to update your database schema.

### Option 1: Drop and Recreate (Development Only)
```sql
DROP TABLE IF EXISTS story_birds CASCADE;
DROP TABLE IF EXISTS stories CASCADE;

-- Let Entity Framework recreate the tables
```

### Option 2: Manual Migration (Production)
```sql
-- Add new Mode column
ALTER TABLE stories ADD COLUMN mode INTEGER NOT NULL DEFAULT 7; -- DailyLife = 7

-- Add VideoUrl column  
ALTER TABLE stories ADD COLUMN video_url VARCHAR(1000);

-- Remove BirdId column
ALTER TABLE stories DROP COLUMN bird_id;

-- Create StoryBird junction table
CREATE TABLE story_birds (
    story_bird_id UUID PRIMARY KEY,
    story_id UUID NOT NULL REFERENCES stories(story_id) ON DELETE CASCADE,
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL,
    UNIQUE(story_id, bird_id)
);

CREATE INDEX idx_story_birds_story_id ON story_birds(story_id);
CREATE INDEX idx_story_birds_bird_id ON story_birds(bird_id);

-- Migrate existing data (if you have stories with bird_id)
-- This creates a story_bird record for each existing story
-- Note: Only run if you have existing data
INSERT INTO story_birds (story_bird_id, story_id, bird_id, created_at)
SELECT gen_random_uuid(), story_id, bird_id, created_at
FROM stories_old
WHERE bird_id IS NOT NULL;
```

## ?? Mobile Team Integration

A comprehensive guide has been created for the mobile team:
- **File:** `MOBILE_STORY_API_GUIDE.md`
- Contains all API endpoints with examples
- Lists all 8 story modes with emojis
- Provides validation rules
- Includes React Native code examples
- Shows media upload flow
- Migration checklist included

## ?? Key Changes Summary

### What Was Removed
- ? Story `Title` field
- ? Single `BirdId` field
- ? Single `Bird` in responses

### What Was Added
- ? Multiple birds per story (minimum 1 required)
- ? Story `Mode` (8 categories with emojis)
- ? Optional `VideoUrl` field
- ? StoryBird junction table

### What Stayed The Same
- ? Content field (still required, max 5000 chars)
- ? Optional image support
- ? Author/user relationship
- ? Created timestamp
- ? Premium highlight features

## ?? Next Steps

1. **Database Migration:**
   - Run database migration script (see Migration Requirements above)
   - Verify all tables created correctly

2. **Testing:**
   - Test creating story with single bird
   - Test creating story with multiple birds
   - Test all 8 story modes
   - Test with/without image
   - Test with/without video
   - Test with both image and video
   - Test update operations
   - Test delete operations

3. **Mobile Team:**
   - Share `MOBILE_STORY_API_GUIDE.md`
   - Coordinate API testing
   - Update mobile UI for bird selection (multi-select)
   - Update mobile UI for mode selection
   - Add video support to mobile app

## ?? API Documentation

The API documentation is available at:
- Development: `https://localhost:7297/scalar/v1`
- OpenAPI Spec: `https://localhost:7297/openapi/v1.json`

## ? Build Status

? **Build Successful** - All compilation errors resolved

---

**Date:** January 2024  
**Version:** 2.0  
**Status:** Complete ?
