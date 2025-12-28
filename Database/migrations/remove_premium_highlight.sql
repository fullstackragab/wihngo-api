-- Remove Premium and Highlight Features
-- "All birds are equal" - Wihngo Core Principle
-- This migration removes premium subscriptions and story highlighting

BEGIN;

-- Step 1: Drop dependent tables first (foreign key constraints)
DROP TABLE IF EXISTS bird_premium_subscriptions CASCADE;
DROP TABLE IF EXISTS premium_styles CASCADE;

-- Step 2: Remove premium columns from birds table
ALTER TABLE birds DROP COLUMN IF EXISTS is_premium;
ALTER TABLE birds DROP COLUMN IF EXISTS premium_style_json;
ALTER TABLE birds DROP COLUMN IF EXISTS premium_expires_at;
ALTER TABLE birds DROP COLUMN IF EXISTS premium_plan;

-- Step 3: Remove highlight columns from stories table
ALTER TABLE stories DROP COLUMN IF EXISTS is_highlighted;
ALTER TABLE stories DROP COLUMN IF EXISTS highlight_order;

-- Step 4: Add a comment to document the change
COMMENT ON TABLE birds IS 'Birds table - All birds are equal. No premium or highlighted birds.';
COMMENT ON TABLE stories IS 'Stories table - All stories are equal. No highlighted stories.';

COMMIT;

-- Verify the changes
DO $$
BEGIN
    RAISE NOTICE 'Premium and highlight features removed successfully.';
    RAISE NOTICE 'Wihngo Core Principle: All birds are equal.';
END $$;
