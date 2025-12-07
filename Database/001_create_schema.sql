-- 001_create_schema.sql
-- Create Wihngo schema for PostgreSQL
-- Run as a superuser or a user with CREATE EXTENSION and CREATE privileges

BEGIN;

-- Enable pgcrypto for gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Drop existing tables (CASCADE to remove dependent objects)
DROP TABLE IF EXISTS support_transactions CASCADE;
DROP TABLE IF EXISTS stories CASCADE;
DROP TABLE IF EXISTS birds CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Users table
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

-- Birds table
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

-- Indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON users (email);
CREATE INDEX IF NOT EXISTS idx_birds_owner ON birds (owner_id);
CREATE INDEX IF NOT EXISTS idx_stories_bird ON stories (bird_id);
CREATE INDEX IF NOT EXISTS idx_stories_author ON stories (author_id);
CREATE INDEX IF NOT EXISTS idx_support_transactions_supporter ON support_transactions (supporter_id);
CREATE INDEX IF NOT EXISTS idx_support_transactions_bird ON support_transactions (bird_id);

COMMIT;
