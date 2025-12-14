-- Cleanup script for birds with NULL owner_id
-- This should not happen as owner_id is a required field, but may occur due to data corruption

-- Option 1: Delete birds with NULL owner_id (RECOMMENDED if they're orphaned data)
-- DELETE FROM birds WHERE owner_id IS NULL;

-- Option 2: Check if there are any birds with NULL owner_id first
SELECT 
    bird_id,
    name,
    species,
    created_at,
    owner_id
FROM birds 
WHERE owner_id IS NULL;

-- Option 3: If you want to assign orphaned birds to a default admin user
-- UPDATE birds 
-- SET owner_id = '<your-admin-user-guid-here>'
-- WHERE owner_id IS NULL;

-- Check for birds with invalid owner_id (not in users table)
SELECT 
    b.bird_id,
    b.name,
    b.species,
    b.owner_id,
    b.created_at
FROM birds b
LEFT JOIN users u ON b.owner_id = u.user_id
WHERE u.user_id IS NULL;

-- Option 4: Delete birds with invalid owner_id references
-- DELETE FROM birds 
-- WHERE bird_id IN (
--     SELECT b.bird_id
--     FROM birds b
--     LEFT JOIN users u ON b.owner_id = u.user_id
--     WHERE u.user_id IS NULL
-- );
