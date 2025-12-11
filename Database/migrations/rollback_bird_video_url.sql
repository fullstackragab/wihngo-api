-- ============================================
-- Rollback: Remove video_url from birds table
-- Description: Rollback the video_url migration
-- Date: 2024
-- ============================================

BEGIN;

-- Remove index
DROP INDEX IF EXISTS idx_birds_video_url;

-- Make image_url nullable again (if needed)
ALTER TABLE birds 
ALTER COLUMN image_url DROP NOT NULL;

-- Remove video_url column
ALTER TABLE birds 
DROP COLUMN IF EXISTS video_url;

-- Verification
DO $$
BEGIN
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'ROLLBACK COMPLETED SUCCESSFULLY!';
    RAISE NOTICE '==============================================';
    RAISE NOTICE '';
    RAISE NOTICE 'video_url column removed from birds table';
    RAISE NOTICE 'image_url is now nullable again';
    RAISE NOTICE '==============================================';
END $$;

COMMIT;
