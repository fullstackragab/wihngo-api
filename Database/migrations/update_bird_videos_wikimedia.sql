-- ============================================
-- Update video URLs for existing birds (Wikimedia Commons)
-- Description: Update video_url with direct .webm video file URLs
-- Date: 2024
-- ============================================

BEGIN;

-- Update Sunny (Canary)
UPDATE birds 
SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/5/53/Canary_singing.webm'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

-- Update Anna's Hummingbird
UPDATE birds 
SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/3/3c/Calypte_anna_male.webm'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001';

-- Update American Robin
UPDATE birds 
SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/6/61/Turdus_migratorius_video.webm'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002';

-- Update Black-capped Chickadee
UPDATE birds 
SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/4/4f/Poecile_atricapillus.webm'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003';

-- Update Anna's Juvenile
UPDATE birds 
SET video_url = 'https://upload.wikimedia.org/wikipedia/commons/8/89/Calypte_anna_feeding.webm'
WHERE bird_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001';

-- Verification
DO $$
DECLARE
    v_updated_count INTEGER;
    rec RECORD;
BEGIN
    SELECT COUNT(*) INTO v_updated_count 
    FROM birds 
    WHERE bird_id IN (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003',
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001'
    ) AND video_url IS NOT NULL AND video_url != '';
    
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'VIDEO URL UPDATE COMPLETED!';
    RAISE NOTICE '==============================================';
    RAISE NOTICE '';
    RAISE NOTICE 'Birds updated with Wikimedia Commons video URLs: %', v_updated_count;
    RAISE NOTICE '';
    
    -- Show updated birds with full URLs
    RAISE NOTICE 'Updated birds:';
    FOR rec IN 
        SELECT name, species, video_url
        FROM birds 
        WHERE bird_id IN (
            'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001',
            'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002',
            'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003',
            'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001'
        )
        ORDER BY name
    LOOP
        RAISE NOTICE '  ? % (%)', rec.name, rec.species;
        RAISE NOTICE '    %', rec.video_url;
    END LOOP;
    
    RAISE NOTICE '';
    RAISE NOTICE 'All video URLs are direct .webm files from Wikimedia Commons';
    RAISE NOTICE '==============================================';
END $$;

COMMIT;
