-- Migration: Add bird activity tracking
-- Purpose: Track last activity date for birds to determine support availability
-- Date: 2025-12-16

-- Add last_activity_at column to birds table
ALTER TABLE birds
ADD COLUMN IF NOT EXISTS last_activity_at TIMESTAMP WITH TIME ZONE;

-- Initialize last_activity_at with the most recent activity date
-- Priority: most recent story creation > bird creation date
UPDATE birds b
SET last_activity_at = COALESCE(
    (SELECT MAX(s.created_at) FROM stories s WHERE s.bird_id = b.bird_id),
    b.created_at
)
WHERE b.last_activity_at IS NULL;

-- Add index for querying birds by activity status
CREATE INDEX IF NOT EXISTS idx_birds_last_activity_at ON birds(last_activity_at);

-- Comment on column
COMMENT ON COLUMN birds.last_activity_at IS 'Timestamp of last activity (story post, profile update). Used to determine support availability.';
