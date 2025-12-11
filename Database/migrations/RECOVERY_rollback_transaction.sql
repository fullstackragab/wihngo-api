-- ============================================
-- RECOVERY: Fix aborted transaction
-- Description: Rollback failed transaction and reset
-- Date: 2024
-- ============================================

-- If you see "current transaction is aborted" error,
-- run this first to recover:

ROLLBACK;

-- Then you can run the COMBINED_add_and_update_videos.sql script
-- OR run these commands manually:

-- Check if video_url column exists
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'birds' 
AND column_name IN ('image_url', 'video_url');

-- If video_url doesn't exist, add it:
-- ALTER TABLE birds ADD COLUMN video_url VARCHAR(1000) DEFAULT '';
-- ALTER TABLE birds ALTER COLUMN video_url SET NOT NULL;
-- CREATE INDEX idx_birds_video_url ON birds(video_url);
