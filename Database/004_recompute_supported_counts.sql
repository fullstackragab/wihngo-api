-- 004_recompute_supported_counts.sql
-- Recompute supported_count on birds from support_transactions and ensure data consistency
-- Run: psql -h <HOST> -U <USER> -d wihngo -f Database/004_recompute_supported_counts.sql

BEGIN;

-- Set supported_count to 0 where null
UPDATE birds SET supported_count = 0 WHERE supported_count IS NULL;

-- Recompute supported_count using support_transactions
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
);

COMMIT;
