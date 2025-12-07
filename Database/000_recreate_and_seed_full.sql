-- 000_recreate_and_seed_full.sql
-- Destructive: Drop and recreate the wihngo database, create schema and seed richer data
-- Run interactively with psql as a superuser (will drop and recreate the database):
-- psql -h <host> -U <user> -d postgres -f Database/000_recreate_and_seed_full.sql
-- Or run step-by-step manually: uncomment DROP/CREATE DB lines below when connected to 'postgres' DB.

-- Uncomment the next two lines to drop and recreate the database (destructive)
-- DROP DATABASE IF EXISTS wihngo;
-- CREATE DATABASE wihngo OWNER postgres;

-- If run interactively from a psql session, connect to the new DB now:
-- \c wihngo

-- From here onward the script can be run while connected to the target database (wihngo)

BEGIN;

-- Ensure pgcrypto is available for gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Clean up existing tables if any
DROP TABLE IF EXISTS bird_fun_facts CASCADE;
DROP TABLE IF EXISTS bird_personalities CASCADE;
DROP TABLE IF EXISTS bird_conservation CASCADE;
DROP TABLE IF EXISTS support_transactions CASCADE;
DROP TABLE IF EXISTS stories CASCADE;
DROP TABLE IF EXISTS birds CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Users
CREATE TABLE users (
  user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(200) NOT NULL,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash TEXT NOT NULL,
  profile_image TEXT,
  avatar VARCHAR(16),
  location VARCHAR(255),
  bio TEXT,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Birds
CREATE TABLE birds (
  bird_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  name VARCHAR(200) NOT NULL,
  species VARCHAR(200),
  tagline VARCHAR(500),
  description TEXT,
  image_url TEXT,
  loved_count INT NOT NULL DEFAULT 0,
  supported_count INT NOT NULL DEFAULT 0,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Stories
CREATE TABLE stories (
  story_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
  author_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  content TEXT NOT NULL,
  image_url TEXT,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Support transactions
CREATE TABLE support_transactions (
  transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  supporter_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
  amount NUMERIC(12,2) NOT NULL CHECK (amount > 0),
  message TEXT,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Rich data: personality, fun facts, conservation
CREATE TABLE bird_personalities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
  trait TEXT NOT NULL
);

CREATE TABLE bird_fun_facts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
  fact TEXT NOT NULL
);

CREATE TABLE bird_conservation (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  bird_id UUID NOT NULL UNIQUE REFERENCES birds(bird_id) ON DELETE CASCADE,
  status VARCHAR(100) NOT NULL,
  needs TEXT
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON users (email);
CREATE INDEX IF NOT EXISTS idx_birds_owner ON birds (owner_id);
CREATE INDEX IF NOT EXISTS idx_stories_bird ON stories (bird_id);
CREATE INDEX IF NOT EXISTS idx_stories_author ON stories (author_id);
CREATE INDEX IF NOT EXISTS idx_support_transactions_supporter ON support_transactions (supporter_id);
CREATE INDEX IF NOT EXISTS idx_support_transactions_bird ON support_transactions (bird_id);
CREATE INDEX IF NOT EXISTS idx_personalities_bird ON bird_personalities (bird_id);
CREATE INDEX IF NOT EXISTS idx_funfacts_bird ON bird_fun_facts (bird_id);

-- SEED DATA
-- Users
INSERT INTO users (user_id, name, email, password_hash, profile_image, avatar, location, bio, created_at)
VALUES
('11111111-1111-1111-1111-111111111111', 'Wihngo Owner', 'owner@wihngo.local', '', NULL, '??', 'Portland, OR', 'I love birds', now()),
('22222222-2222-2222-2222-222222222222', 'Supporter One', 'supporter@wihngo.local', '', NULL, '??', 'Eugene, OR', 'Happy to support birds', now()),
('33333333-3333-3333-3333-333333333333', 'Sarah Chen', 'sarah.chen@wihngo.local', '', NULL, '??', 'Portland, OR', 'Bird lover and backyard habitat creator. Finding peace in the flutter of wings.', now()),
('44444444-4444-4444-4444-444444444444', 'Local Birder', 'birder@wihngo.local', '', NULL, '??', 'Salem, OR', 'Obsessed with local migrants and feeders.', now()),
('55555555-5555-5555-5555-555555555555', 'Backyard Supporter', 'supporter2@wihngo.local', '', NULL, '???', 'Beaverton, OR', 'Enjoys supporting local birds', now())
ON CONFLICT (email) DO NOTHING;

-- Birds
INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, loved_count, supported_count, created_at)
VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Sunny', 'Serinus canaria', 'A cheerful yellow canary', 'A cheerful yellow canary', NULL, 12, 5, now()),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', '33333333-3333-3333-3333-333333333333', 'Anna''s Hummingbird', 'Calypte anna', 'A tiny jewel that brings wonder year-round', 'Named after Anna Masséna, Duchess of Rivoli, this remarkable hummingbird is known for its iridescent rose-pink crown and throat. Year-round residents along the Pacific Coast.', NULL, 2847, 423, now()),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002', '33333333-3333-3333-3333-333333333333', 'American Robin', 'Turdus migratorius', 'The harbinger of spring with a cheerful song', 'A familiar sight in lawns and gardens; known for its rusty-orange breast and cheerful foraging behavior.', NULL, 1200, 210, now()),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003', '33333333-3333-3333-3333-333333333333', 'Black-capped Chickadee', 'Poecile atricapillus', 'A tiny, curious companion to bird feeders', 'A small, friendly songbird that is bold around people and frequent at feeders; easily recognized by its black cap and bib.', NULL, 980, 75, now()),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '44444444-4444-4444-4444-444444444444', 'Anna''s Juvenile', 'Calypte anna', 'Young hummingbird frequenting garden feeders', 'A juvenile Anna''s that started visiting feeders this season.', NULL, 45, 3, now())
ON CONFLICT (bird_id) DO NOTHING;

-- Stories
INSERT INTO stories (story_id, bird_id, author_id, content, image_url, created_at)
VALUES
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Sunny sang beautifully today!', NULL, now()),
('dddddddd-dddd-dddd-dddd-dddddddddd01', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', '33333333-3333-3333-3333-333333333333', 'Morning Visitor: This tiny jewel has been visiting my feeder every morning at 7am. The iridescent pink on its throat catches the sunrise beautifully and brightens my day.', NULL, now() - INTERVAL '2 days'),
('dddddddd-dddd-dddd-dddd-dddddddddd02', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002', '33333333-3333-3333-3333-333333333333', 'First Robin of Spring: Heard that cheerful song today and knew spring had truly arrived. Watched it hopping across the lawn, listening for earthworms.', NULL, now() - INTERVAL '7 days'),
('dddddddd-dddd-dddd-dddd-dddddddddd03', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003', '44444444-4444-4444-4444-444444444444', 'Chickadee antics: Spotted a pair of chickadees inspecting a new suet feeder this morning. They were fearless and delightful to watch.', NULL, now() - INTERVAL '1 days')
ON CONFLICT (story_id) DO NOTHING;

-- Support transactions
INSERT INTO support_transactions (transaction_id, supporter_id, bird_id, amount, message, created_at)
VALUES
('cccccccc-cccc-cccc-cccc-cccccccccccc', '22222222-2222-2222-2222-222222222222', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 10.00, 'Keep up the good care!', now()),
('eeeeeeee-eeee-eeee-eeee-eeeeeeee0001', '55555555-5555-5555-5555-555555555555', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 5.00, 'Love this little hummingbird!', now() - INTERVAL '3 days'),
('eeeeeeee-eeee-eeee-eeee-eeeeeeee0002', '22222222-2222-2222-2222-222222222222', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 10.00, 'For feeder upkeep', now() - INTERVAL '10 days'),
('eeeeeeee-eeee-eeee-eeee-eeeeeeee0003', '55555555-5555-5555-5555-555555555555', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002', 7.50, 'Thanks for the joyful robin stories', now() - INTERVAL '5 days')
ON CONFLICT (transaction_id) DO NOTHING;

-- Personality traits for birds
INSERT INTO bird_personalities (id, bird_id, trait)
VALUES
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'Fearless and territorial'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'Incredibly vocal for their size'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'Early risers who sing before dawn'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'Devoted parents'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002', 'Foraging specialists'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003', 'Bold and curious')
ON CONFLICT (id) DO NOTHING;

-- Fun facts
INSERT INTO bird_fun_facts (id, bird_id, fact)
VALUES
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'Males perform spectacular dive displays, reaching speeds of 60 mph'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'They can remember every flower they''ve visited'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'Their heart beats up to 1,260 times per minute'),
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'They''re one of the few hummingbirds that sing')
ON CONFLICT (id) DO NOTHING;

-- Conservation entries
INSERT INTO bird_conservation (id, bird_id, status, needs)
VALUES
(gen_random_uuid(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001', 'Least Concern', 'Native plant gardens, year-round nectar sources, pesticide-free habitats')
ON CONFLICT (id) DO NOTHING;

-- Recompute bird supported_count from transactions
UPDATE birds b
SET supported_count = COALESCE(sub.cnt, 0)
FROM (
  SELECT bird_id, COUNT(*) AS cnt
  FROM support_transactions
  GROUP BY bird_id
) AS sub
WHERE b.bird_id = sub.bird_id;

COMMIT;
