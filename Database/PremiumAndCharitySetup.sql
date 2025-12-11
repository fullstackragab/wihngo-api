-- ============================================================================
-- Wihngo Premium Bird Profile & Charity Backend - Database Setup Script
-- ============================================================================
-- Description: Complete database setup for premium subscriptions and charity features
-- Database: PostgreSQL
-- Created: December 2024
-- Execute this script once on your production database
-- ============================================================================

-- Start transaction
BEGIN;

-- ============================================================================
-- SECTION 1: Fix Migrations History Table (if needed)
-- ============================================================================
-- This fixes the snake_case naming convention issue with EF Core migrations
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = '__EFMigrationsHistory' 
        AND column_name = 'MigrationId'
    ) THEN
        ALTER TABLE "__EFMigrationsHistory" 
            RENAME COLUMN "MigrationId" TO migration_id;
        
        ALTER TABLE "__EFMigrationsHistory" 
            RENAME COLUMN "ProductVersion" TO product_version;
        
        RAISE NOTICE 'Migration history table columns renamed to snake_case';
    END IF;
END $$;

-- ============================================================================
-- SECTION 2: Create Bird Premium Subscriptions Table
-- ============================================================================
-- Main subscription table that must exist before charity allocations
CREATE TABLE IF NOT EXISTS bird_premium_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL,
    owner_id UUID NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    plan VARCHAR(50) NOT NULL DEFAULT 'monthly',
    provider VARCHAR(50),
    provider_subscription_id VARCHAR(200),
    price_cents BIGINT NOT NULL DEFAULT 300,
    duration_days INTEGER NOT NULL DEFAULT 30,
    started_at TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    current_period_end TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    canceled_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    updated_at TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT fk_bird_premium_subscriptions_bird FOREIGN KEY (bird_id) 
        REFERENCES birds(bird_id) ON DELETE CASCADE,
    CONSTRAINT fk_bird_premium_subscriptions_owner FOREIGN KEY (owner_id) 
        REFERENCES users(user_id) ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_bird_premium_subscriptions_status 
    ON bird_premium_subscriptions(status);

CREATE INDEX IF NOT EXISTS idx_bird_premium_subscriptions_bird_id 
    ON bird_premium_subscriptions(bird_id);

CREATE INDEX IF NOT EXISTS idx_bird_premium_subscriptions_owner_id 
    ON bird_premium_subscriptions(owner_id);

CREATE INDEX IF NOT EXISTS idx_bird_premium_subscriptions_current_period_end 
    ON bird_premium_subscriptions(current_period_end);

CREATE INDEX IF NOT EXISTS idx_bird_premium_subscriptions_plan 
    ON bird_premium_subscriptions(plan);

DO $$ BEGIN RAISE NOTICE 'Bird premium subscriptions table created successfully'; END $$;

-- ============================================================================
-- SECTION 3: Create Premium Styles Table
-- ============================================================================
-- Stores premium customization options for bird profiles
CREATE TABLE IF NOT EXISTS premium_styles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL,
    frame_id VARCHAR(50),
    badge_id VARCHAR(50),
    highlight_color VARCHAR(7),
    theme_id VARCHAR(50),
    cover_image_url VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    updated_at TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT fk_premium_styles_bird FOREIGN KEY (bird_id) 
        REFERENCES birds(bird_id) ON DELETE CASCADE
);

-- Create unique constraint (one premium style per bird)
CREATE UNIQUE INDEX IF NOT EXISTS uq_premium_styles_bird_id 
    ON premium_styles(bird_id);

-- Create index for performance
CREATE INDEX IF NOT EXISTS idx_premium_styles_bird_id 
    ON premium_styles(bird_id);

DO $$ BEGIN RAISE NOTICE 'Premium styles table created successfully'; END $$;

-- ============================================================================
-- SECTION 4: Create Charity Allocations Table
-- ============================================================================
-- Tracks charity contributions from each subscription
CREATE TABLE IF NOT EXISTS charity_allocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID NOT NULL,
    charity_name VARCHAR(255) NOT NULL,
    percentage DECIMAL(5,2) NOT NULL CHECK (percentage >= 0 AND percentage <= 100),
    amount DECIMAL(10,2) NOT NULL CHECK (amount >= 0),
    allocated_at TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT fk_charity_allocations_subscription FOREIGN KEY (subscription_id) 
        REFERENCES bird_premium_subscriptions(id) ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_charity_allocations_subscription_id 
    ON charity_allocations(subscription_id);

CREATE INDEX IF NOT EXISTS idx_charity_allocations_allocated_at 
    ON charity_allocations(allocated_at);

CREATE INDEX IF NOT EXISTS idx_charity_allocations_charity_name 
    ON charity_allocations(charity_name);

DO $$ BEGIN RAISE NOTICE 'Charity allocations table created successfully'; END $$;

-- ============================================================================
-- SECTION 5: Create Charity Impact Stats Table
-- ============================================================================
-- Aggregated global charity impact statistics
CREATE TABLE IF NOT EXISTS charity_impact_stats (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    total_contributed DECIMAL(12,2) NOT NULL DEFAULT 0 CHECK (total_contributed >= 0),
    birds_helped INTEGER NOT NULL DEFAULT 0 CHECK (birds_helped >= 0),
    shelters_supported INTEGER NOT NULL DEFAULT 0 CHECK (shelters_supported >= 0),
    conservation_projects INTEGER NOT NULL DEFAULT 0 CHECK (conservation_projects >= 0),
    last_updated TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);

-- Create index for performance
CREATE INDEX IF NOT EXISTS idx_charity_impact_stats_last_updated 
    ON charity_impact_stats(last_updated DESC);

DO $$ BEGIN RAISE NOTICE 'Charity impact stats table created successfully'; END $$;

-- ============================================================================
-- SECTION 6: Initialize Charity Impact Stats
-- ============================================================================
-- Insert initial global statistics record
INSERT INTO charity_impact_stats (
    id, 
    total_contributed, 
    birds_helped, 
    shelters_supported, 
    conservation_projects, 
    last_updated
)
SELECT 
    gen_random_uuid(), 
    0, 
    0, 
    0, 
    0, 
    NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM charity_impact_stats);

DO $$ BEGIN RAISE NOTICE 'Initial charity impact stats record created'; END $$;

-- ============================================================================
-- SECTION 7: Verification Queries
-- ============================================================================
-- Verify all tables were created successfully
DO $$
DECLARE
    table_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO table_count
    FROM information_schema.tables 
    WHERE table_schema = 'public' 
      AND table_name IN (
          'premium_styles', 
          'charity_allocations', 
          'charity_impact_stats',
          'bird_premium_subscriptions'
      );
    
    IF table_count = 4 THEN
        RAISE NOTICE 'SUCCESS: All 4 required tables exist';
    ELSE
        RAISE WARNING 'WARNING: Only % out of 4 tables exist', table_count;
    END IF;
END $$;

-- ============================================================================
-- SECTION 8: Display Summary Information
-- ============================================================================
-- Show table statistics
SELECT 
    'bird_premium_subscriptions' AS table_name,
    COUNT(*) AS record_count
FROM bird_premium_subscriptions
UNION ALL
SELECT 
    'premium_styles' AS table_name,
    COUNT(*) AS record_count
FROM premium_styles
UNION ALL
SELECT 
    'charity_allocations' AS table_name,
    COUNT(*) AS record_count
FROM charity_allocations
UNION ALL
SELECT 
    'charity_impact_stats' AS table_name,
    COUNT(*) AS record_count
FROM charity_impact_stats;

-- Commit transaction
COMMIT;

-- ============================================================================
-- EXECUTION COMPLETE
-- ============================================================================
-- Next Steps:
-- 1. Verify all tables were created by checking the output above
-- 2. Restart your Wihngo API application
-- 3. Test the new endpoints:
--    - GET /api/premium/plans
--    - GET /api/charity/partners
--    - GET /api/charity/impact/global
-- 4. Monitor Hangfire dashboard at /hangfire for background jobs
-- ============================================================================

-- Display final success message
DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Database setup completed successfully!';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Premium Bird Profile backend is ready';
    RAISE NOTICE 'Charity tracking features are enabled';
    RAISE NOTICE '========================================';
END $$;
