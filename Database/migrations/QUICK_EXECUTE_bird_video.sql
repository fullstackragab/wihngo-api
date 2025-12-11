-- ============================================
-- QUICK EXECUTE: Add Bird Video URL (Copy & Paste)
-- ============================================
-- Execute this directly in psql or pgAdmin Query Tool
-- ============================================

BEGIN;

-- Step 1: Add video_url column
ALTER TABLE birds ADD COLUMN IF NOT EXISTS video_url VARCHAR(1000);

-- Step 2: Set empty string for NULL image_url
UPDATE birds SET image_url = '' WHERE image_url IS NULL;

-- Step 3: Make image_url required
ALTER TABLE birds ALTER COLUMN image_url SET NOT NULL;

-- Step 4: Set empty string for NULL video_url  
UPDATE birds SET video_url = '' WHERE video_url IS NULL;

-- Step 5: Make video_url required
ALTER TABLE birds ALTER COLUMN video_url SET NOT NULL;

-- Step 6: Add performance index
CREATE INDEX IF NOT EXISTS idx_birds_video_url ON birds(video_url);

COMMIT;

-- Verify
SELECT 'Migration Complete!' as status,
       COUNT(*) as total_birds,
       COUNT(CASE WHEN image_url != '' THEN 1 END) as birds_with_images,
       COUNT(CASE WHEN video_url != '' THEN 1 END) as birds_with_videos
FROM birds;
