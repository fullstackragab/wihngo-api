-- ============================================
-- Migration: Add video_url to birds table
-- Description: Make image_url and video_url mandatory for bird profiles
-- Date: 2024
-- ============================================

BEGIN;

-- Add video_url column to birds table
ALTER TABLE birds 
ADD COLUMN IF NOT EXISTS video_url VARCHAR(1000);

-- Make image_url NOT NULL (if it isn't already)
-- First, update any NULL values to empty string
UPDATE birds 
SET image_url = '' 
WHERE image_url IS NULL;

-- Now alter the column to NOT NULL
ALTER TABLE birds 
ALTER COLUMN image_url SET NOT NULL;

-- Update any NULL video_url values to empty string
UPDATE birds 
SET video_url = '' 
WHERE video_url IS NULL;

-- Make video_url NOT NULL
ALTER TABLE birds 
ALTER COLUMN video_url SET NOT NULL;

-- Add index for faster video URL lookups
CREATE INDEX IF NOT EXISTS idx_birds_video_url ON birds(video_url);

-- Verification
DO $$
DECLARE
    v_total_birds INTEGER;
    v_birds_with_image INTEGER;
    v_birds_with_video INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_total_birds FROM birds;
    SELECT COUNT(*) INTO v_birds_with_image FROM birds WHERE image_url IS NOT NULL AND image_url != '';
    SELECT COUNT(*) INTO v_birds_with_video FROM birds WHERE video_url IS NOT NULL AND video_url != '';
    
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'MIGRATION COMPLETED SUCCESSFULLY!';
    RAISE NOTICE '==============================================';
    RAISE NOTICE '';
    RAISE NOTICE 'Total birds: %', v_total_birds;
    RAISE NOTICE 'Birds with image_url: %', v_birds_with_image;
    RAISE NOTICE 'Birds with video_url: %', v_birds_with_video;
    RAISE NOTICE '';
    RAISE NOTICE 'IMPORTANT: All existing birds have empty video_url!';
    RAISE NOTICE 'Owners must upload videos for their birds.';
    RAISE NOTICE '==============================================';
END $$;

COMMIT;
