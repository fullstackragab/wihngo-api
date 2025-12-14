-- =========================================
-- Like and Comment System Migration
-- =========================================
-- This migration adds tables for:
-- 1. Story likes
-- 2. Comments (with nested reply support)
-- 3. Comment likes
-- =========================================

-- Table: story_likes
-- Stores likes on stories
CREATE TABLE IF NOT EXISTS story_likes (
    like_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    story_id UUID NOT NULL,
    user_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Foreign keys
    CONSTRAINT fk_story_likes_story FOREIGN KEY (story_id) 
        REFERENCES stories(story_id) ON DELETE CASCADE,
    CONSTRAINT fk_story_likes_user FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE CASCADE,
    
    -- Prevent duplicate likes (one like per user per story)
    CONSTRAINT uk_story_likes_user_story UNIQUE (story_id, user_id)
);

-- Index for efficient queries
CREATE INDEX IF NOT EXISTS idx_story_likes_story_id ON story_likes(story_id);
CREATE INDEX IF NOT EXISTS idx_story_likes_user_id ON story_likes(user_id);
CREATE INDEX IF NOT EXISTS idx_story_likes_created_at ON story_likes(created_at DESC);

COMMENT ON TABLE story_likes IS 'Stores likes on stories';
COMMENT ON COLUMN story_likes.like_id IS 'Primary key';
COMMENT ON COLUMN story_likes.story_id IS 'Reference to the liked story';
COMMENT ON COLUMN story_likes.user_id IS 'User who liked the story';
COMMENT ON COLUMN story_likes.created_at IS 'When the like was created';

-- =========================================

-- Table: comments
-- Stores comments on stories with support for nested replies
CREATE TABLE IF NOT EXISTS comments (
    comment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    story_id UUID NOT NULL,
    user_id UUID NOT NULL,
    content TEXT NOT NULL CHECK (char_length(content) <= 2000),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    parent_comment_id UUID,
    like_count INTEGER NOT NULL DEFAULT 0,
    
    -- Foreign keys
    CONSTRAINT fk_comments_story FOREIGN KEY (story_id) 
        REFERENCES stories(story_id) ON DELETE CASCADE,
    CONSTRAINT fk_comments_user FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT fk_comments_parent FOREIGN KEY (parent_comment_id) 
        REFERENCES comments(comment_id) ON DELETE CASCADE,
    
    -- Validation
    CONSTRAINT chk_comments_content_not_empty CHECK (char_length(trim(content)) > 0)
);

-- Indexes for efficient queries
CREATE INDEX IF NOT EXISTS idx_comments_story_id ON comments(story_id);
CREATE INDEX IF NOT EXISTS idx_comments_user_id ON comments(user_id);
CREATE INDEX IF NOT EXISTS idx_comments_parent_id ON comments(parent_comment_id);
CREATE INDEX IF NOT EXISTS idx_comments_created_at ON comments(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_comments_story_created ON comments(story_id, created_at DESC);

COMMENT ON TABLE comments IS 'Stores comments on stories with nested reply support';
COMMENT ON COLUMN comments.comment_id IS 'Primary key';
COMMENT ON COLUMN comments.story_id IS 'Reference to the story being commented on';
COMMENT ON COLUMN comments.user_id IS 'User who wrote the comment';
COMMENT ON COLUMN comments.content IS 'Comment text (max 2000 characters)';
COMMENT ON COLUMN comments.created_at IS 'When the comment was created';
COMMENT ON COLUMN comments.updated_at IS 'When the comment was last edited';
COMMENT ON COLUMN comments.parent_comment_id IS 'Parent comment for nested replies (null for top-level)';
COMMENT ON COLUMN comments.like_count IS 'Cached count of likes on this comment';

-- =========================================

-- Table: comment_likes
-- Stores likes on comments
CREATE TABLE IF NOT EXISTS comment_likes (
    like_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    comment_id UUID NOT NULL,
    user_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Foreign keys
    CONSTRAINT fk_comment_likes_comment FOREIGN KEY (comment_id) 
        REFERENCES comments(comment_id) ON DELETE CASCADE,
    CONSTRAINT fk_comment_likes_user FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE CASCADE,
    
    -- Prevent duplicate likes (one like per user per comment)
    CONSTRAINT uk_comment_likes_user_comment UNIQUE (comment_id, user_id)
);

-- Indexes for efficient queries
CREATE INDEX IF NOT EXISTS idx_comment_likes_comment_id ON comment_likes(comment_id);
CREATE INDEX IF NOT EXISTS idx_comment_likes_user_id ON comment_likes(user_id);
CREATE INDEX IF NOT EXISTS idx_comment_likes_created_at ON comment_likes(created_at DESC);

COMMENT ON TABLE comment_likes IS 'Stores likes on comments';
COMMENT ON COLUMN comment_likes.like_id IS 'Primary key';
COMMENT ON COLUMN comment_likes.comment_id IS 'Reference to the liked comment';
COMMENT ON COLUMN comment_likes.user_id IS 'User who liked the comment';
COMMENT ON COLUMN comment_likes.created_at IS 'When the like was created';

-- =========================================

-- Trigger: Update comment like_count on insert
CREATE OR REPLACE FUNCTION increment_comment_like_count()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE comments 
    SET like_count = like_count + 1 
    WHERE comment_id = NEW.comment_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_increment_comment_like_count
AFTER INSERT ON comment_likes
FOR EACH ROW
EXECUTE FUNCTION increment_comment_like_count();

-- Trigger: Update comment like_count on delete
CREATE OR REPLACE FUNCTION decrement_comment_like_count()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE comments 
    SET like_count = like_count - 1 
    WHERE comment_id = OLD.comment_id;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_decrement_comment_like_count
AFTER DELETE ON comment_likes
FOR EACH ROW
EXECUTE FUNCTION decrement_comment_like_count();

-- =========================================

-- Add like_count column to stories table for efficient querying
ALTER TABLE stories ADD COLUMN IF NOT EXISTS like_count INTEGER NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS idx_stories_like_count ON stories(like_count DESC);

COMMENT ON COLUMN stories.like_count IS 'Cached count of likes on this story';

-- Trigger: Update story like_count on insert
CREATE OR REPLACE FUNCTION increment_story_like_count()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE stories 
    SET like_count = like_count + 1 
    WHERE story_id = NEW.story_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_increment_story_like_count
AFTER INSERT ON story_likes
FOR EACH ROW
EXECUTE FUNCTION increment_story_like_count();

-- Trigger: Update story like_count on delete
CREATE OR REPLACE FUNCTION decrement_story_like_count()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE stories 
    SET like_count = like_count - 1 
    WHERE story_id = OLD.story_id;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_decrement_story_like_count
AFTER DELETE ON story_likes
FOR EACH ROW
EXECUTE FUNCTION decrement_story_like_count();

-- =========================================

-- Add comment_count column to stories table for efficient querying
ALTER TABLE stories ADD COLUMN IF NOT EXISTS comment_count INTEGER NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS idx_stories_comment_count ON stories(comment_count DESC);

COMMENT ON COLUMN stories.comment_count IS 'Cached count of comments on this story';

-- Trigger: Update story comment_count on insert
CREATE OR REPLACE FUNCTION increment_story_comment_count()
RETURNS TRIGGER AS $$
BEGIN
    -- Only count top-level comments (not replies)
    IF NEW.parent_comment_id IS NULL THEN
        UPDATE stories 
        SET comment_count = comment_count + 1 
        WHERE story_id = NEW.story_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_increment_story_comment_count
AFTER INSERT ON comments
FOR EACH ROW
EXECUTE FUNCTION increment_story_comment_count();

-- Trigger: Update story comment_count on delete
CREATE OR REPLACE FUNCTION decrement_story_comment_count()
RETURNS TRIGGER AS $$
BEGIN
    -- Only count top-level comments (not replies)
    IF OLD.parent_comment_id IS NULL THEN
        UPDATE stories 
        SET comment_count = comment_count - 1 
        WHERE story_id = OLD.story_id;
    END IF;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_decrement_story_comment_count
AFTER DELETE ON comments
FOR EACH ROW
EXECUTE FUNCTION decrement_story_comment_count();

-- =========================================

-- Grant permissions (adjust role name as needed)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON story_likes TO your_app_role;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON comments TO your_app_role;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON comment_likes TO your_app_role;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO your_app_role;

-- =========================================
-- End of migration
-- =========================================
