-- Seed Birds and Stories for Any Existing User
-- This script finds the most recently created user and adds birds/stories to them
-- Or you can manually specify a user_id

BEGIN;

-- This will add birds to the most recent user in the system
-- If you want to specify a different user, replace the subquery with a specific UUID

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
SELECT 
    gen_random_uuid(),
    (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1),
    'Ruby',
    'Northern Cardinal',
    'A brilliant red beacon in my backyard',
    'Ruby is a stunning male cardinal who visits my feeder every morning. His vibrant red plumage brightens even the cloudiest days.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/Cardinal.jpg/640px-Cardinal.jpg',
    'https://commons.wikimedia.org/wiki/File:Northern_Cardinal_Call.ogg',
    342, 67, now() - INTERVAL '3 months', false, 12500, 5
WHERE EXISTS (SELECT 1 FROM users);

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
SELECT 
    gen_random_uuid(),
    (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1),
    'Jazz',
    'Blue Jay',
    'Intelligent and vocal backyard entertainer',
    'Jazz is a curious and bold blue jay who rules the feeder. He has learned to mimic hawk calls to scare other birds away!',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f4/Blue_jay_in_PP_%2830960%29.jpg/640px-Blue_jay_in_PP_%2830960%29.jpg',
    'https://commons.wikimedia.org/wiki/File:Blue_Jay_Call.ogg',
    287, 43, now() - INTERVAL '2 months', false, 8900, 5
WHERE EXISTS (SELECT 1 FROM users);

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
SELECT 
    gen_random_uuid(),
    (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1),
    'Rosie',
    'House Finch',
    'Sweet songbird with a cheerful melody',
    'Rosie is a female house finch with a delicate brown streaked pattern. She brings her mate to the feeder daily.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/9/9a/House_Finch_%28female%29.jpg/640px-House_Finch_%28female%29.jpg',
    'https://commons.wikimedia.org/wiki/File:House_Finch_song.ogg',
    156, 28, now() - INTERVAL '1 month', false, 5200, 5
WHERE EXISTS (SELECT 1 FROM users);

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
SELECT 
    gen_random_uuid(),
    (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1),
    'Coo',
    'Mourning Dove',
    'Gentle ground feeder with a peaceful presence',
    'Coo is a regular visitor who prefers to forage on the ground beneath the feeders. His soft cooing is incredibly soothing.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/b/b7/Mourning_Dove_2006.jpg/640px-Mourning_Dove_2006.jpg',
    'https://commons.wikimedia.org/wiki/File:Mourning_Dove_call.ogg',
    198, 31, now() - INTERVAL '6 weeks', false, 6700, 5
WHERE EXISTS (SELECT 1 FROM users);

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
SELECT 
    gen_random_uuid(),
    (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1),
    'Sunny',
    'American Goldfinch',
    'A splash of sunshine on the feeder',
    'Sunny lives up to his name with brilliant yellow plumage that looks like captured sunlight. He is acrobatic at the thistle feeder.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/American_Goldfinch-27527.jpg/640px-American_Goldfinch-27527.jpg',
    'https://commons.wikimedia.org/wiki/File:American_Goldfinch.ogg',
    423, 89, now() - INTERVAL '4 months', true, 18900, 10
WHERE EXISTS (SELECT 1 FROM users);

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
SELECT 
    gen_random_uuid(),
    (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1),
    'Drumstick',
    'Downy Woodpecker',
    'Tiny drummer creating backyard rhythms',
    'Drumstick is a small but mighty woodpecker who loves the suet feeder. His drumming on the dead oak tree announces spring.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f6/Downy_Woodpecker_male.jpg/640px-Downy_Woodpecker_male.jpg',
    'https://commons.wikimedia.org/wiki/File:Downy_Woodpecker_drumming.ogg',
    234, 52, now() - INTERVAL '5 weeks', false, 9800, 5
WHERE EXISTS (SELECT 1 FROM users);

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
SELECT 
    gen_random_uuid(),
    (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1),
    'Sky',
    'Eastern Bluebird',
    'Living jewel bringing joy and hope',
    'Sky discovered the mealworm feeder last spring and has been a loyal visitor ever since. His cobalt blue back is breathtaking.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/9/94/Sialia_sialis_-Chincoteague_National_Wildlife_Refuge%2C_Virginia%2C_USA-8.jpg/640px-Sialia_sialis_-Chincoteague_National_Wildlife_Refuge%2C_Virginia%2C_USA-8.jpg',
    'https://commons.wikimedia.org/wiki/File:Eastern_Bluebird_song.ogg',
    512, 124, now() - INTERVAL '7 months', true, 24300, 10
WHERE EXISTS (SELECT 1 FROM users);

-- Now add stories for each bird
-- We'll use a temporary function to make this cleaner
DO $$
DECLARE
    v_user_id uuid;
    v_bird_ruby uuid;
    v_bird_jazz uuid;
    v_bird_rosie uuid;
    v_bird_coo uuid;
    v_bird_sunny uuid;
    v_bird_drumstick uuid;
    v_bird_sky uuid;
BEGIN
    -- Get the user ID
    SELECT user_id INTO v_user_id FROM users ORDER BY created_at DESC LIMIT 1;
    
    IF v_user_id IS NULL THEN
        RAISE NOTICE 'No users found in the database. Please create a user first.';
        RETURN;
    END IF;

    -- Get the bird IDs we just created (by name and owner)
    SELECT bird_id INTO v_bird_ruby FROM birds WHERE owner_id = v_user_id AND name = 'Ruby' ORDER BY created_at DESC LIMIT 1;
    SELECT bird_id INTO v_bird_jazz FROM birds WHERE owner_id = v_user_id AND name = 'Jazz' ORDER BY created_at DESC LIMIT 1;
    SELECT bird_id INTO v_bird_rosie FROM birds WHERE owner_id = v_user_id AND name = 'Rosie' ORDER BY created_at DESC LIMIT 1;
    SELECT bird_id INTO v_bird_coo FROM birds WHERE owner_id = v_user_id AND name = 'Coo' ORDER BY created_at DESC LIMIT 1;
    SELECT bird_id INTO v_bird_sunny FROM birds WHERE owner_id = v_user_id AND name = 'Sunny' ORDER BY created_at DESC LIMIT 1;
    SELECT bird_id INTO v_bird_drumstick FROM birds WHERE owner_id = v_user_id AND name = 'Drumstick' ORDER BY created_at DESC LIMIT 1;
    SELECT bird_id INTO v_bird_sky FROM birds WHERE owner_id = v_user_id AND name = 'Sky' ORDER BY created_at DESC LIMIT 1;

    -- Stories for Ruby (Cardinal)
    IF v_bird_ruby IS NOT NULL THEN
        INSERT INTO stories (story_id, bird_id, author_id, content, created_at, is_highlighted, highlight_order)
        VALUES
        (gen_random_uuid(), v_bird_ruby, v_user_id, 
         'First Meeting: I spotted Ruby on a snowy morning, his red feathers a striking contrast against the white landscape. He was tentatively approaching the new feeder I had just installed. Within minutes, he became a regular visitor!',
         now() - INTERVAL '3 months', false, NULL),
        (gen_random_uuid(), v_bird_ruby, v_user_id,
         'Ruby and Pearl: Today I witnessed the most heartwarming moment - Ruby was feeding Pearl seeds directly, beak to beak. This courtship feeding behavior shows they are bonding for the breeding season. Nature''s romance at its finest!',
         now() - INTERVAL '2 months', true, 1),
        (gen_random_uuid(), v_bird_ruby, v_user_id,
         'Morning Serenade: Ruby has started singing from the top of the oak tree at dawn. His clear whistling "cheer-cheer-cheer" is my favorite alarm clock. He performs for about 20 minutes before flying down to breakfast.',
         now() - INTERVAL '3 weeks', false, NULL);
    END IF;

    -- Stories for Jazz (Blue Jay)
    IF v_bird_jazz IS NOT NULL THEN
        INSERT INTO stories (story_id, bird_id, author_id, content, created_at, is_highlighted, highlight_order)
        VALUES
        (gen_random_uuid(), v_bird_jazz, v_user_id,
         'The Trickster: Caught Jazz mimicking a red-tailed hawk cry perfectly! All the smaller birds scattered, and he had the entire feeder to himself. He looked quite pleased with his clever deception.',
         now() - INTERVAL '2 months', true, 2),
        (gen_random_uuid(), v_bird_jazz, v_user_id,
         'Peanut Stash: Jazz has been burying peanuts all over the yard. I counted him hiding 15 in one morning! His memory must be incredible because he retrieves them days later from their exact locations.',
         now() - INTERVAL '5 weeks', false, NULL),
        (gen_random_uuid(), v_bird_jazz, v_user_id,
         'Family Lesson: Jazz brought his offspring to the feeder today - three young jays learning the ropes. He demonstrated how to crack open sunflower seeds while they watched intently. Proud papa moment!',
         now() - INTERVAL '2 weeks', false, NULL);
    END IF;

    -- Stories for Rosie (House Finch)
    IF v_bird_rosie IS NOT NULL THEN
        INSERT INTO stories (story_id, bird_id, author_id, content, created_at, is_highlighted, highlight_order)
        VALUES
        (gen_random_uuid(), v_bird_rosie, v_user_id,
         'Love Songs: Rosie''s mate serenades her every morning with the most complex, warbling songs. She responds with her own calls, and they perform these musical conversations for hours. Pure joy!',
         now() - INTERVAL '1 month', false, NULL),
        (gen_random_uuid(), v_bird_rosie, v_user_id,
         'Nest Building: Spotted Rosie collecting nesting material from the yard - small twigs, grass, and even some dryer lint I left out! She made dozens of trips to a cozy spot under the eaves.',
         now() - INTERVAL '2 weeks', false, NULL);
    END IF;

    -- Stories for Coo (Mourning Dove)
    IF v_bird_coo IS NOT NULL THEN
        INSERT INTO stories (story_id, bird_id, author_id, content, created_at, is_highlighted, highlight_order)
        VALUES
        (gen_random_uuid(), v_bird_coo, v_user_id,
         'Peaceful Presence: Coo sat on the fence post this afternoon, preening in the warm sun. His gentle cooing created such a peaceful atmosphere. These moments remind me why I started bird feeding.',
         now() - INTERVAL '5 weeks', false, NULL),
        (gen_random_uuid(), v_bird_coo, v_user_id,
         'Speed Demon: A hawk flew over today and Coo took off like a rocket! I never knew mourning doves could fly that fast. He was back within minutes, cautiously checking if the coast was clear.',
         now() - INTERVAL '3 weeks', false, NULL);
    END IF;

    -- Stories for Sunny (Goldfinch)
    IF v_bird_sunny IS NOT NULL THEN
        INSERT INTO stories (story_id, bird_id, author_id, content, created_at, is_highlighted, highlight_order)
        VALUES
        (gen_random_uuid(), v_bird_sunny, v_user_id,
         'Acrobatic Master: Sunny was hanging completely upside down from the thistle feeder today, showing off for the other finches. His bouncy, undulating flight pattern when he left was like watching a tiny roller coaster!',
         now() - INTERVAL '3 months', true, 3),
        (gen_random_uuid(), v_bird_sunny, v_user_id,
         'Color Change: I have been watching Sunny molt from his duller winter plumage into brilliant breeding colors. He is getting more yellow every day! The transformation is remarkable.',
         now() - INTERVAL '6 weeks', false, NULL),
        (gen_random_uuid(), v_bird_sunny, v_user_id,
         'Social Hour: Ten goldfinches at the feeder today! Sunny was right in the middle of the social gathering. They all chattered and fed together peacefully. Such a cheerful, social species!',
         now() - INTERVAL '1 week', false, NULL);
    END IF;

    -- Stories for Drumstick (Woodpecker)
    IF v_bird_drumstick IS NOT NULL THEN
        INSERT INTO stories (story_id, bird_id, author_id, content, created_at, is_highlighted, highlight_order)
        VALUES
        (gen_random_uuid(), v_bird_drumstick, v_user_id,
         'Spring Drumming: Drumstick has been drumming on the metal drainpipe every morning at 6 AM sharp. It is LOUD but I cannot be mad - he is announcing his territory and attracting a mate. Nature''s alarm clock!',
         now() - INTERVAL '4 weeks', false, NULL),
        (gen_random_uuid(), v_bird_drumstick, v_user_id,
         'Suet Lover: Drumstick discovered the new peanut butter suet I put out. He spent 20 minutes hanging on it, occasionally stopping to look around for competitors. The red patch on his head is so vibrant!',
         now() - INTERVAL '2 weeks', false, NULL);
    END IF;

    -- Stories for Sky (Bluebird)
    IF v_bird_sky IS NOT NULL THEN
        INSERT INTO stories (story_id, bird_id, author_id, content, created_at, is_highlighted, highlight_order)
        VALUES
        (gen_random_uuid(), v_bird_sky, v_user_id,
         'Nest Box Success: Sky and his mate chose my nest box! I watched them carry in soft grass and pine needles for days. Soon I heard the peeping of babies inside. So rewarding!',
         now() - INTERVAL '6 months', true, 4),
        (gen_random_uuid(), v_bird_sky, v_user_id,
         'Feeding Frenzy: Sky made over 100 feeding trips today bringing mealworms to his nestlings! Both parents were working non-stop. I restocked the mealworm feeder three times. Exhausting but amazing to witness.',
         now() - INTERVAL '5 months', false, NULL),
        (gen_random_uuid(), v_bird_sky, v_user_id,
         'Fledgling Day: The babies fledged today! Sky was calling encouragement from nearby branches as three fluffy young bluebirds took their first wobbly flights. Two made it to the fence, one landed in the grass. All safe!',
         now() - INTERVAL '4 months', true, 5),
        (gen_random_uuid(), v_bird_sky, v_user_id,
         'Winter Visit: Sky showed up today at the mealworm feeder despite the cold! Most bluebirds migrate south, but he is staying for the winter. I will make sure he is well fed through the cold months.',
         now() - INTERVAL '1 week', false, NULL);
    END IF;

    RAISE NOTICE 'Successfully added birds and stories for user: %', v_user_id;
END $$;

COMMIT;

-- Verification query - run this after the script to see what was added
-- SELECT u.name, u.email, b.name as bird_name, b.species, COUNT(s.story_id) as story_count
-- FROM users u
-- LEFT JOIN birds b ON b.owner_id = u.user_id
-- LEFT JOIN stories s ON s.bird_id = b.bird_id
-- WHERE u.user_id = (SELECT user_id FROM users ORDER BY created_at DESC LIMIT 1)
-- GROUP BY u.name, u.email, b.name, b.species
-- ORDER BY b.name;
