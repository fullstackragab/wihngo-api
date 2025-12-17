-- =============================================
-- Wihngo Database Reset & Seed Script
-- =============================================
-- This script will:
-- 1. Delete all existing data (in correct order to respect foreign keys)
-- 2. Reset sequences
-- 3. Insert fresh seed data with proper relationships
-- =============================================

-- =============================================
-- STEP 1: DELETE ALL DATA (Respect Foreign Keys)
-- =============================================

BEGIN;

-- Disable triggers temporarily for faster deletion
SET session_replication_role = 'replica';

-- Delete in reverse dependency order
TRUNCATE TABLE memorial_messages CASCADE;
TRUNCATE TABLE memorial_fund_redirections CASCADE;
TRUNCATE TABLE comment_likes CASCADE;
TRUNCATE TABLE comments CASCADE;
TRUNCATE TABLE story_likes CASCADE;
TRUNCATE TABLE stories CASCADE;
TRUNCATE TABLE loves CASCADE;
TRUNCATE TABLE support_usages CASCADE;
TRUNCATE TABLE support_transactions CASCADE;
TRUNCATE TABLE bird_premium_subscriptions CASCADE;
TRUNCATE TABLE premium_styles CASCADE;
TRUNCATE TABLE birds CASCADE;
TRUNCATE TABLE payout_transactions CASCADE;
TRUNCATE TABLE payout_methods CASCADE;
TRUNCATE TABLE payout_balances CASCADE;
TRUNCATE TABLE charity_allocations CASCADE;
TRUNCATE TABLE charity_impact_stats CASCADE;
TRUNCATE TABLE payment_events CASCADE;
TRUNCATE TABLE refund_requests CASCADE;
TRUNCATE TABLE payments CASCADE;
TRUNCATE TABLE invoices CASCADE;
TRUNCATE TABLE webhooks_received CASCADE;
TRUNCATE TABLE audit_logs CASCADE;
TRUNCATE TABLE blockchain_cursors CASCADE;
TRUNCATE TABLE on_chain_deposits CASCADE;
TRUNCATE TABLE crypto_transactions CASCADE;
TRUNCATE TABLE crypto_payment_requests CASCADE;
TRUNCATE TABLE crypto_payment_methods CASCADE;
TRUNCATE TABLE crypto_exchange_rates CASCADE;
TRUNCATE TABLE platform_wallets CASCADE;
TRUNCATE TABLE token_configurations CASCADE;
TRUNCATE TABLE supported_tokens CASCADE;
TRUNCATE TABLE user_devices CASCADE;
TRUNCATE TABLE notification_preferences CASCADE;
TRUNCATE TABLE notification_settings CASCADE;
TRUNCATE TABLE notifications CASCADE;
TRUNCATE TABLE users CASCADE;

-- Re-enable triggers
SET session_replication_role = 'origin';

COMMIT;

-- =============================================
-- STEP 2: RESET SEQUENCES
-- =============================================

-- Reset invoice sequence
DROP SEQUENCE IF EXISTS wihngo_invoice_seq CASCADE;
CREATE SEQUENCE wihngo_invoice_seq START 1000;

-- =============================================
-- STEP 3: INSERT SEED DATA
-- =============================================

BEGIN;

-- =============================================
-- USERS (5 users)
-- =============================================
INSERT INTO users (
    user_id, name, email, password_hash, profile_image, bio, 
    created_at, email_confirmed, is_account_locked, failed_login_attempts,
    last_password_change_at
) VALUES
-- Alice (bird lover, active community member)
('11111111-1111-1111-1111-111111111111', 
 'Alice Johnson', 
 'alice@example.com', 
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'Bird enthusiast and backyard conservationist. Love watching hummingbirds!',
 NOW() - INTERVAL '90 days',
 true,
 false,
 0,
 NOW() - INTERVAL '90 days'),

-- Bob (photography enthusiast)
('22222222-2222-2222-2222-222222222222',
 'Bob Martinez',
 'bob@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'Wildlife photographer specializing in birds. Sharing my backyard visitors.',
 NOW() - INTERVAL '60 days',
 true,
 false,
 0,
 NOW() - INTERVAL '60 days'),

-- Carol (new user)
('33333333-3333-3333-3333-333333333333',
 'Carol Davis',
 'carol@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'Just started feeding birds in my yard. Learning so much!',
 NOW() - INTERVAL '15 days',
 true,
 false,
 0,
 NOW() - INTERVAL '15 days'),

-- David (rescue volunteer)
('44444444-4444-4444-4444-444444444444',
 'David Chen',
 'david@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'Wildlife rescue volunteer. Documenting rehabilitation journeys.',
 NOW() - INTERVAL '120 days',
 true,
 false,
 0,
 NOW() - INTERVAL '120 days'),

-- Emma (educator)
('55555555-5555-5555-5555-555555555555',
 'Emma Wilson',
 'emma@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'Elementary teacher using bird watching to teach kids about nature.',
 NOW() - INTERVAL '45 days',
 true,
 false,
 0,
 NOW() - INTERVAL '45 days');

-- =============================================
-- BIRDS (10 birds with different owners)
-- =============================================
INSERT INTO birds (
    bird_id, owner_id, name, species, tagline, description, image_url,
    created_at, loved_count, supported_count, donation_cents,
    is_premium, is_memorial, max_media_count
) VALUES
-- Alice's birds
('aaaaaaaa-0001-0001-0001-000000000001',
 '11111111-1111-1111-1111-111111111111',
 'Ruby',
 'Anna''s Hummingbird',
 'The fearless little warrior',
 'Ruby is a regular visitor to my backyard feeder. She''s incredibly territorial and will chase away birds twice her size! Her iridescent red gorget is absolutely stunning in the morning sunlight.',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/c/c5/Anna%27s_hummingbird.jpg/640px-Anna%27s_hummingbird.jpg',
 NOW() - INTERVAL '85 days',
 23, 8, 3500,
 true, false, 20),

('aaaaaaaa-0002-0002-0002-000000000002',
 '11111111-1111-1111-1111-111111111111',
 'Jasper',
 'Black-chinned Hummingbird',
 'The gentle visitor',
 'Jasper arrived in spring and has become a favorite. Unlike Ruby, he''s very calm and will even feed while I''m nearby. His purple chin stripe is gorgeous!',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/83/Black-chinned_Hummingbird.jpg/640px-Black-chinned_Hummingbird.jpg',
 NOW() - INTERVAL '70 days',
 15, 4, 1200,
 false, false, 10),

-- Bob's birds (premium)
('bbbbbbbb-0001-0001-0001-000000000001',
 '22222222-2222-2222-2222-222222222222',
 'Sunshine',
 'American Goldfinch',
 'Bright as morning light',
 'Sunshine brings joy to my garden every day. Her vibrant yellow feathers are like little rays of sunshine, especially in summer!',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/American_Goldfinch-27527.jpg/640px-American_Goldfinch-27527.jpg',
 NOW() - INTERVAL '55 days',
 31, 12, 5200,
 true, false, 20),

('bbbbbbbb-0002-0002-0002-000000000002',
 '22222222-2222-2222-2222-222222222222',
 'Bella',
 'House Finch',
 'The melodious singer',
 'Bella serenades us every morning with beautiful songs. She''s raised two broods in our yard this year!',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/9a/House_Finch_%28female%29.jpg/640px-House_Finch_%28female%29.jpg',
 NOW() - INTERVAL '50 days',
 18, 7, 2800,
 false, false, 10),

-- Carol's bird (new user)
('cccccccc-0001-0001-0001-000000000001',
 '33333333-3333-3333-3333-333333333333',
 'Chirpy',
 'House Sparrow',
 'My first backyard friend',
 'I just set up my first bird feeder and Chirpy was the first visitor! I''m learning so much about birds.',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/6/6e/Passer_domesticus_male_%2815%29.jpg/640px-Passer_domesticus_male_%2815%29.jpg',
 NOW() - INTERVAL '10 days',
 5, 1, 250,
 false, false, 10),

-- David's birds (rescue cases)
('dddddddd-0001-0001-0001-000000000001',
 '44444444-4444-4444-4444-444444444444',
 'Phoenix',
 'Red-tailed Hawk',
 'Rise from the ashes',
 'Phoenix was found injured on the roadside. After months of rehabilitation, she''s almost ready to return to the wild. Her recovery has been remarkable.',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/8a/Red-tailed_Hawk.jpg/640px-Red-tailed_Hawk.jpg',
 NOW() - INTERVAL '100 days',
 42, 18, 8500,
 true, false, 20),

('dddddddd-0002-0002-0002-000000000002',
 '44444444-4444-4444-4444-444444444444',
 'Hope',
 'American Robin',
 'Symbol of resilience',
 'Hope fell from her nest during a storm. We raised her and successfully released her back to her territory. She still visits!',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/b/b0/Turdus_migratorius.jpg/640px-Turdus_migratorius.jpg',
 NOW() - INTERVAL '80 days',
 28, 11, 4200,
 false, false, 10),

-- Emma's birds (educational)
('eeeeeeee-0001-0001-0001-000000000001',
 '55555555-5555-5555-5555-555555555555',
 'Professor Hoot',
 'Barred Owl',
 'The classroom favorite',
 'Professor Hoot lives in the tree near our school. My students love watching him and learning about owls.',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/ec/Strix-varia-Barred-Owl-cropped.jpg/640px-Strix-varia-Barred-Owl-cropped.jpg',
 NOW() - INTERVAL '40 days',
 35, 15, 6200,
 true, false, 50),

('eeeeeeee-0002-0002-0002-000000000002',
 '55555555-5555-5555-5555-555555555555',
 'Flutter',
 'Mourning Dove',
 'Gentle and peaceful',
 'Flutter visits our school garden daily. Kids love her soft cooing sounds.',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/b/b7/Mourning_Dove_2006.jpg/640px-Mourning_Dove_2006.jpg',
 NOW() - INTERVAL '35 days',
 12, 5, 1500,
 false, false, 10),

-- David's memorial bird
('dddddddd-0003-0003-0003-000000000003',
 '44444444-4444-4444-4444-444444444444',
 'Angel',
 'Blue Jay',
 'Forever in our hearts',
 'Angel was a rescue we couldn''t save. She taught us so much about compassion and fighting spirit. Gone but never forgotten.',
 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f4/Blue_jay_in_PP_%2830960%29.jpg/640px-Blue_jay_in_PP_%2830960%29.jpg',
 NOW() - INTERVAL '90 days',
 67, 25, 12500,
 true, true, 20);

-- =============================================
-- LOVES (Users loving birds)
-- =============================================
INSERT INTO loves (user_id, bird_id) VALUES
-- Alice loves
('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-0001-0001-0001-000000000001'), -- Sunshine
('11111111-1111-1111-1111-111111111111', 'dddddddd-0001-0001-0001-000000000001'), -- Phoenix
('11111111-1111-1111-1111-111111111111', 'eeeeeeee-0001-0001-0001-000000000001'), -- Professor Hoot
('11111111-1111-1111-1111-111111111111', 'dddddddd-0003-0003-0003-000000000003'), -- Angel

-- Bob loves
('22222222-2222-2222-2222-222222222222', 'aaaaaaaa-0001-0001-0001-000000000001'), -- Ruby
('22222222-2222-2222-2222-222222222222', 'dddddddd-0001-0001-0001-000000000001'), -- Phoenix
('22222222-2222-2222-2222-222222222222', 'eeeeeeee-0001-0001-0001-000000000001'), -- Professor Hoot

-- Carol loves
('33333333-3333-3333-3333-333333333333', 'aaaaaaaa-0001-0001-0001-000000000001'), -- Ruby
('33333333-3333-3333-3333-333333333333', 'bbbbbbbb-0001-0001-0001-000000000001'), -- Sunshine
('33333333-3333-3333-3333-333333333333', 'eeeeeeee-0001-0001-0001-000000000001'), -- Professor Hoot

-- David loves
('44444444-4444-4444-4444-444444444444', 'aaaaaaaa-0001-0001-0001-000000000001'), -- Ruby
('44444444-4444-4444-4444-444444444444', 'bbbbbbbb-0001-0001-0001-000000000001'), -- Sunshine
('44444444-4444-4444-4444-444444444444', 'eeeeeeee-0001-0001-0001-000000000001'), -- Professor Hoot

-- Emma loves
('55555555-5555-5555-5555-555555555555', 'aaaaaaaa-0001-0001-0001-000000000001'), -- Ruby
('55555555-5555-5555-5555-555555555555', 'bbbbbbbb-0001-0001-0001-000000000001'), -- Sunshine
('55555555-5555-5555-5555-555555555555', 'dddddddd-0001-0001-0001-000000000001'), -- Phoenix
('55555555-5555-5555-5555-555555555555', 'dddddddd-0003-0003-0003-000000000003'); -- Angel

-- =============================================
-- STORIES (20 stories)
-- =============================================
INSERT INTO stories (
    story_id, author_id, bird_id, content, mode, image_url, video_url, created_at
) VALUES
-- Ruby's stories
('11111111-1001-1001-1001-000000000001',
 '11111111-1111-1111-1111-111111111111',
 'aaaaaaaa-0001-0001-0001-000000000001',
 'Ruby had an amazing day today! She visited the feeder five times and even let me get within 3 feet while she was feeding. Her iridescent red gorget was absolutely stunning in the morning sunlight. I''m so proud of how far she''s come!',
 1, -- Excited
 'https://upload.wikimedia.org/wikipedia/commons/thumb/d/d2/Ruby-throated_Hummingbird_at_feeder.jpg/640px-Ruby-throated_Hummingbird_at_feeder.jpg',
 NULL,
 NOW() - INTERVAL '2 days'),

('11111111-1002-1002-1002-000000000002',
 '11111111-1111-1111-1111-111111111111',
 'aaaaaaaa-0001-0001-0001-000000000001',
 'Watched Ruby chase away a much larger bird today. She''s fearless! Her territorial behavior is fascinating to observe.',
 0, -- Happy
 NULL,
 NULL,
 NOW() - INTERVAL '5 days'),

-- Sunshine's stories
('22222222-1001-1001-1001-000000000001',
 '22222222-2222-2222-2222-222222222222',
 'bbbbbbbb-0001-0001-0001-000000000001',
 'Sunshine brought her partner to the feeder today! They''re such a beautiful pair. The male''s summer plumage is incredible - bright yellow with black wings.',
 1, -- Excited
 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a7/American_Goldfinches_%28Spinus_tristis%29.jpg/640px-American_Goldfinches_%28Spinus_tristis%29.jpg',
 NULL,
 NOW() - INTERVAL '1 day'),

('22222222-1002-1002-1002-000000000002',
 '22222222-2222-2222-2222-222222222222',
 'bbbbbbbb-0001-0001-0001-000000000001',
 'Spent an hour photographing Sunshine this morning. Managed to capture her with a sunflower seed in her beak. The lighting was perfect!',
 0, -- Happy
 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/95/American_goldfinch_winter_male.jpg/640px-American_goldfinch_winter_male.jpg',
 NULL,
 NOW() - INTERVAL '4 days'),

-- Phoenix's stories (rescue journey)
('44444444-1001-1001-1001-000000000001',
 '44444444-4444-4444-4444-444444444444',
 'dddddddd-0001-0001-0001-000000000001',
 'Major milestone today! Phoenix successfully caught prey during flight training. This is huge progress toward her release. The team is so proud!',
 1, -- Excited
 'https://upload.wikimedia.org/wikipedia/commons/thumb/0/02/Red-tailed_Hawk_Buteo_jamaicensis_Full_Body_1880px.jpg/640px-Red-tailed_Hawk_Buteo_jamaicensis_Full_Body_1880px.jpg',
 NULL,
 NOW() - INTERVAL '3 days'),

('44444444-1002-1002-1002-000000000002',
 '44444444-4444-4444-4444-444444444444',
 'dddddddd-0001-0001-0001-000000000001',
 'Phoenix''s wing is healing beautifully. X-rays show complete bone fusion. We''re starting flight exercises next week. She''s been so patient through this recovery.',
 2, -- Calm
 'https://upload.wikimedia.org/wikipedia/commons/thumb/d/d6/Red-tailed_Hawk_closeup.jpg/640px-Red-tailed_Hawk_closeup.jpg',
 NULL,
 NOW() - INTERVAL '10 days'),

-- Professor Hoot's stories
('55555555-1001-1001-1001-000000000001',
 '55555555-5555-5555-5555-555555555555',
 'eeeeeeee-0001-0001-0001-000000000001',
 'Professor Hoot gave the kids an amazing show today during recess! He caught a mouse right in front of them. It turned into an impromptu lesson on the food chain. The students were absolutely mesmerized!',
 1, -- Excited
 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/3c/Barred_owl_%28Strix_varia%29.jpg/640px-Barred_owl_%28Strix_varia%29.jpg',
 NULL,
 NOW() - INTERVAL '1 day'),

('55555555-1002-1002-1002-000000000002',
 '55555555-5555-5555-5555-555555555555',
 'eeeeeeee-0001-0001-0001-000000000001',
 'The kids made drawings of Professor Hoot in art class. He''s become our unofficial school mascot. Using him to teach about nocturnal animals and owl adaptations.',
 0, -- Happy
 NULL,
 NULL,
 NOW() - INTERVAL '7 days'),

-- Chirpy's stories (new bird)
('33333333-1001-1001-1001-000000000001',
 '33333333-3333-3333-3333-333333333333',
 'cccccccc-0001-0001-0001-000000000001',
 'Chirpy came back! I wasn''t sure if he''d return but he did! I''m learning that consistency is key with bird feeding. So excited!',
 1, -- Excited
 NULL,
 NULL,
 NOW() - INTERVAL '1 day'),

-- Bella's stories
('22222222-1003-1003-1003-000000000003',
 '22222222-2222-2222-2222-222222222222',
 'bbbbbbbb-0002-0002-0002-000000000002',
 'Bella''s babies fledged today! All three made it successfully. Watching them learn to fly was incredible. Mom is teaching them to use the feeders.',
 1, -- Excited
 'https://upload.wikimedia.org/wikipedia/commons/thumb/4/40/House_Finch_with_nestlings.jpg/640px-House_Finch_with_nestlings.jpg',
 NULL,
 NOW() - INTERVAL '6 days'),

-- Hope's stories
('44444444-1003-1003-1003-000000000003',
 '44444444-4444-4444-4444-444444444444',
 'dddddddd-0002-0002-0002-000000000002',
 'Hope returned to visit today! It''s been two weeks since her release. She recognized me and came close. This is why we do what we do.',
 0, -- Happy
 'https://upload.wikimedia.org/wikipedia/commons/thumb/c/c5/Turdus_migratorius_1.jpg/640px-Turdus_migratorius_1.jpg',
 NULL,
 NOW() - INTERVAL '2 days'),

-- More diverse stories
('11111111-1003-1003-1003-000000000003',
 '11111111-1111-1111-1111-111111111111',
 'aaaaaaaa-0002-0002-0002-000000000002',
 'Jasper is so different from Ruby! He''s much more relaxed and doesn''t chase other birds away. Peaceful feeder time.',
 2, -- Calm
 NULL,
 NULL,
 NOW() - INTERVAL '8 days'),

('33333333-1002-1002-1002-000000000002',
 '33333333-3333-3333-3333-333333333333',
 'cccccccc-0001-0001-0001-000000000001',
 'Set up my first bird bath! Chirpy was the first to try it. Watching birds bathe is adorable!',
 0, -- Happy
 'https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/House_sparrow_bathing.jpg/640px-House_sparrow_bathing.jpg',
 NULL,
 NOW() - INTERVAL '5 days'),

('55555555-1003-1003-1003-000000000003',
 '55555555-5555-5555-5555-555555555555',
 'eeeeeeee-0002-0002-0002-000000000002',
 'Flutter built a nest in our school garden! The kids are documenting the nesting process. Real-life science in action!',
 1, -- Excited
 NULL,
 NULL,
 NOW() - INTERVAL '12 days'),

-- Angel's memorial stories
('44444444-1004-1004-1004-000000000004',
 '44444444-4444-4444-4444-444444444444',
 'dddddddd-0003-0003-0003-000000000003',
 'Today marks one month since we lost Angel. She fought so hard. We learned so much from her about resilience and never giving up. Rest in peace, sweet bird.',
 4, -- Sad
 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e2/Blue_Jay-27527-2.jpg/640px-Blue_Jay-27527-2.jpg',
 NULL,
 NOW() - INTERVAL '30 days'),

-- Cross-user stories (users writing about others' birds)
('11111111-1004-1004-1004-000000000004',
 '11111111-1111-1111-1111-111111111111',
 'dddddddd-0001-0001-0001-000000000001',
 'Following Phoenix''s recovery journey. David, you''re doing amazing work! Can''t wait to see her fly free again.',
 0, -- Happy
 NULL,
 NULL,
 NOW() - INTERVAL '15 days'),

('22222222-1004-1004-1004-000000000004',
 '22222222-2222-2222-2222-222222222222',
 'eeeeeeee-0001-0001-0001-000000000001',
 'Professor Hoot visited my yard today! Emma, your famous owl is making rounds. Got some great photos!',
 1, -- Excited
 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/8f/Barred_owl_portrait.jpg/640px-Barred_owl_portrait.jpg',
 NULL,
 NOW() - INTERVAL '20 days'),

-- Recent stories
('11111111-1005-1005-1005-000000000005',
 '11111111-1111-1111-1111-111111111111',
 'aaaaaaaa-0001-0001-0001-000000000001',
 'Ruby defending her territory from a larger hummingbird. She''s fearless!',
 5, -- Playful
 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/85/Anna%27s_Hummingbird_in_flight.jpg/640px-Anna%27s_Hummingbird_in_flight.jpg',
 NULL,
 NOW() - INTERVAL '12 hours'),

('22222222-1005-1005-1005-000000000005',
 '22222222-2222-2222-2222-222222222222',
 'bbbbbbbb-0001-0001-0001-000000000001',
 'Perfect golden hour light for photographing Sunshine today.',
 0, -- Happy
 'https://upload.wikimedia.org/wikipedia/commons/thumb/b/b1/American_Goldfinch_%28Spinus_tristis%29_on_branch.jpg/640px-American_Goldfinch_%28Spinus_tristis%29_on_branch.jpg',
 NULL,
 NOW() - INTERVAL '18 hours'),

('55555555-1004-1004-1004-000000000004',
 '55555555-5555-5555-5555-555555555555',
 'eeeeeeee-0001-0001-0001-000000000001',
 'Professor Hoot hooting during our morning assembly. Perfect timing!',
 1, -- Excited
 NULL,
 NULL,
 NOW() - INTERVAL '6 hours');

-- =============================================
-- COMMENTS (on stories)
-- =============================================
INSERT INTO comments (
    comment_id, story_id, user_id, content, created_at
) VALUES
-- Comments on Ruby's latest story
('c0000001-0001-0001-0001-000000000001',
 '11111111-1005-1005-1005-000000000005',
 '22222222-2222-2222-2222-222222222222',
 'Ruby is amazing! I love how protective she is of her territory.',
 NOW() - INTERVAL '10 hours'),

('c0000002-0002-0002-0002-000000000002',
 '11111111-1005-1005-1005-000000000005',
 '55555555-5555-5555-5555-555555555555',
 'Great shot! The detail on her feathers is incredible.',
 NOW() - INTERVAL '8 hours'),

-- Comments on Phoenix's training story
('c0000003-0003-0003-0003-000000000003',
 '44444444-1001-1001-1001-000000000001',
 '11111111-1111-1111-1111-111111111111',
 'This is wonderful news! Phoenix''s recovery has been inspiring to follow. ??',
 NOW() - INTERVAL '2 days 20 hours'),

('c0000004-0004-0004-0004-000000000004',
 '44444444-1001-1001-1001-000000000001',
 '55555555-5555-5555-5555-555555555555',
 'David, your dedication is amazing. The kids at school are following Phoenix''s story!',
 NOW() - INTERVAL '2 days 18 hours'),

-- Comments on Professor Hoot's story
('c0000005-0005-0005-0005-000000000005',
 '55555555-1001-1001-1001-000000000001',
 '11111111-1111-1111-1111-111111111111',
 'What a great teaching moment! Your students are so lucky.',
 NOW() - INTERVAL '23 hours'),

('c0000006-0006-0006-0006-000000000006',
 '55555555-1001-1001-1001-000000000001',
 '44444444-4444-4444-4444-444444444444',
 'Love seeing wildlife used for education. Keep up the great work!',
 NOW() - INTERVAL '20 hours'),

-- Comments on Chirpy's story
('c0000007-0007-0007-0007-000000000007',
 '33333333-1001-1001-1001-000000000001',
 '11111111-1111-1111-1111-111111111111',
 'Welcome to bird feeding! It''s so rewarding. Keep consistent with your feeding times.',
 NOW() - INTERVAL '22 hours'),

-- Nested comment (reply to comment)
('c0000008-0008-0008-0008-000000000008',
 '33333333-1001-1001-1001-000000000001',
 '33333333-3333-3333-3333-333333333333',
 'Thank you! I''m feeding twice a day now. It''s become my favorite part of the day!',
 NOW() - INTERVAL '18 hours')
ON CONFLICT DO NOTHING;

-- Set parent for nested comment
UPDATE comments 
SET parent_comment_id = 'c0000007-0007-0007-0007-000000000007'
WHERE comment_id = 'c0000008-0008-0008-0008-000000000008';

-- =============================================
-- STORY LIKES
-- =============================================
INSERT INTO story_likes (like_id, story_id, user_id, created_at) VALUES
('sl000001-0001-0001-0001-000000000001', '11111111-1001-1001-1001-000000000001', '22222222-2222-2222-2222-222222222222', NOW() - INTERVAL '1 day 20 hours'),
('sl000002-0002-0002-0002-000000000002', '11111111-1001-1001-1001-000000000001', '33333333-3333-3333-3333-333333333333', NOW() - INTERVAL '1 day 18 hours'),
('sl000003-0003-0003-0003-000000000003', '11111111-1001-1001-1001-000000000001', '44444444-4444-4444-4444-444444444444', NOW() - INTERVAL '1 day 15 hours'),
('sl000004-0004-0004-0004-000000000004', '22222222-1001-1001-1001-000000000001', '11111111-1111-1111-1111-111111111111', NOW() - INTERVAL '20 hours'),
('sl000005-0005-0005-0005-000000000005', '22222222-1001-1001-1001-000000000001', '55555555-5555-5555-5555-555555555555', NOW() - INTERVAL '18 hours'),
('sl000006-0006-0006-0006-000000000006', '44444444-1001-1001-1001-000000000001', '11111111-1111-1111-1111-111111111111', NOW() - INTERVAL '2 days 15 hours'),
('sl000007-0007-0007-0007-000000000007', '44444444-1001-1001-1001-000000000001', '22222222-2222-2222-2222-222222222222', NOW() - INTERVAL '2 days 12 hours'),
('sl000008-0008-0008-0008-000000000008', '55555555-1001-1001-1001-000000000001', '11111111-1111-1111-1111-111111111111', NOW() - INTERVAL '20 hours'),
('sl000009-0009-0009-0009-000000000009', '55555555-1001-1001-1001-000000000001', '44444444-4444-4444-4444-444444444444', NOW() - INTERVAL '18 hours');

-- =============================================
-- COMMENT LIKES
-- =============================================
INSERT INTO comment_likes (like_id, comment_id, user_id, created_at) VALUES
('cl000001-0001-0001-0001-000000000001', 'c0000001-0001-0001-0001-000000000001', '11111111-1111-1111-1111-111111111111', NOW() - INTERVAL '9 hours'),
('cl000002-0002-0002-0002-000000000002', 'c0000001-0001-0001-0001-000000000001', '33333333-3333-3333-3333-333333333333', NOW() - INTERVAL '8 hours'),
('cl000003-0003-0003-0003-000000000003', 'c0000003-0003-0003-0003-000000000003', '44444444-4444-4444-4444-444444444444', NOW() - INTERVAL '2 days 15 hours'),
('cl000004-0004-0004-0004-000000000004', 'c0000005-0005-0005-0005-000000000005', '55555555-5555-5555-5555-555555555555', NOW() - INTERVAL '20 hours');

-- =============================================
-- SUPPORT TRANSACTIONS (Donations)
-- =============================================
INSERT INTO support_transactions (
    transaction_id, bird_id, supporter_id, amount, message, created_at
) VALUES
-- Donations to Phoenix (rescue bird)
('t0000001-0001-0001-0001-000000000001', 'dddddddd-0001-0001-0001-000000000001', '11111111-1111-1111-1111-111111111111', 25.00, 'For Phoenix''s medical care!', NOW() - INTERVAL '50 days'),
('t0000002-0002-0002-0002-000000000002', 'dddddddd-0001-0001-0001-000000000001', '22222222-2222-2222-2222-222222222222', 50.00, 'Amazing work! Keep it up!', NOW() - INTERVAL '45 days'),
('t0000003-0003-0003-0003-000000000003', 'dddddddd-0001-0001-0001-000000000001', '55555555-5555-5555-5555-555555555555', 35.00, NULL, NOW() - INTERVAL '30 days'),

-- Donations to Ruby
('t0000004-0004-0004-0004-000000000004', 'aaaaaaaa-0001-0001-0001-000000000001', '22222222-2222-2222-2222-222222222222', 10.00, 'Ruby is adorable!', NOW() - INTERVAL '60 days'),
('t0000005-0005-0005-0005-000000000005', 'aaaaaaaa-0001-0001-0001-000000000001', '44444444-4444-4444-4444-444444444444', 15.00, NULL, NOW() - INTERVAL '40 days'),

-- Donations to Professor Hoot
('t0000006-0006-0006-0006-000000000006', 'eeeeeeee-0001-0001-0001-000000000001', '11111111-1111-1111-1111-111111111111', 30.00, 'For the school program!', NOW() - INTERVAL '35 days'),
('t0000007-0007-0007-0007-000000000007', 'eeeeeeee-0001-0001-0001-000000000001', '22222222-2222-2222-2222-222222222222', 32.00, 'Educational birds are important!', NOW() - INTERVAL '25 days'),

-- Donations to Sunshine
('t0000008-0008-0008-0008-000000000008', 'bbbbbbbb-0001-0001-0001-000000000001', '11111111-1111-1111-1111-111111111111', 20.00, 'Beautiful photos!', NOW() - INTERVAL '45 days'),
('t0000009-0009-0009-0009-000000000009', 'bbbbbbbb-0001-0001-0001-000000000001', '44444444-4444-4444-4444-444444444444', 32.00, NULL, NOW() - INTERVAL '20 days'),

-- Memorial donations to Angel
('t0000010-0010-0010-0010-000000000010', 'dddddddd-0003-0003-0003-000000000003', '11111111-1111-1111-1111-111111111111', 50.00, 'In memory of Angel', NOW() - INTERVAL '28 days'),
('t0000011-0011-0011-0011-000000000011', 'dddddddd-0003-0003-0003-000000000003', '22222222-2222-2222-2222-222222222222', 75.00, 'RIP Angel. Thank you for all you taught us.', NOW() - INTERVAL '25 days');

-- =============================================
-- BIRD PREMIUM SUBSCRIPTIONS
-- =============================================
INSERT INTO bird_premium_subscriptions (
    id, bird_id, owner_id, status, plan, provider, provider_subscription_id,
    price_cents, duration_days, started_at, current_period_end, created_at, updated_at
) VALUES
-- Ruby - Monthly Premium
('sub00001-0001-0001-0001-000000000001',
 'aaaaaaaa-0001-0001-0001-000000000001',
 '11111111-1111-1111-1111-111111111111',
 'active',
 'monthly',
 'local',
 'sub_ruby_monthly_001',
 300,
 30,
 NOW() - INTERVAL '15 days',
 NOW() + INTERVAL '15 days',
 NOW() - INTERVAL '15 days',
 NOW() - INTERVAL '15 days'),

-- Sunshine - Lifetime Premium
('sub00002-0002-0002-0002-000000000002',
 'bbbbbbbb-0001-0001-0001-000000000001',
 '22222222-2222-2222-2222-222222222222',
 'active',
 'lifetime',
 'local',
 'sub_sunshine_lifetime_001',
 7000,
 2147483647, -- Max int (effectively infinite)
 NOW() - INTERVAL '45 days',
 '9999-12-31 23:59:59',
 NOW() - INTERVAL '45 days',
 NOW() - INTERVAL '45 days'),

-- Phoenix - Monthly Premium
('sub00003-0003-0003-0003-000000000003',
 'dddddddd-0001-0001-0001-000000000001',
 '44444444-4444-4444-4444-444444444444',
 'active',
 'monthly',
 'local',
 'sub_phoenix_monthly_001',
 300,
 30,
 NOW() - INTERVAL '20 days',
 NOW() + INTERVAL '10 days',
 NOW() - INTERVAL '20 days',
 NOW() - INTERVAL '20 days'),

-- Professor Hoot - Lifetime Premium
('sub00004-0004-0004-0004-000000000004',
 'eeeeeeee-0001-0001-0001-000000000001',
 '55555555-5555-5555-5555-555555555555',
 'active',
 'lifetime',
 'local',
 'sub_hoot_lifetime_001',
 7000,
 2147483647,
 NOW() - INTERVAL '35 days',
 '9999-12-31 23:59:59',
 NOW() - INTERVAL '35 days',
 NOW() - INTERVAL '35 days'),

-- Angel (memorial) - Lifetime Premium
('sub00005-0005-0005-0005-000000000005',
 'dddddddd-0003-0003-0003-000000000003',
 '44444444-4444-4444-4444-444444444444',
 'active',
 'lifetime',
 'local',
 'sub_angel_lifetime_001',
 7000,
 2147483647,
 NOW() - INTERVAL '85 days',
 '9999-12-31 23:59:59',
 NOW() - INTERVAL '85 days',
 NOW() - INTERVAL '85 days');

-- =============================================
-- MEMORIAL MESSAGES (for Angel)
-- =============================================
INSERT INTO memorial_messages (
    id, bird_id, user_id, message, is_anonymous, created_at
) VALUES
('mem00001-0001-0001-0001-000000000001',
 'dddddddd-0003-0003-0003-000000000003',
 '11111111-1111-1111-1111-111111111111',
 'Angel taught us that every life matters. Thank you for caring for her, David.',
 false,
 NOW() - INTERVAL '27 days'),

('mem00002-0002-0002-0002-000000000002',
 'dddddddd-0003-0003-0003-000000000003',
 '22222222-2222-2222-2222-222222222222',
 'Your beautiful spirit will never be forgotten. Fly free, Angel. ??',
 false,
 NOW() - INTERVAL '26 days'),

('mem00003-0003-0003-0003-000000000003',
 'dddddddd-0003-0003-0003-000000000003',
 '55555555-5555-5555-5555-555555555555',
 'My students made a memorial art piece for Angel. She touched many hearts.',
 false,
 NOW() - INTERVAL '24 days'),

('mem00004-0004-0004-0004-000000000004',
 'dddddddd-0003-0003-0003-000000000003',
 '33333333-3333-3333-3333-333333333333',
 'Even though I never met Angel, her story moved me to start helping birds.',
 false,
 NOW() - INTERVAL '20 days');

-- =============================================
-- SUPPORTED TOKENS (for crypto payments)
-- =============================================
INSERT INTO supported_tokens (
    id, token_symbol, chain, mint_address, decimals, is_active, tolerance_percent, created_at
) VALUES
('token001-0001-0001-0001-000000000001', 'USDC', 'solana', 'EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v', 6, true, 0.5, NOW() - INTERVAL '180 days'),
('token002-0002-0002-0002-000000000002', 'EURC', 'solana', 'HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr', 6, true, 0.5, NOW() - INTERVAL '180 days');

-- =============================================
-- PAYOUT BALANCES (for users with earnings)
-- =============================================
INSERT INTO payout_balances (
    id, user_id, available_balance, pending_balance, total_earned, total_paid_out,
    currency, last_payout_date, last_payout_amount, created_at, updated_at
) VALUES
-- Alice's balance
('bal00001-0001-0001-0001-000000000001',
 '11111111-1111-1111-1111-111111111111',
 35.00,
 0.00,
 60.00,
 25.00,
 'USD',
 NOW() - INTERVAL '35 days',
 25.00,
 NOW() - INTERVAL '90 days',
 NOW()),

-- Bob's balance (high earner)
('bal00002-0002-0002-0002-000000000002',
 '22222222-2222-2222-2222-222222222222',
 52.00,
 0.00,
 152.00,
 100.00,
 'USD',
 NOW() - INTERVAL '30 days',
 100.00,
 NOW() - INTERVAL '60 days',
 NOW()),

-- David's balance (rescue fundraising)
('bal00003-0003-0003-0003-000000000003',
 '44444444-4444-4444-4444-444444444444',
 235.00,
 0.00,
 485.00,
 250.00,
 'USD',
 NOW() - INTERVAL '30 days',
 250.00,
 NOW() - INTERVAL '120 days',
 NOW()),

-- Emma's balance (educational program)
('bal00004-0004-0004-0004-000000000004',
 '55555555-5555-5555-5555-555555555555',
 77.00,
 0.00,
 177.00,
 100.00,
 'USD',
 NOW() - INTERVAL '30 days',
 100.00,
 NOW() - INTERVAL '45 days',
 NOW());

COMMIT;

-- =============================================
-- VERIFICATION QUERIES
-- =============================================
SELECT 'Database reset and seed completed successfully!' as status;
SELECT '';
SELECT 'Summary of seeded data:' as info;
SELECT 'Users: ' || COUNT(*) as count FROM users;
SELECT 'Birds: ' || COUNT(*) as count FROM birds;
SELECT 'Stories: ' || COUNT(*) as count FROM stories;
SELECT 'Comments: ' || COUNT(*) as count FROM comments;
SELECT 'Loves: ' || COUNT(*) as count FROM loves;
SELECT 'Support Transactions: ' || COUNT(*) as count FROM support_transactions;
SELECT 'Premium Subscriptions: ' || COUNT(*) as count FROM bird_premium_subscriptions;
SELECT 'Memorial Messages: ' || COUNT(*) as count FROM memorial_messages;
SELECT 'Payout Balances: ' || COUNT(*) as count FROM payout_balances;
SELECT '';
SELECT 'Test credentials (all users same password):' as info;
SELECT '  Email: alice@example.com  | Password: Password123!' as credentials;
SELECT '  Email: bob@example.com    | Password: Password123!' as credentials;
SELECT '  Email: carol@example.com  | Password: Password123!' as credentials;
SELECT '  Email: david@example.com  | Password: Password123!' as credentials;
SELECT '  Email: emma@example.com   | Password: Password123!' as credentials;
