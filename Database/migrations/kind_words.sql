-- Kind Words (Comments) System Migration
-- A constrained, care-focused comment system for Wihngo

-- 1. Kind Words table
CREATE TABLE IF NOT EXISTS kind_words (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    author_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    text VARCHAR(200) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    is_visible BOOLEAN NOT NULL DEFAULT TRUE,

    -- Indexes for common queries
    CONSTRAINT kind_words_text_not_empty CHECK (LENGTH(TRIM(text)) > 0)
);

-- Index for fetching kind words by bird (most common query)
CREATE INDEX IF NOT EXISTS idx_kind_words_bird_id ON kind_words(bird_id);

-- Index for fetching kind words by author
CREATE INDEX IF NOT EXISTS idx_kind_words_author ON kind_words(author_user_id);

-- Index for filtering visible, non-deleted kind words
CREATE INDEX IF NOT EXISTS idx_kind_words_visible ON kind_words(bird_id, is_deleted, is_visible);

-- 2. Bird settings for kind words (add column to birds table)
ALTER TABLE birds ADD COLUMN IF NOT EXISTS kind_words_enabled BOOLEAN NOT NULL DEFAULT TRUE;

-- 3. Blocked users table (bird owner can block specific users)
CREATE TABLE IF NOT EXISTS kind_words_blocked_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    blocked_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    blocked_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    blocked_by_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,

    -- Prevent duplicate blocks
    CONSTRAINT uq_kind_words_blocked UNIQUE (bird_id, blocked_user_id)
);

-- Index for checking if user is blocked
CREATE INDEX IF NOT EXISTS idx_kind_words_blocked ON kind_words_blocked_users(bird_id, blocked_user_id);

-- 4. Rate limiting tracking (3 kind words per bird per day per user)
-- This is handled in-app but we add an index to support the query
CREATE INDEX IF NOT EXISTS idx_kind_words_rate_limit ON kind_words(bird_id, author_user_id, created_at);

-- Migration complete
