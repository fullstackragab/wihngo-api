-- Seed Birds and Stories for Current User
-- This script adds dummy bird and story data for testing purposes
-- Replace @current_user_id with your actual user ID or use the test user created below

BEGIN;

-- Option 1: Create a test user if needed (uncomment if you need a test user)
-- INSERT INTO users (user_id, name, email, password_hash, profile_image, bio, created_at)
-- VALUES
-- ('11111111-1111-1111-1111-111111111111', 'Test User', 'test@wihngo.local', '$2a$11$abcdefghijklmnopqrstuvwxyz1234567890', NULL, 'Bird enthusiast and nature lover', now())
-- ON CONFLICT (user_id) DO NOTHING;

-- Set the user ID for whom we're adding birds (change this to your actual user ID)
-- For testing, using a known user ID
DO $$
DECLARE
    current_user_id uuid := '11111111-1111-1111-1111-111111111111'; -- Change this to your user ID
BEGIN

-- Birds data
INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, video_url, loved_count, supported_count, created_at, is_premium, donation_cents, max_media_count)
VALUES
-- Bird 1: Cardinal
('bbbbbbbb-1111-1111-1111-111111111111', current_user_id, 'Ruby', 'Northern Cardinal', 'A brilliant red beacon in my backyard', 
'Ruby is a stunning male cardinal who visits my feeder every morning. His vibrant red plumage brightens even the cloudiest days. He''s quite territorial but shares the feeder with his mate, a lovely brown female I call Pearl.',
'https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/Cardinal.jpg/640px-Cardinal.jpg',
'https://commons.wikimedia.org/wiki/File:Northern_Cardinal_Call.ogg',
342, 67, now() - INTERVAL '3 months', false, 12500, 5),

-- Bird 2: Blue Jay
('bbbbbbbb-2222-2222-2222-222222222222', current_user_id, 'Jazz', 'Blue Jay', 'Intelligent and vocal backyard entertainer',
'Jazz is a curious and bold blue jay who rules the feeder. He''s learned to mimic hawk calls to scare other birds away! Despite his assertive personality, he''s fascinating to watch and incredibly intelligent.',
'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f4/Blue_jay_in_PP_%2830960%29.jpg/640px-Blue_jay_in_PP_%2830960%29.jpg',
'https://commons.wikimedia.org/wiki/File:Blue_Jay_Call.ogg',
287, 43, now() - INTERVAL '2 months', false, 8900, 5),

-- Bird 3: House Finch
('bbbbbbbb-3333-3333-3333-333333333333', current_user_id, 'Rosie', 'House Finch', 'Sweet songbird with a cheerful melody',
'Rosie is a female house finch with a delicate brown streaked pattern. She brings her mate to the feeder daily, and they sing the most beautiful duets. Their cheerful chirping is the soundtrack to my mornings.',
'https://upload.wikimedia.org/wikipedia/commons/thumb/9/9a/House_Finch_%28female%29.jpg/640px-House_Finch_%28female%29.jpg',
'https://commons.wikimedia.org/wiki/File:House_Finch_song.ogg',
156, 28, now() - INTERVAL '1 month', false, 5200, 5),

-- Bird 4: Mourning Dove
('bbbbbbbb-4444-4444-4444-444444444444', current_user_id, 'Coo', 'Mourning Dove', 'Gentle ground feeder with a peaceful presence',
'Coo is a regular visitor who prefers to forage on the ground beneath the feeders. His soft cooing is incredibly soothing, and he''s surprisingly fast when he needs to be. A true symbol of peace in my garden.',
'https://upload.wikimedia.org/wikipedia/commons/thumb/b/b7/Mourning_Dove_2006.jpg/640px-Mourning_Dove_2006.jpg',
'https://commons.wikimedia.org/wiki/File:Mourning_Dove_call.ogg',
198, 31, now() - INTERVAL '6 weeks', false, 6700, 5),

-- Bird 5: American Goldfinch
('bbbbbbbb-5555-5555-5555-555555555555', current_user_id, 'Sunny', 'American Goldfinch', 'A splash of sunshine on the feeder',
'Sunny lives up to his name with brilliant yellow plumage that looks like captured sunlight. He''s acrobatic at the thistle feeder and often hangs upside down while eating. His bouncy flight pattern makes him easy to spot.',
'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/American_Goldfinch-27527.jpg/640px-American_Goldfinch-27527.jpg',
'https://commons.wikimedia.org/wiki/File:American_Goldfinch.ogg',
423, 89, now() - INTERVAL '4 months', true, 18900, 10),

-- Bird 6: Downy Woodpecker
('bbbbbbbb-6666-6666-6666-666666666666', current_user_id, 'Drumstick', 'Downy Woodpecker', 'Tiny drummer creating backyard rhythms',
'Drumstick is a small but mighty woodpecker who loves the suet feeder. The distinctive red patch on the back of his head identifies him as male. His drumming on the dead oak tree announces spring every year.',
'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f6/Downy_Woodpecker_male.jpg/640px-Downy_Woodpecker_male.jpg',
'https://commons.wikimedia.org/wiki/File:Downy_Woodpecker_drumming.ogg',
234, 52, now() - INTERVAL '5 weeks', false, 9800, 5),

-- Bird 7: Eastern Bluebird
('bbbbbbbb-7777-7777-7777-777777777777', current_user_id, 'Sky', 'Eastern Bluebird', 'Living jewel bringing joy and hope',
'Sky discovered the mealworm feeder last spring and has been a loyal visitor ever since. His cobalt blue back and rusty breast are breathtaking. Watching him successfully fledge babies from the nest box was magical.',
'https://upload.wikimedia.org/wikipedia/commons/thumb/9/94/Sialia_sialis_-Chincoteague_National_Wildlife_Refuge%2C_Virginia%2C_USA-8.jpg/640px-Sialia_sialis_-Chincoteague_National_Wildlife_Refuge%2C_Virginia%2C_USA-8.jpg',
'https://commons.wikimedia.org/wiki/File:Eastern_Bluebird_song.ogg',
512, 124, now() - INTERVAL '7 months', true, 24300, 10)

ON CONFLICT (bird_id) DO NOTHING;

-- Stories for the birds
INSERT INTO stories (story_id, bird_id, author_id, content, image_url, created_at, is_highlighted, highlight_order)
VALUES
-- Stories for Ruby (Cardinal)
('ssssssss-1111-1111-1111-111111111111', 'bbbbbbbb-1111-1111-1111-111111111111', current_user_id,
'First Meeting: I spotted Ruby on a snowy morning, his red feathers a striking contrast against the white landscape. He was tentatively approaching the new feeder I had just installed. Within minutes, he became a regular visitor!',
NULL, now() - INTERVAL '3 months', false, NULL),

('ssssssss-1112-1112-1112-111111111112', 'bbbbbbbb-1111-1111-1111-111111111111', current_user_id,
'Ruby and Pearl: Today I witnessed the most heartwarming moment - Ruby was feeding Pearl seeds directly, beak to beak. This courtship feeding behavior shows they''re bonding for the breeding season. Nature''s romance at its finest!',
NULL, now() - INTERVAL '2 months', true, 1),

('ssssssss-1113-1113-1113-111111111113', 'bbbbbbbb-1111-1111-1111-111111111111', current_user_id,
'Morning Serenade: Ruby has started singing from the top of the oak tree at dawn. His clear whistling "cheer-cheer-cheer" is my favorite alarm clock. He performs for about 20 minutes before flying down to breakfast.',
NULL, now() - INTERVAL '3 weeks', false, NULL),

-- Stories for Jazz (Blue Jay)
('ssssssss-2221-2221-2221-222222222221', 'bbbbbbbb-2222-2222-2222-222222222222', current_user_id,
'The Trickster: Caught Jazz mimicking a red-tailed hawk cry perfectly! All the smaller birds scattered, and he had the entire feeder to himself. He looked quite pleased with his clever deception.',
NULL, now() - INTERVAL '2 months', true, 2),

('ssssssss-2222-2222-2222-222222222222', 'bbbbbbbb-2222-2222-2222-222222222222', current_user_id,
'Peanut Stash: Jazz has been burying peanuts all over the yard. I counted him hiding 15 in one morning! His memory must be incredible because he retrieves them days later from their exact locations.',
NULL, now() - INTERVAL '5 weeks', false, NULL),

('ssssssss-2223-2223-2223-222222222223', 'bbbbbbbb-2222-2222-2222-222222222222', current_user_id,
'Family Lesson: Jazz brought his offspring to the feeder today - three young jays learning the ropes. He demonstrated how to crack open sunflower seeds while they watched intently. Proud papa moment!',
NULL, now() - INTERVAL '2 weeks', false, NULL),

-- Stories for Rosie (House Finch)
('ssssssss-3331-3331-3331-333333333331', 'bbbbbbbb-3333-3333-3333-333333333333', current_user_id,
'Love Songs: Rosie''s mate serenades her every morning with the most complex, warbling songs. She responds with her own calls, and they perform these musical conversations for hours. Pure joy!',
NULL, now() - INTERVAL '1 month', false, NULL),

('ssssssss-3332-3332-3332-333333333332', 'bbbbbbbb-3333-3333-3333-333333333333', current_user_id,
'Nest Building: Spotted Rosie collecting nesting material from the yard - small twigs, grass, and even some dryer lint I left out! She made dozens of trips to a cozy spot under the eaves.',
NULL, now() - INTERVAL '2 weeks', false, NULL),

-- Stories for Coo (Mourning Dove)
('ssssssss-4441-4441-4441-444444444441', 'bbbbbbbb-4444-4444-4444-444444444444', current_user_id,
'Peaceful Presence: Coo sat on the fence post this afternoon, preening in the warm sun. His gentle cooing created such a peaceful atmosphere. These moments remind me why I started bird feeding.',
NULL, now() - INTERVAL '5 weeks', false, NULL),

('ssssssss-4442-4442-4442-444444444442', 'bbbbbbbb-4444-4444-4444-444444444444', current_user_id,
'Speed Demon: A hawk flew over today and Coo took off like a rocket! I never knew mourning doves could fly that fast. He was back within minutes, cautiously checking if the coast was clear.',
NULL, now() - INTERVAL '3 weeks', false, NULL),

-- Stories for Sunny (Goldfinch)
('ssssssss-5551-5551-5551-555555555551', 'bbbbbbbb-5555-5555-5555-555555555555', current_user_id,
'Acrobatic Master: Sunny was hanging completely upside down from the thistle feeder today, showing off for the other finches. His bouncy, undulating flight pattern when he left was like watching a tiny roller coaster!',
NULL, now() - INTERVAL '3 months', true, 3),

('ssssssss-5552-5552-5552-555555555552', 'bbbbbbbb-5555-5555-5555-555555555555', current_user_id,
'Color Change: I''ve been watching Sunny molt from his duller winter plumage into brilliant breeding colors. He''s getting more yellow every day! The transformation is remarkable.',
NULL, now() - INTERVAL '6 weeks', false, NULL),

('ssssssss-5553-5553-5553-555555555553', 'bbbbbbbb-5555-5555-5555-555555555555', current_user_id,
'Social Hour: Ten goldfinches at the feeder today! Sunny was right in the middle of the social gathering. They all chattered and fed together peacefully. Such a cheerful, social species!',
NULL, now() - INTERVAL '1 week', false, NULL),

-- Stories for Drumstick (Woodpecker)
('ssssssss-6661-6661-6661-666666666661', 'bbbbbbbb-6666-6666-6666-666666666666', current_user_id,
'Spring Drumming: Drumstick has been drumming on the metal drainpipe every morning at 6 AM sharp. It''s LOUD but I can''t be mad - he''s announcing his territory and attracting a mate. Nature''s alarm clock!',
NULL, now() - INTERVAL '4 weeks', false, NULL),

('ssssssss-6662-6662-6662-666666666662', 'bbbbbbbb-6666-6666-6666-666666666666', current_user_id,
'Suet Lover: Drumstick discovered the new peanut butter suet I put out. He spent 20 minutes hanging on it, occasionally stopping to look around for competitors. The red patch on his head is so vibrant!',
NULL, now() - INTERVAL '2 weeks', false, NULL),

-- Stories for Sky (Bluebird)
('ssssssss-7771-7771-7771-777777777771', 'bbbbbbbb-7777-7777-7777-777777777777', current_user_id,
'Nest Box Success: Sky and his mate chose my nest box! I watched them carry in soft grass and pine needles for days. Soon I heard the peeping of babies inside. So rewarding!',
NULL, now() - INTERVAL '6 months', true, 4),

('ssssssss-7772-7772-7772-777777777772', 'bbbbbbbb-7777-7777-7777-777777777777', current_user_id,
'Feeding Frenzy: Sky made over 100 feeding trips today bringing mealworms to his nestlings! Both parents were working non-stop. I restocked the mealworm feeder three times. Exhausting but amazing to witness.',
NULL, now() - INTERVAL '5 months', false, NULL),

('ssssssss-7773-7773-7773-777777777773', 'bbbbbbbb-7777-7777-7777-777777777777', current_user_id,
'Fledgling Day: The babies fledged today! Sky was calling encouragement from nearby branches as three fluffy young bluebirds took their first wobbly flights. Two made it to the fence, one landed in the grass. All safe!',
NULL, now() - INTERVAL '4 months', true, 5),

('ssssssss-7774-7774-7774-777777777774', 'bbbbbbbb-7777-7777-7777-777777777777', current_user_id,
'Winter Visit: Sky showed up today at the mealworm feeder despite the cold! Most bluebirds migrate south, but he''s staying for the winter. I''ll make sure he''s well fed through the cold months.',
NULL, now() - INTERVAL '1 week', false, NULL)

ON CONFLICT (story_id) DO NOTHING;

-- Add some support transactions (optional)
INSERT INTO support_transactions (transaction_id, supporter_id, bird_id, amount, message, created_at)
VALUES
('tttttttt-1111-1111-1111-111111111111', current_user_id, 'bbbbbbbb-5555-5555-5555-555555555555', 25.00, 'For thistle seed - Sunny deserves the best!', now() - INTERVAL '2 months'),
('tttttttt-2222-2222-2222-222222222222', current_user_id, 'bbbbbbbb-7777-7777-7777-777777777777', 50.00, 'Helping fund mealworms for the nestlings!', now() - INTERVAL '5 months'),
('tttttttt-3333-3333-3333-333333333333', current_user_id, 'bbbbbbbb-1111-1111-1111-111111111111', 15.00, 'Supporting my favorite cardinal', now() - INTERVAL '1 month')
ON CONFLICT (transaction_id) DO NOTHING;

-- Add some loves
INSERT INTO loves (user_id, bird_id, created_at)
VALUES
(current_user_id, 'bbbbbbbb-1111-1111-1111-111111111111', now() - INTERVAL '3 months'),
(current_user_id, 'bbbbbbbb-2222-2222-2222-222222222222', now() - INTERVAL '2 months'),
(current_user_id, 'bbbbbbbb-5555-5555-5555-555555555555', now() - INTERVAL '4 months'),
(current_user_id, 'bbbbbbbb-7777-7777-7777-777777777777', now() - INTERVAL '7 months')
ON CONFLICT (user_id, bird_id) DO NOTHING;

END;
$$ LANGUAGE plpgsql;

COMMIT;
