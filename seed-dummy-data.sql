-- ============================================
-- Wihngo Dummy Data Seed Script
-- Populates database with sample data
-- ============================================

\c wihngo_kzno;

-- Add missing column first
ALTER TABLE support_transactions ADD COLUMN IF NOT EXISTS message TEXT;

-- Insert 10 sample users
INSERT INTO users (user_id, name, email, password_hash, bio, created_at) VALUES
(gen_random_uuid(), 'Alice Johnson', 'alice@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Bird enthusiast and wildlife photographer', NOW()),
(gen_random_uuid(), 'Bob Smith', 'bob@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Nature lover and backyard birder', NOW()),
(gen_random_uuid(), 'Carol White', 'carol@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Ornithologist specializing in migratory patterns', NOW()),
(gen_random_uuid(), 'David Brown', 'david@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Conservation volunteer', NOW()),
(gen_random_uuid(), 'Emma Davis', 'emma@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Bird rescue and rehabilitation specialist', NOW()),
(gen_random_uuid(), 'Frank Miller', 'frank@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Birdwatching tour guide', NOW()),
(gen_random_uuid(), 'Grace Lee', 'grace@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Urban wildlife advocate', NOW()),
(gen_random_uuid(), 'Henry Wilson', 'henry@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Retired teacher and bird feeder enthusiast', NOW()),
(gen_random_uuid(), 'Iris Martinez', 'iris@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Wildlife biologist', NOW()),
(gen_random_uuid(), 'Jack Taylor', 'jack@example.com', '$2a$11$xKvL9ZhYBmNBqwHH7qNJeOxKZ6mYJl.6.9JG5Y0rZKvB5H9z5Y5Ou', 'Photographer capturing bird behavior', NOW())
ON CONFLICT (email) DO NOTHING;

-- Insert 20 sample birds
INSERT INTO birds (bird_id, name, species, description, image_url, video_url, owner_id, tagline, loved_count, supported_count, donation_cents, created_at)
SELECT 
    gen_random_uuid(),
    bird_data.name,
    bird_data.species,
    bird_data.description,
    bird_data.image_url,
    bird_data.video_url,
    u.user_id,
    bird_data.tagline,
    bird_data.loved_count,
    0,
    bird_data.donation_cents,
    NOW()
FROM (VALUES
    ('Charlie', 'American Robin', 'A friendly robin who visits my yard every morning', 'https://thegraphicsfairy.com/wp-content/uploads/2020/09/Robin-Branch-Vintage-Image-GraphicsFairy.jpg', 'https://cdn.pixabay.com/video/2019/11/13/29033-373125430_large.mp4', 'Morning visitor with a beautiful song', 12, 5000),
    ('Tweety', 'Yellow Warbler', 'Bright yellow songbird spotted near the creek', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297363271', NULL, 'Sunshine on wings', 8, 3000),
    ('Shadow', 'Common Raven', 'Intelligent and curious raven who follows me on hikes', 'https://www.allaboutbirds.org/guide/assets/photo/306348101-480px.jpg', NULL, 'The trickster of the forest', 15, 7500),
    ('Ruby', 'Annas Hummingbird', 'Tiny jewel who loves the red flowers', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297348321', 'https://cdn.pixabay.com/video/2021/08/14/85373-594847146_large.mp4', 'Fastest flier in my garden', 20, 10000),
    ('Blue', 'Blue Jay', 'Bold and noisy visitor at the feeder', 'https://www.allaboutbirds.org/guide/assets/photo/306339661-480px.jpg', NULL, 'The feeder king', 10, 4500),
    ('Penny', 'House Sparrow', 'Small but mighty backyard regular', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297350521', NULL, 'Little warrior of the suburbs', 6, 2000),
    ('Snowball', 'Snowy Owl', 'Rare winter visitor spotted at the lake', 'https://www.allaboutbirds.org/guide/assets/photo/306320261-480px.jpg', NULL, 'Arctic beauty', 25, 15000),
    ('Goldie', 'American Goldfinch', 'Bright yellow finch at thistle feeder', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297353161', NULL, 'Sunshine finch', 9, 3500),
    ('Redford', 'Northern Cardinal', 'Vibrant red male cardinal', 'https://www.allaboutbirds.org/guide/assets/photo/306340941-480px.jpg', NULL, 'Red royalty', 18, 9000),
    ('Splash', 'Mallard Duck', 'Friendly duck at the pond', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297364231', NULL, 'Pond master', 7, 2500),
    ('Woody', 'Downy Woodpecker', 'Small woodpecker who loves suet', 'https://www.allaboutbirds.org/guide/assets/photo/306341251-480px.jpg', NULL, 'Tiny drummer', 11, 4000),
    ('Luna', 'Barn Owl', 'Graceful night hunter', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297358721', NULL, 'Silent flight', 22, 12000),
    ('Pepper', 'Black-capped Chickadee', 'Cute and acrobatic visitor', 'https://www.allaboutbirds.org/guide/assets/photo/306339751-480px.jpg', NULL, 'Cheerful acrobat', 14, 6000),
    ('Sunny', 'Eastern Bluebird', 'Beautiful blue bird of happiness', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297349811', NULL, 'Bluebird of happiness', 16, 8000),
    ('Flash', 'Peregrine Falcon', 'Fast and powerful predator', 'https://www.allaboutbirds.org/guide/assets/photo/306332671-480px.jpg', NULL, 'Speed demon', 30, 20000),
    ('Coco', 'Brown Pelican', 'Coastal bird with impressive wingspan', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297362341', NULL, 'Ocean diver', 13, 5500),
    ('Daisy', 'Mourning Dove', 'Peaceful dove with soothing coo', 'https://www.allaboutbirds.org/guide/assets/photo/306336341-480px.jpg', NULL, 'Voice of peace', 9, 3200),
    ('Storm', 'Great Blue Heron', 'Majestic wading bird', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297361451', NULL, 'Patience personified', 19, 11000),
    ('Zippy', 'Ruby-throated Hummingbird', 'Energetic hummingbird', 'https://www.allaboutbirds.org/guide/assets/photo/306350971-480px.jpg', NULL, 'Tiny speedster', 17, 8500),
    ('Ace', 'Red-tailed Hawk', 'Powerful raptor of the skies', 'https://cdn.download.ams.birds.cornell.edu/api/v1/asset/297357841', NULL, 'Sky ruler', 21, 13000)
) AS bird_data(name, species, description, image_url, video_url, tagline, loved_count, donation_cents),
users u
ORDER BY RANDOM()
LIMIT 20;

-- Insert stories (3-5 per bird)
INSERT INTO stories (story_id, bird_id, user_id, author_id, content, image_url, created_at, is_highlighted)
SELECT 
    gen_random_uuid(),
    b.bird_id,
    b.owner_id,
    b.owner_id,
    story_data.content,
    story_data.image_url,
    NOW() - (story_data.days_ago || ' days')::INTERVAL,
    story_data.is_highlighted
FROM birds b
CROSS JOIN LATERAL (VALUES
    ('Just spotted this beautiful bird in my backyard! What a wonderful way to start the day.', NULL, 1, false),
    ('This bird has been visiting every morning for seeds. Such a loyal friend!', NULL, 5, false),
    ('Caught an amazing moment when it was singing. The melody is incredible!', NULL, 10, true),
    ('Built a nest right outside my window. Watching the family grow has been amazing.', NULL, 15, false),
    ('Today marked 30 days of daily visits. This bird has become part of my routine.', NULL, 20, false)
) AS story_data(content, image_url, days_ago, is_highlighted)
LIMIT 80;

-- Insert loves (random loves for birds)
INSERT INTO loves (love_id, user_id, bird_id, created_at)
SELECT 
    gen_random_uuid(),
    u.user_id,
    b.bird_id,
    NOW() - (RANDOM() * 30 || ' days')::INTERVAL
FROM users u
CROSS JOIN birds b
WHERE RANDOM() < 0.3
ON CONFLICT (user_id, bird_id) DO NOTHING;

-- Insert support transactions
INSERT INTO support_transactions (transaction_id, supporter_id, bird_id, amount, message, created_at)
SELECT 
    gen_random_uuid(),
    u.user_id,
    b.bird_id,
    (RANDOM() * 50 + 5)::DECIMAL(18,2),
    CASE (RANDOM() * 5)::INT
        WHEN 0 THEN 'Supporting your amazing bird!'
        WHEN 1 THEN 'Keep up the great work!'
        WHEN 2 THEN 'Love seeing these updates!'
        WHEN 3 THEN 'Happy to help!'
        ELSE 'Such a beautiful bird!'
    END,
    NOW() - (RANDOM() * 60 || ' days')::INTERVAL
FROM users u
CROSS JOIN birds b
WHERE RANDOM() < 0.15
LIMIT 50;

-- Insert notifications
INSERT INTO notifications (notification_id, user_id, type, title, message, bird_id, is_read, created_at)
SELECT 
    gen_random_uuid(),
    b.owner_id,
    'BirdLoved',
    u.name || ' loved ' || b.name || '!',
    u.name || ' loved your ' || b.species || '. You now have ' || b.loved_count || ' loves!',
    b.bird_id,
    RANDOM() < 0.5,
    NOW() - (RANDOM() * 10 || ' days')::INTERVAL
FROM birds b
CROSS JOIN users u
WHERE u.user_id != b.owner_id
AND RANDOM() < 0.2
LIMIT 100;

-- Update bird counts based on actual data
UPDATE birds b
SET loved_count = (SELECT COUNT(*) FROM loves WHERE bird_id = b.bird_id),
    supported_count = (SELECT COUNT(*) FROM support_transactions WHERE bird_id = b.bird_id),
    donation_cents = (SELECT COALESCE(SUM(amount * 100), 0) FROM support_transactions WHERE bird_id = b.bird_id);

-- Verification
SELECT 'Seed data summary:' as info;
SELECT 'Users: ' || COUNT(*) FROM users;
SELECT 'Birds: ' || COUNT(*) FROM birds;
SELECT 'Stories: ' || COUNT(*) FROM stories;
SELECT 'Loves: ' || COUNT(*) FROM loves;
SELECT 'Support Transactions: ' || COUNT(*) FROM support_transactions;
SELECT 'Notifications: ' || COUNT(*) FROM notifications;

SELECT '? Dummy data seeded successfully!' as message;
