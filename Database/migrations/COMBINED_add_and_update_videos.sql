-- ============================================
-- COMBINED: Add video_url column AND update existing birds
-- Description: Complete migration + data update in correct order
-- Date: 2024
-- ============================================

-- STEP 1: Add video_url column (if it doesn't exist)
DO $$
BEGIN
    -- Check if column exists
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'birds' 
        AND column_name = 'video_url'
    ) THEN
        RAISE NOTICE 'Adding video_url column to birds table...';
        
        -- Add video_url column
        ALTER TABLE birds ADD COLUMN video_url VARCHAR(1000);
        
        -- Set empty string for NULL values
        UPDATE birds SET video_url = '' WHERE video_url IS NULL;
        
        -- Make it NOT NULL
        ALTER TABLE birds ALTER COLUMN video_url SET NOT NULL;
        
        -- Add index
        CREATE INDEX idx_birds_video_url ON birds(video_url);
        
        RAISE NOTICE '? video_url column added successfully';
    ELSE
        RAISE NOTICE '? video_url column already exists';
    END IF;
END $$;

-- STEP 2: Make image_url NOT NULL (if it isn't already)
DO $$
BEGIN
    -- Update NULL values first
    UPDATE birds SET image_url = '' WHERE image_url IS NULL;
    
    -- Check if already NOT NULL
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'birds' 
        AND column_name = 'image_url'
        AND is_nullable = 'YES'
    ) THEN
        ALTER TABLE birds ALTER COLUMN image_url SET NOT NULL;
        RAISE NOTICE '? image_url is now NOT NULL';
    ELSE
        RAISE NOTICE '? image_url already NOT NULL';
    END IF;
END $$;

-- STEP 3: Update specific birds with video URLs
DO $$
DECLARE
    v_updated_count INTEGER := 0;
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE 'Updating bird video URLs...';
    
    -- Update Sunny (Canary)
    UPDATE birds 
    SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/5/53/Canary_singing.webm'
    WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    IF FOUND THEN v_updated_count := v_updated_count + 1; END IF;

    -- Update Anna's Hummingbird
    UPDATE birds 
    SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/3/3c/Calypte_anna_male.webm'
    WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001';
    IF FOUND THEN v_updated_count := v_updated_count + 1; END IF;

    -- Update American Robin
    UPDATE birds 
    SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/6/61/Turdus_migratorius_video.webm'
    WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002';
    IF FOUND THEN v_updated_count := v_updated_count + 1; END IF;

    -- Update Black-capped Chickadee
    UPDATE birds 
    SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/4/4f/Poecile_atricapillus.webm'
    WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003';
    IF FOUND THEN v_updated_count := v_updated_count + 1; END IF;

    -- Update Anna's Juvenile
    UPDATE birds 
    SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/8/89/Calypte_anna_feeding.webm'
    WHERE bird_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001';
    IF FOUND THEN v_updated_count := v_updated_count + 1; END IF;

    RAISE NOTICE '? Updated % bird(s) with Wikimedia Commons video URLs', v_updated_count;
END $$;

-- STEP 4: Final Verification
DO $$
DECLARE
    v_total_birds INTEGER;
    v_birds_with_image INTEGER;
    v_birds_with_video INTEGER;
    v_birds_needing_video INTEGER;
    rec RECORD;
BEGIN
    SELECT COUNT(*) INTO v_total_birds FROM birds;
    SELECT COUNT(*) INTO v_birds_with_image FROM birds WHERE image_url IS NOT NULL AND image_url != '';
    SELECT COUNT(*) INTO v_birds_with_video FROM birds WHERE video_url IS NOT NULL AND video_url != '';
    SELECT COUNT(*) INTO v_birds_needing_video FROM birds WHERE video_url = '';
    
    RAISE NOTICE '';
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'MIGRATION COMPLETED SUCCESSFULLY!';
    RAISE NOTICE '==============================================';
    RAISE NOTICE '';
    RAISE NOTICE 'Total birds: %', v_total_birds;
    RAISE NOTICE 'Birds with image_url: %', v_birds_with_image;
    RAISE NOTICE 'Birds with video_url: %', v_birds_with_video;
    RAISE NOTICE 'Birds needing video: %', v_birds_needing_video;
    RAISE NOTICE '';
    
    IF v_birds_with_video > 0 THEN
        RAISE NOTICE 'Birds with video URLs:';
        FOR rec IN 
            SELECT name, 
                   LEFT(video_url, 60) || '...' as video_preview
            FROM birds 
            WHERE video_url != ''
            ORDER BY name
            LIMIT 10
        LOOP
            RAISE NOTICE '  ? %: %', rec.name, rec.video_preview;
        END LOOP;
        RAISE NOTICE '';
    END IF;
    
    IF v_birds_needing_video > 0 THEN
        RAISE NOTICE 'WARNING: % bird(s) still need video URLs!', v_birds_needing_video;
        RAISE NOTICE 'Owners must upload videos for their birds.';
    END IF;
    
    RAISE NOTICE '';
    RAISE NOTICE '==============================================';
END $$;
