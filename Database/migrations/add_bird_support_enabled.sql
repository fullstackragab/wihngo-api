-- Migration: Add support_enabled field to birds table
-- This allows bird owners to control whether their bird can receive support
-- Date: 2025-12-29

-- Add support_enabled column (defaults to true for existing birds)
ALTER TABLE birds
ADD COLUMN IF NOT EXISTS support_enabled BOOLEAN NOT NULL DEFAULT true;

-- Add index for filtering supportable birds
CREATE INDEX IF NOT EXISTS idx_birds_support_enabled
ON birds(support_enabled)
WHERE support_enabled = true;

-- Comment for documentation
COMMENT ON COLUMN birds.support_enabled IS 'Whether this bird is currently accepting support. Controlled by bird owner.';

-- Verify the column was added
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'birds' AND column_name = 'support_enabled';
