-- ============================================
-- Update video URLs for existing birds
-- Description: Update video_url for specific birds with provided URLs
-- Date: 2024
-- ============================================

BEGIN;

-- Update Sunny (Canary)
UPDATE birds 
SET video_url = 'https://www.youtube.com/watch?v=QD8bTJX0gbE'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

-- Update Anna's Hummingbird
UPDATE birds 
SET video_url = 'https://www.britannica.com/video/courtship-hummingbird-Anna-display-flight-diving-male/-244877'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001';

-- Update American Robin
UPDATE birds 
SET video_url = 'https://macaulaylibrary.org/video/226292131'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002';

-- Update Black-capped Chickadee
UPDATE birds 
SET video_url = 'https://www.youtube.com/watch?v=Ba3CKjFDEDY'
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003';

-- Update Anna's Juvenile
UPDATE birds 
SET video_url = 'https://macaulaylibrary.org/video/466292'
WHERE bird_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001';

-- Verification
DO $$
DECLARE
    v_updated_count INTEGER;
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
    RAISE NOTICE 'Birds updated with video URLs: %', v_updated_count;
    RAISE NOTICE '';
    
    -- Show updated birds
    RAISE NOTICE 'Updated birds:';
    FOR rec IN 
        SELECT name, 
               LEFT(video_url, 50) || '...' as video_preview
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
        RAISE NOTICE '  - %: %', rec.name, rec.video_preview;
    END LOOP;
    
    RAISE NOTICE '';
    RAISE NOTICE '==============================================';
END $$;

COMMIT;
