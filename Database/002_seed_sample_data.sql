-- 002_seed_sample_data.sql
-- Seed data that aligns with the React UI samples

BEGIN;

-- Users
INSERT INTO users (user_id, name, email, password_hash, profile_image, avatar, location, bio, created_at)
VALUES
('33333333-3333-3333-3333-333333333333', 'Sarah Chen', 'sarah.chen@wihngo.local', '', NULL, '??', 'Portland, OR', 'Bird lover and backyard habitat creator. Finding peace in the flutter of wings.', now())
ON CONFLICT (email) DO NOTHING;

INSERT INTO users (user_id, name, email, password_hash, profile_image, avatar, location, bio, created_at)
VALUES
('44444444-4444-4444-4444-444444444444', 'Local Birder', 'birder@wihngo.local', '', NULL, '??', 'Salem, OR', 'Obsessed with local migrants and feeders.', now())
ON CONFLICT (email) DO NOTHING;

INSERT INTO users (user_id, name, email, password_hash, profile_image, avatar, location, bio, created_at)
VALUES
('55555555-5555-5555-5555-555555555555', 'Backyard Supporter', 'supporter2@wihngo.local', '', NULL, '???', 'Beaverton, OR', 'Enjoys supporting local birds', now())
ON CONFLICT (email) DO NOTHING;

-- Birds for Sarah
INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, loved_count, supported_count, created_at)
VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', '33333333-3333-3333-3333-333333333333', 'Anna''s Hummingbird', 'Calypte anna', 'A tiny jewel that brings wonder year-round', 'Named after Anna Masséna, this hummingbird is known for its iridescent rose-pink crown and throat.', NULL, 2847, 423, now())
ON CONFLICT (bird_id) DO NOTHING;

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, loved_count, supported_count, created_at)
VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002', '33333333-3333-3333-3333-333333333333', 'American Robin', 'Turdus migratorius', 'The harbinger of spring with a cheerful song', 'A familiar sight in lawns and gardens; known for its rusty-orange breast and cheerful foraging behavior.', NULL, 1200, 210, now())
ON CONFLICT (bird_id) DO NOTHING;

INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, loved_count, supported_count, created_at)
VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003', '33333333-3333-3333-3333-333333333333', 'Black-capped Chickadee', 'Poecile atricapillus', 'A tiny, curious companion to bird feeders', 'A small, friendly songbird that is bold around people and frequent at feeders.', NULL, 980, 75, now())
ON CONFLICT (bird_id) DO NOTHING;

-- Additional bird
INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, loved_count, supported_count, created_at)
VALUES
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001', '44444444-4444-4444-4444-444444444444', 'Anna''s Juvenile', 'Calypte anna', 'Young hummingbird frequenting garden feeders', 'A juvenile Anna''s that started visiting feeders this season.', NULL, 45, 3, now())
ON CONFLICT (bird_id) DO NOTHING;

-- Stories
INSERT INTO stories (story_id, bird_id, author_id, content, image_url, created_at)
VALUES
('dddddddd-dddd-dddd-dddd-dddddddddd01', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', '33333333-3333-3333-3333-333333333333', 'Morning Visitor: This tiny jewel has been visiting my feeder every morning at 7am. The iridescent pink on its throat catches the sunrise beautifully and brightens my day.', NULL, now() - INTERVAL '2 days')
ON CONFLICT (story_id) DO NOTHING;

INSERT INTO stories (story_id, bird_id, author_id, content, image_url, created_at)
VALUES
('dddddddd-dddd-dddd-dddd-dddddddddd02', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002', '33333333-3333-3333-3333-333333333333', 'First Robin of Spring: Heard that cheerful song today and knew spring had truly arrived. Watched it hopping across the lawn, listening for earthworms.', NULL, now() - INTERVAL '7 days')
ON CONFLICT (story_id) DO NOTHING;

INSERT INTO stories (story_id, bird_id, author_id, content, image_url, created_at)
VALUES
('dddddddd-dddd-dddd-dddd-dddddddddd03', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003', '44444444-4444-4444-4444-444444444444', 'Chickadee antics: Spotted a pair of chickadees inspecting a new suet feeder this morning. They were fearless and delightful to watch.', NULL, now() - INTERVAL '1 days')
ON CONFLICT (story_id) DO NOTHING;

-- Support transactions
INSERT INTO support_transactions (transaction_id, supporter_id, bird_id, amount, message, created_at)
VALUES
('eeeeeeee-eeee-eeee-eeee-eeeeeeee0001', '55555555-5555-5555-5555-555555555555', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 5.00, 'Love this little hummingbird!', now() - INTERVAL '3 days')
ON CONFLICT (transaction_id) DO NOTHING;

INSERT INTO support_transactions (transaction_id, supporter_id, bird_id, amount, message, created_at)
VALUES
('eeeeeeee-eeee-eeee-eeee-eeeeeeee0002', '22222222-2222-2222-2222-222222222222', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 10.00, 'For feeder upkeep', now() - INTERVAL '10 days')
ON CONFLICT (transaction_id) DO NOTHING;

INSERT INTO support_transactions (transaction_id, supporter_id, bird_id, amount, message, created_at)
VALUES
('eeeeeeee-eeee-eeee-eeee-eeeeeeee0003', '55555555-5555-5555-5555-555555555555', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002', 7.50, 'Thanks for the joyful robin stories', now() - INTERVAL '5 days')
ON CONFLICT (transaction_id) DO NOTHING;

-- Update supported_count
UPDATE birds b
SET supported_count = COALESCE(sub.cnt, 0)
FROM (
  SELECT bird_id, COUNT(*) AS cnt
  FROM support_transactions
  GROUP BY bird_id
) AS sub
WHERE b.bird_id = sub.bird_id;

COMMIT;
