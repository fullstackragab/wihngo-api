-- Migration: Add location and age fields to birds table
-- Date: 2025-12-29

-- Add location column
ALTER TABLE birds
ADD COLUMN IF NOT EXISTS location VARCHAR(200);

-- Add age column
ALTER TABLE birds
ADD COLUMN IF NOT EXISTS age VARCHAR(100);

-- Comments for documentation
COMMENT ON COLUMN birds.location IS 'Location/habitat of the bird (e.g., California, USA)';
COMMENT ON COLUMN birds.age IS 'Age of the bird (e.g., 3 years, 6 months)';

-- Verify columns were added
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'birds' AND column_name IN ('location', 'age');
