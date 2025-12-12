-- This script manually marks migrations as applied in the database
-- Run this ONLY if your database already has the tables/columns from these migrations

-- Check current migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";

-- Mark pending migrations as applied (if the changes already exist in your database)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('20251210001226_AddDonationCentsToBirds', '8.0.14'),
    ('20251210005314_AddBirdPremiumColumns', '8.0.14'),
    ('20251210010101_AddStoryHighlightFields', '8.0.14'),
    ('20251211121022_AddPremiumAndCharityEntities', '8.0.14'),
    ('20251211162608_AddSepoliaWallet', '8.0.14')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verify
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
