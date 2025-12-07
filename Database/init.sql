-- Initialize Wihngo PostgreSQL schema and sample seed data
-- Run as a superuser or a user with CREATE EXTENSION and CREATE privileges.

-- Enable UUID generation functions (choose one depending on your Postgres setup)
-- Option A: pgcrypto (Postgres 13+ commonly available)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Option B: use uuid-ossp instead if preferred
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Drop existing tables if you want to recreate
DROP TABLE IF EXISTS support_transactions;
DROP TABLE IF EXISTS stories;
DROP TABLE IF EXISTS birds;
DROP TABLE IF EXISTS users;

-- Users table
CREATE TABLE users (
  user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(200) NOT NULL,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash TEXT NOT NULL,
  profile_image TEXT,
  bio TEXT,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Birds table
CREATE TABLE birds (
  bird_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  name VARCHAR(200) NOT NULL,
  species VARCHAR(200),
  description TEXT,
  image_url TEXT,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Stories table
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

-- Sample seed data (replace password_hash values via registration or with a bcrypt hash)
-- NOTE: It's safer to create users via the API which will hash passwords properly. If you want to seed password hashes here,
-- compute bcrypt hashes externally and replace the empty string values below.

-- Fixed UUIDs to reference in related tables
-- Owner user
INSERT INTO users (user_id, name, email, password_hash, profile_image, bio, created_at)
VALUES
('11111111-1111-1111-1111-111111111111', 'Wihngo Owner', 'owner@wihngo.local', '', NULL, 'I love birds', now()),
('22222222-2222-2222-2222-222222222222', 'Supporter One', 'supporter@wihngo.local', '', NULL, 'Happy to support birds', now());

-- Sample bird owned by the owner
INSERT INTO birds (bird_id, owner_id, name, species, description, image_url, created_at)
VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Sunny', 'Canary', 'A cheerful yellow canary', NULL, now());

-- Sample story for the bird
INSERT INTO stories (story_id, bird_id, author_id, content, image_url, created_at)
VALUES
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Sunny sang beautifully today!', NULL, now());

-- Sample support transaction (supporter supports the bird)
INSERT INTO support_transactions (transaction_id, supporter_id, bird_id, amount, message, created_at)
VALUES
('cccccccc-cccc-cccc-cccc-cccccccccccc', '22222222-2222-2222-2222-222222222222', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 10.00, 'Keep up the good care!', now());

-- End of init script
