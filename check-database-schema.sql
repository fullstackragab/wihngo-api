-- Check if tables exist
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Check if specific columns exist
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'stories' 
ORDER BY ordinal_position;

SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'birds' 
AND column_name IN ('donation_cents', 'is_premium', 'premium_expires_at')
ORDER BY ordinal_position;

-- Check migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
