-- =====================================================
-- STORY SINGLE BIRD MIGRATION
-- =====================================================
-- This migration converts the stories table from a many-to-many 
-- relationship with birds (via story_birds junction table) to a 
-- simple one-to-one relationship with a bird_id foreign key.
--
-- Also adds the 'mode' column for story mood/category.
--
-- WARNING: This migration will:
-- 1. Add bird_id column to stories table
-- 2. Add mode column to stories table (INT, nullable)
-- 3. Migrate data from story_birds (keeping only the first bird per story)
-- 4. Drop the story_birds junction table
-- 5. Stories with multiple birds will keep only the oldest bird association
--
-- BACKUP YOUR DATA BEFORE RUNNING THIS MIGRATION!
-- =====================================================

BEGIN;

-- Step 1: Add bird_id column to stories table (nullable initially)
ALTER TABLE stories 
ADD COLUMN IF NOT EXISTS bird_id UUID;

-- Step 2: Add mode column to stories table (for story mood/category)
-- mode is an integer that maps to StoryMode enum:
-- 0=LoveAndBond, 1=NewBeginning, 2=ProgressAndWins, 3=FunnyMoment,
-- 4=PeacefulMoment, 5=LossAndMemory, 6=CareAndHealth, 7=DailyLife
ALTER TABLE stories 
ADD COLUMN IF NOT EXISTS mode INT;

-- Step 3: Add video_url column if it doesn't exist (for stories with video instead of image)
ALTER TABLE stories 
ADD COLUMN IF NOT EXISTS video_url TEXT;

-- Step 4: Add highlight columns for premium bird feature
ALTER TABLE stories 
ADD COLUMN IF NOT EXISTS is_highlighted BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE stories 
ADD COLUMN IF NOT EXISTS highlight_order INT;

-- Step 5: Migrate data from story_birds to stories.bird_id
-- For each story, pick the bird with the earliest created_at from story_birds
UPDATE stories s
SET bird_id = (
    SELECT sb.bird_id
    FROM story_birds sb
    WHERE sb.story_id = s.story_id
    ORDER BY sb.created_at ASC
    LIMIT 1
)
WHERE EXISTS (SELECT 1 FROM story_birds WHERE story_id = s.story_id);

-- Step 6: Make bird_id NOT NULL (all stories should now have a bird_id)
-- First, check if any stories don't have a bird_id
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM stories WHERE bird_id IS NULL) THEN
        RAISE EXCEPTION 'Cannot proceed: Some stories do not have a bird_id. Please investigate.';
    END IF;
END $$;

ALTER TABLE stories 
ALTER COLUMN bird_id SET NOT NULL;

-- Step 7: Add foreign key constraint
ALTER TABLE stories
ADD CONSTRAINT fk_stories_bird 
    FOREIGN KEY (bird_id) 
    REFERENCES birds(bird_id) 
    ON DELETE CASCADE;

-- Step 8: Add index on bird_id for better query performance
CREATE INDEX IF NOT EXISTS idx_stories_bird_id ON stories(bird_id);

-- Step 9: Add index on mode for filtering stories by mood
CREATE INDEX IF NOT EXISTS idx_stories_mode ON stories(mode) WHERE mode IS NOT NULL;

-- Step 10: Add index on is_highlighted for premium features
CREATE INDEX IF NOT EXISTS idx_stories_highlighted ON stories(bird_id, is_highlighted, highlight_order) 
WHERE is_highlighted = TRUE;

-- Step 11: Drop the story_birds junction table (no longer needed)
DROP TABLE IF EXISTS story_birds CASCADE;

-- Step 12: Verify migration
DO $$
DECLARE
    story_count INT;
    stories_with_birds INT;
BEGIN
    SELECT COUNT(*) INTO story_count FROM stories;
    SELECT COUNT(*) INTO stories_with_birds FROM stories WHERE bird_id IS NOT NULL;
    
    RAISE NOTICE 'Migration completed successfully!';
    RAISE NOTICE 'Total stories: %', story_count;
    RAISE NOTICE 'Stories with birds: %', stories_with_birds;
    
    IF story_count != stories_with_birds THEN
        RAISE EXCEPTION 'Inconsistency detected: % stories without birds', (story_count - stories_with_birds);
    END IF;
END $$;

COMMIT;

-- =====================================================
-- ROLLBACK INSTRUCTIONS (if needed)
-- =====================================================
-- If you need to roll back this migration:
--
-- 1. Restore from backup (recommended)
-- OR
-- 2. Manually recreate story_birds table:
/*
BEGIN;

CREATE TABLE story_birds (
    story_bird_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    story_id UUID NOT NULL,
    bird_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_story_birds_story 
        FOREIGN KEY (story_id) 
        REFERENCES stories(story_id) 
        ON DELETE CASCADE,
    
    CONSTRAINT fk_story_birds_bird 
        FOREIGN KEY (bird_id) 
        REFERENCES birds(bird_id) 
        ON DELETE CASCADE,
    
    CONSTRAINT uq_story_birds_story_bird 
        UNIQUE (story_id, bird_id)
);

-- Migrate data back
INSERT INTO story_birds (story_id, bird_id, created_at)
SELECT story_id, bird_id, created_at
FROM stories
WHERE bird_id IS NOT NULL;

-- Remove columns from stories
ALTER TABLE stories DROP CONSTRAINT IF EXISTS fk_stories_bird;
ALTER TABLE stories DROP COLUMN IF EXISTS bird_id;
ALTER TABLE stories DROP COLUMN IF EXISTS mode;
ALTER TABLE stories DROP COLUMN IF EXISTS is_highlighted;
ALTER TABLE stories DROP COLUMN IF EXISTS highlight_order;

COMMIT;
*/
