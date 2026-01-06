-- Add is_public column to birds table
-- Allows bird owners to hide/unpublish their birds from public listings

ALTER TABLE birds
ADD COLUMN IF NOT EXISTS is_public BOOLEAN NOT NULL DEFAULT TRUE;

-- Add index for filtering public birds efficiently
CREATE INDEX IF NOT EXISTS idx_birds_is_public ON birds(is_public) WHERE is_public = TRUE;

-- Comment
COMMENT ON COLUMN birds.is_public IS 'Whether this bird is visible to public. If false, bird is hidden/draft mode and only visible to owner.';
