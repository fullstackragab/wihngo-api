-- =============================================
-- Clean up stories with short or empty content
-- =============================================

BEGIN;

-- Show stories that will be deleted
SELECT
    story_id,
    LEFT(content, 50) as content_preview,
    LENGTH(COALESCE(content, '')) as content_len,
    created_at
FROM stories
WHERE LENGTH(COALESCE(content, '')) <= 50
ORDER BY content_len;

-- Delete stories with content 50 characters or less
DELETE FROM stories
WHERE LENGTH(COALESCE(content, '')) <= 50;

COMMIT;

-- Show statistics of remaining stories
SELECT
    COUNT(*) as total_stories,
    MIN(LENGTH(content)) as min_content_len,
    MAX(LENGTH(content)) as max_content_len,
    AVG(LENGTH(content))::int as avg_content_len
FROM stories;
