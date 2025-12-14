-- =====================================================
-- REMOVE BIRD VIDEO URL MIGRATION
-- =====================================================
-- This migration removes the video_url column from the birds table
-- as bird profiles will only have a single image going forward.
--
-- WARNING: This migration will:
-- 1. Remove video_url column from birds table
-- 2. All existing bird video URLs will be lost
--
-- BACKUP YOUR DATA BEFORE RUNNING THIS MIGRATION!
-- =====================================================

BEGIN;

-- Step 1: Drop video_url column from birds table
ALTER TABLE birds 
DROP COLUMN IF EXISTS video_url;

-- Step 2: Verify migration
DO $$
DECLARE
    column_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'birds' 
        AND column_name = 'video_url'
    ) INTO column_exists;
    
    IF column_exists THEN
        RAISE EXCEPTION 'Migration failed: video_url column still exists in birds table';
    ELSE
        RAISE NOTICE 'Migration completed successfully! video_url column removed from birds table.';
    END IF;
END $$;

COMMIT;

-- =====================================================
-- ROLLBACK INSTRUCTIONS (if needed)
-- =====================================================
-- If you need to roll back this migration:
--
-- 1. Restore from backup (recommended)
-- OR
-- 2. Manually recreate video_url column:
/*
BEGIN;

ALTER TABLE birds 
ADD COLUMN video_url VARCHAR(1000) NOT NULL DEFAULT '';

COMMIT;
*/
