-- =====================================================
-- ADD AUDIO TO STORIES MIGRATION
-- =====================================================
-- This migration adds audio recording support to stories
-- Date: 2025-12-16
--
-- Changes:
-- 1. Add audio_url column to stories table (VARCHAR 1000, nullable)
-- 2. Add index for audio_url lookups (partial index on non-null values)
--
-- Audio recordings are stored in S3 with path pattern:
-- users/stories/{userId}/{storyId}/{uuid}.m4a
-- =====================================================

BEGIN;

-- Step 1: Add audio_url column to stories table
ALTER TABLE stories
ADD COLUMN IF NOT EXISTS audio_url VARCHAR(1000);

-- Step 2: Add index for audio_url lookups (partial index for performance)
CREATE INDEX IF NOT EXISTS idx_stories_audio_url
ON stories(audio_url)
WHERE audio_url IS NOT NULL;

-- Step 3: Add column comment
COMMENT ON COLUMN stories.audio_url IS 'S3 key for story audio recording (e.g., users/stories/{userId}/{storyId}/{uuid}.m4a). Audio can be combined with image_url and/or video_url.';

COMMIT;

-- Verification query
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_name = 'stories' AND column_name = 'audio_url';
