-- 005_ensure_counters_and_recompute.sql
-- Ensure bird counter columns exist and recompute supported_count from support_transactions
-- Run: psql -h <HOST> -U <USER> -d wihngo -f Database/005_ensure_counters_and_recompute.sql

BEGIN;

-- Add columns if they don't exist
ALTER TABLE birds
  ADD COLUMN IF NOT EXISTS loved_count INT NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS supported_count INT NOT NULL DEFAULT 0;

-- Recompute supported_count from support_transactions
UPDATE birds b
SET supported_count = COALESCE(sub.cnt, 0)
FROM (
  SELECT bird_id, COUNT(*) AS cnt
  FROM support_transactions
  GROUP BY bird_id
) AS sub
WHERE b.bird_id = sub.bird_id;

-- Set supported_count to 0 for birds with no transactions
UPDATE birds b
SET supported_count = 0
WHERE NOT EXISTS (
  SELECT 1 FROM support_transactions st WHERE st.bird_id = b.bird_id
) AND b.supported_count IS DISTINCT FROM 0;

COMMIT;
