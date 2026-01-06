-- Migration: Add needs_support field to birds and weekly support tracking
-- This enables the "birds need support" feature with 3 rounds per week

-- 1. Add needs_support flag to birds table (owner opt-in)
ALTER TABLE birds ADD COLUMN IF NOT EXISTS needs_support BOOLEAN NOT NULL DEFAULT false;

-- 2. Create table to track weekly support rounds per bird
-- Each bird can receive support up to 3 times per week
CREATE TABLE IF NOT EXISTS weekly_bird_support_rounds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
    week_start_date DATE NOT NULL,
    times_supported INTEGER NOT NULL DEFAULT 0,
    last_supported_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,

    -- One record per bird per week
    CONSTRAINT uq_bird_week UNIQUE (bird_id, week_start_date)
);

-- Index for efficient weekly queries
CREATE INDEX IF NOT EXISTS idx_weekly_support_week ON weekly_bird_support_rounds(week_start_date);
CREATE INDEX IF NOT EXISTS idx_weekly_support_bird ON weekly_bird_support_rounds(bird_id);

-- Comment explaining the feature
COMMENT ON TABLE weekly_bird_support_rounds IS
'Tracks how many times each bird has received support in a given week.
Birds can receive support up to 3 times per week (3 rounds).
Round 1: All birds needing support are shown
Round 2: After all birds supported once, show all again
Round 3: After all birds supported twice, show all again
After 3 rounds: Show thank you message, no birds displayed';

COMMENT ON COLUMN birds.needs_support IS
'When true, this bird appears in the "birds need support" list.
Set by the bird owner to indicate their bird needs community support.';
