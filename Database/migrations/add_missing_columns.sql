-- Migration: Add missing columns to stories table
-- This fixes the error: column s.highlight_order does not exist
-- Run this against your PostgreSQL database

BEGIN;

-- Add missing columns to stories table
ALTER TABLE public.stories 
ADD COLUMN IF NOT EXISTS is_highlighted BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS highlight_order INTEGER;

-- Add index on stories.is_highlighted for performance when querying highlighted stories
CREATE INDEX IF NOT EXISTS idx_stories_highlighted 
ON public.stories (is_highlighted, highlight_order) 
WHERE is_highlighted = TRUE;

-- Grant permissions (adjust if needed based on your user)
ALTER TABLE IF EXISTS public.stories OWNER to wingo;

COMMIT;
