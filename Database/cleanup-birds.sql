-- =============================================
-- Clean up birds with short names or empty descriptions
-- =============================================

BEGIN;

-- First, show what will be deleted
SELECT
    name,
    species,
    LENGTH(name) as name_length,
    LENGTH(COALESCE(description, '')) as desc_length,
    description
FROM birds
WHERE
    LENGTH(name) <= 3
    OR LENGTH(COALESCE(description, '')) <= 50
ORDER BY name_length, desc_length;

-- Delete birds with:
-- 1. Names that are 2-3 characters long
-- 2. Descriptions that are empty or very short (50 characters or less)
DELETE FROM birds
WHERE
    LENGTH(name) <= 3
    OR LENGTH(COALESCE(description, '')) <= 50;

COMMIT;

-- Show remaining birds
SELECT
    name,
    species,
    LENGTH(name) as name_length,
    LENGTH(description) as desc_length
FROM birds
ORDER BY name;
