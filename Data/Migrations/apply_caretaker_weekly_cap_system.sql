-- Migration: 20250107000000_AddCaretakerWeeklyCapSystem
-- Adds caretaker weekly cap system and love videos feature

BEGIN;

-- =========================================
-- 0. Create EF Migrations History table if not exists
-- =========================================
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- =========================================
-- 1. Add weekly_cap column to users table
-- =========================================
ALTER TABLE users
ADD COLUMN IF NOT EXISTS weekly_cap numeric(18,6) NOT NULL DEFAULT 5.00;

-- =========================================
-- 2. Create caretaker_support_receipts table
-- =========================================
CREATE TABLE IF NOT EXISTS caretaker_support_receipts (
    id uuid NOT NULL,
    caretaker_user_id uuid NOT NULL,
    supporter_user_id uuid NOT NULL,
    bird_id uuid NULL,
    tx_signature character varying(128) NOT NULL,
    amount numeric(18,6) NOT NULL,
    transaction_type character varying(20) NOT NULL,
    week_id character varying(10) NOT NULL,
    support_intent_id uuid NULL,
    verified_on_chain boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_caretaker_support_receipts" PRIMARY KEY (id),
    CONSTRAINT "FK_caretaker_support_receipts_users_caretaker_user_id" FOREIGN KEY (caretaker_user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT "FK_caretaker_support_receipts_users_supporter_user_id" FOREIGN KEY (supporter_user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT "FK_caretaker_support_receipts_birds_bird_id" FOREIGN KEY (bird_id) REFERENCES birds(bird_id) ON DELETE SET NULL,
    CONSTRAINT "FK_caretaker_support_receipts_support_intents_support_intent_id" FOREIGN KEY (support_intent_id) REFERENCES support_intents(id) ON DELETE SET NULL
);

-- Indexes for caretaker_support_receipts
CREATE INDEX IF NOT EXISTS ix_caretaker_support_receipts_caretaker_user_id ON caretaker_support_receipts(caretaker_user_id);
CREATE INDEX IF NOT EXISTS ix_caretaker_support_receipts_supporter_user_id ON caretaker_support_receipts(supporter_user_id);
CREATE INDEX IF NOT EXISTS ix_caretaker_support_receipts_week_id ON caretaker_support_receipts(week_id);
CREATE INDEX IF NOT EXISTS ix_caretaker_support_receipts_weekly_baseline ON caretaker_support_receipts(caretaker_user_id, week_id, transaction_type);
CREATE UNIQUE INDEX IF NOT EXISTS ix_caretaker_support_receipts_tx_signature_unique ON caretaker_support_receipts(tx_signature);
CREATE INDEX IF NOT EXISTS ix_caretaker_support_receipts_bird_id ON caretaker_support_receipts(bird_id);
CREATE INDEX IF NOT EXISTS ix_caretaker_support_receipts_created_at ON caretaker_support_receipts(created_at);

-- =========================================
-- 3. Create love_videos table
-- =========================================
CREATE TABLE IF NOT EXISTS love_videos (
    id uuid NOT NULL,
    youtube_url character varying(500) NULL,
    youtube_video_id character varying(20) NULL,
    description character varying(500) NULL,
    category character varying(50) NOT NULL,
    status character varying(20) NOT NULL,
    submitted_by_user_id uuid NOT NULL,
    rejection_reason character varying(500) NULL,
    moderated_by_user_id uuid NULL,
    created_at timestamp with time zone NOT NULL,
    approved_at timestamp with time zone NULL,
    rejected_at timestamp with time zone NULL,
    updated_at timestamp with time zone NOT NULL,
    media_key character varying(500) NULL,
    media_url character varying(500) NULL,
    media_type character varying(20) NULL,
    CONSTRAINT "PK_love_videos" PRIMARY KEY (id),
    CONSTRAINT "FK_love_videos_users_submitted_by_user_id" FOREIGN KEY (submitted_by_user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT "FK_love_videos_users_moderated_by_user_id" FOREIGN KEY (moderated_by_user_id) REFERENCES users(user_id) ON DELETE SET NULL
);

-- Indexes for love_videos
CREATE INDEX IF NOT EXISTS ix_love_videos_status ON love_videos(status);
CREATE INDEX IF NOT EXISTS ix_love_videos_category ON love_videos(category);
CREATE INDEX IF NOT EXISTS ix_love_videos_created_at ON love_videos(created_at);
CREATE INDEX IF NOT EXISTS ix_love_videos_youtube_video_id ON love_videos(youtube_video_id);
CREATE INDEX IF NOT EXISTS ix_love_videos_submitted_by_user_id ON love_videos(submitted_by_user_id);

-- =========================================
-- 4. Record migration in EF history table
-- =========================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20250107000000_AddCaretakerWeeklyCapSystem', '8.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20250107000000_AddCaretakerWeeklyCapSystem'
);

COMMIT;
