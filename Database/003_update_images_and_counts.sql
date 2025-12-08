-- 003_update_images_and_counts.sql
-- Update users' profile images and birds' image URLs and counts to match frontend sample (Unsplash images)
-- Run: psql -h <host> -U <user> -d wihngo -f Database/003_update_images_and_counts.sql

BEGIN;

-- Update users' profile images
UPDATE users
SET profile_image = 'https://images.unsplash.com/photo-1494790108377-be9c29b29330'
WHERE user_id = '33333333-3333-3333-3333-333333333333';

UPDATE users
SET profile_image = 'https://images.unsplash.com/photo-1508214751196-bcfd4ca60f91'
WHERE user_id = '44444444-4444-4444-4444-444444444444';

UPDATE users
SET profile_image = 'https://images.unsplash.com/photo-1544005313-94ddf0286df2'
WHERE user_id = '11111111-1111-1111-1111-111111111111';

-- Update birds' image URLs and counts
UPDATE birds
SET image_url = 'https://images.unsplash.com/photo-1604079628040-94301bb21b91',
    loved_count = 980,
    supported_count = 75
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003';

UPDATE birds
SET image_url = 'https://images.unsplash.com/photo-1552728089-57bdde30beb3',
    loved_count = 45,
    supported_count = 3
WHERE bird_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001';

UPDATE birds
SET image_url = 'https://images.unsplash.com/photo-1621091211034-53136cc1eb32',
    loved_count = 12,
    supported_count = 1
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

UPDATE birds
SET image_url = 'https://images.unsplash.com/photo-1520808663317-647b476a81b9',
    loved_count = 2847,
    supported_count = 2
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001';

UPDATE birds
SET image_url = 'https://images.unsplash.com/photo-1610465299993-8a8ec20b6c95',
    loved_count = 1200,
    supported_count = 1
WHERE bird_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002';

COMMIT;
