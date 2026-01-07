-- Migration: Add AI moderation fields to love_videos table
-- This enables AI-powered automatic moderation of submissions

BEGIN;

-- =========================================
-- Add AI moderation columns to love_videos
-- =========================================

-- AI decision: auto_approve, needs_human_review, reject
ALTER TABLE love_videos
ADD COLUMN IF NOT EXISTS ai_decision character varying(30) NULL;

-- AI confidence score (0.0 to 1.0)
ALTER TABLE love_videos
ADD COLUMN IF NOT EXISTS ai_confidence double precision NULL;

-- AI-generated flags (JSON array)
ALTER TABLE love_videos
ADD COLUMN IF NOT EXISTS ai_flags character varying(500) NULL;

-- AI-generated reasons (JSON array)
ALTER TABLE love_videos
ADD COLUMN IF NOT EXISTS ai_reasons character varying(1000) NULL;

-- When AI moderation was performed
ALTER TABLE love_videos
ADD COLUMN IF NOT EXISTS ai_moderated_at timestamp with time zone NULL;

-- =========================================
-- Add indexes for AI moderation queries
-- =========================================

-- Index on AI decision for filtering
CREATE INDEX IF NOT EXISTS ix_love_videos_ai_decision ON love_videos(ai_decision);

-- Index for finding items needing human review
CREATE INDEX IF NOT EXISTS ix_love_videos_ai_pending_review
ON love_videos(ai_decision, status)
WHERE ai_decision = 'needs_human_review' AND status = 'pending';

COMMIT;
