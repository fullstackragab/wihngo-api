-- Seed Birds and Stories for faaf.dv@example.com
-- Run this script to populate the database with sample bird and story data

BEGIN;

DO $$
DECLARE
    target_user_id uuid;
BEGIN
    -- Get the user ID for faaf.dv@example.com
    SELECT user_id INTO target_user_id FROM users WHERE email = 'faaf.dv@example.com';

    IF target_user_id IS NULL THEN
        RAISE EXCEPTION 'User faaf.dv@example.com not found. Please register this user first.';
    END IF;

    RAISE NOTICE 'Found user faaf.dv@example.com with ID: %', target_user_id;

    -- Insert birds for this user
    INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, loved_count, supported_count, created_at, is_premium, max_media_count, donation_cents)
    VALUES
    -- Bird 1: Cockatiel
    (gen_random_uuid(), target_user_id, 'Mango', 'Cockatiel', 'The whistling sunshine of our home',
    'Mango is a cheerful lutino cockatiel who loves to whistle popular songs. He greets everyone with head bobs and chirps, and has learned to mimic the microwave beep perfectly. His favorite spot is on shoulders, where he enjoys gentle head scratches.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/d/d4/Cockatiel_crest.jpg/640px-Cockatiel_crest.jpg',
    128, 34, now() - INTERVAL '4 months', false, 5, 0),

    -- Bird 2: Budgerigar
    (gen_random_uuid(), target_user_id, 'Blueberry', 'Budgerigar', 'Tiny acrobat with a big personality',
    'Blueberry is a sky-blue budgie who loves to perform aerial tricks in his flight cage. He chatters constantly, mixing bird sounds with words he''s picked up. His favorite activity is playing with bells and mirrors.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/2/22/Budgerigar-male-head.jpg/640px-Budgerigar-male-head.jpg',
    95, 22, now() - INTERVAL '3 months', false, 5, 0),

    -- Bird 3: Lovebird
    (gen_random_uuid(), target_user_id, 'Peaches', 'Fischer''s Lovebird', 'Feisty little bundle of love',
    'Peaches is a beautiful orange-faced lovebird with emerald green feathers. Despite her small size, she has the biggest attitude! She loves to shred paper and tuck the strips into her feathers. Very bonded to her human family.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f7/Fischers_lovebird.jpg/640px-Fischers_lovebird.jpg',
    156, 41, now() - INTERVAL '6 months', false, 5, 0),

    -- Bird 4: Green Cheek Conure
    (gen_random_uuid(), target_user_id, 'Kiwi', 'Green-Cheeked Conure', 'Cuddly clown who loves adventures',
    'Kiwi is a playful green cheek conure who thinks he''s a big parrot. He loves to hang upside down, wrestle with toys, and snuggle under blankets. His gentle nature makes him perfect for family interaction.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/1/1f/Green-cheeked_Parakeet_%28Pyrrhura_molinae%29_-eating-2.jpg/640px-Green-cheeked_Parakeet_%28Pyrrhura_molinae%29_-eating-2.jpg',
    203, 58, now() - INTERVAL '8 months', false, 5, 0),

    -- Bird 5: African Grey
    (gen_random_uuid(), target_user_id, 'Einstein', 'African Grey Parrot', 'The genius who talks back',
    'Einstein lives up to his name with an incredible vocabulary of over 200 words and phrases. He answers questions, tells jokes, and even argues sometimes! His intelligence constantly amazes us, and he loves puzzle toys that challenge his mind.',
    'https://upload.wikimedia.org/wikipedia/commons/thumb/6/66/Psittacus_erithacus_-perching_on_tray-8d.jpg/640px-Psittacus_erithacus_-perching_on_tray-8d.jpg',
    445, 127, now() - INTERVAL '1 year', true, 10, 5000)

    ON CONFLICT (bird_id) DO NOTHING;

    -- Now insert stories for these birds (need to get the bird_ids we just created)
    INSERT INTO stories (story_id, bird_id, author_id, content, image_url, created_at, is_highlighted)
    SELECT
        gen_random_uuid(),
        b.bird_id,
        target_user_id,
        story.content,
        NULL,
        story.created_at,
        false
    FROM birds b
    CROSS JOIN (
        VALUES
        ('Mango', 'First song learned! Mango finally mastered the iPhone ringtone after weeks of practice. Now he performs it every time someone''s phone rings. The confusion he causes is hilarious!', now() - INTERVAL '3 months'),
        ('Mango', 'Mango discovered his reflection today and spent an hour serenading the "other bird." His heart songs are the sweetest sound. He even tried to feed the mirror!', now() - INTERVAL '2 months'),
        ('Mango', 'Morning routine: Mango now wakes up at exactly 7 AM with a cheerful whistle and won''t stop until someone comes to say good morning. He''s our feathered alarm clock!', now() - INTERVAL '1 week'),
        ('Blueberry', 'Blueberry learned a new trick today - he flies through a small hoop for millet spray! Took two weeks of training but he''s so proud of himself.', now() - INTERVAL '2 months'),
        ('Blueberry', 'Caught Blueberry having a full conversation with himself in the mirror. He was bobbing, chattering, and seemed to be teaching his reflection new words!', now() - INTERVAL '3 weeks'),
        ('Peaches', 'Paper shredding champion! Peaches destroyed an entire newspaper in under an hour and decorated herself with the strips. She looked like a tiny green porcupine!', now() - INTERVAL '5 months'),
        ('Peaches', 'Peaches met a new person today and immediately claimed them as her new best friend. She''s very selective, so this visitor should feel honored!', now() - INTERVAL '2 months'),
        ('Peaches', 'Discovered Peaches hiding treats in her food bowl like a little treasure hoard. She gets so defensive when we clean it. Such a funny little bird!', now() - INTERVAL '1 month'),
        ('Kiwi', 'Kiwi has developed a new game: he drops his toy, waits for us to pick it up, then immediately drops it again. He could play this for hours!', now() - INTERVAL '7 months'),
        ('Kiwi', 'Blanket monster! Kiwi found his way into a fleece blanket and made it his cave. He peeks out occasionally, then retreats back into his cozy fortress.', now() - INTERVAL '4 months'),
        ('Kiwi', 'Kiwi learned to give kisses! He makes a little smooching sound and tilts his head. Absolutely melts our hearts every time.', now() - INTERVAL '2 weeks'),
        ('Einstein', 'Einstein said his first full sentence today: "What are you doing?" in perfect context when I was cooking. This bird understands more than he lets on!', now() - INTERVAL '11 months'),
        ('Einstein', 'Einstein now answers the phone by saying "Hello? Hello?" and gets confused when no one responds. He''s also learned to imitate our laughter.', now() - INTERVAL '6 months'),
        ('Einstein', 'Puzzle master! Einstein figured out a new foraging toy in just 15 minutes. He looked so smug afterwards, like he was saying "Is that all you''ve got?"', now() - INTERVAL '3 months'),
        ('Einstein', 'Einstein started singing "Happy Birthday" completely unprompted today. It''s not anyone''s birthday. He just felt like celebrating!', now() - INTERVAL '1 week')
    ) AS story(bird_name, content, created_at)
    WHERE b.name = story.bird_name AND b.owner_id = target_user_id;

    RAISE NOTICE 'Successfully added birds and stories for faaf.dv@example.com!';

END;
$$ LANGUAGE plpgsql;

COMMIT;
