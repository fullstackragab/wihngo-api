-- =============================================
-- Wihngo Notifications System - Complete Migration
-- Execute this entire script once to set up the notifications system
-- =============================================

BEGIN;

-- =============================================
-- STEP 1: Create Tables
-- =============================================

-- Create notifications table
CREATE TABLE IF NOT EXISTS notifications (
    notification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    priority INTEGER NOT NULL DEFAULT 1,
    channels INTEGER NOT NULL DEFAULT 1,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMP,
    deep_link VARCHAR(500),
    bird_id UUID,
    story_id UUID,
    transaction_id UUID,
    actor_user_id UUID,
    group_id UUID,
    group_count INTEGER NOT NULL DEFAULT 1,
    push_sent BOOLEAN NOT NULL DEFAULT FALSE,
    email_sent BOOLEAN NOT NULL DEFAULT FALSE,
    sms_sent BOOLEAN NOT NULL DEFAULT FALSE,
    push_sent_at TIMESTAMP,
    email_sent_at TIMESTAMP,
    sms_sent_at TIMESTAMP,
    metadata VARCHAR(2000),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_notifications_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

-- Create notification_preferences table
CREATE TABLE IF NOT EXISTS notification_preferences (
    preference_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    notification_type VARCHAR(50) NOT NULL,
    in_app_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    push_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    email_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    sms_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_notification_preferences_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT uq_notification_preferences UNIQUE (user_id, notification_type)
);

-- Create notification_settings table
CREATE TABLE IF NOT EXISTS notification_settings (
    settings_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    quiet_hours_start TIME NOT NULL DEFAULT '22:00:00',
    quiet_hours_end TIME NOT NULL DEFAULT '08:00:00',
    quiet_hours_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    max_push_per_day INTEGER NOT NULL DEFAULT 5,
    max_email_per_day INTEGER NOT NULL DEFAULT 2,
    enable_notification_grouping BOOLEAN NOT NULL DEFAULT TRUE,
    grouping_window_minutes INTEGER NOT NULL DEFAULT 60,
    enable_daily_digest BOOLEAN NOT NULL DEFAULT FALSE,
    daily_digest_time TIME NOT NULL DEFAULT '09:00:00',
    time_zone VARCHAR(100) DEFAULT 'UTC',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_notification_settings_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT uq_notification_settings_user UNIQUE (user_id)
);

-- Create user_devices table for push notifications
CREATE TABLE IF NOT EXISTS user_devices (
    device_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    push_token VARCHAR(500) NOT NULL,
    device_type VARCHAR(50),
    device_name VARCHAR(200),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_used_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_devices_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT uq_user_devices_push_token UNIQUE (push_token)
);

-- =============================================
-- STEP 2: Create Indexes for Performance
-- =============================================

-- Notifications indexes
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_user_is_read ON notifications(user_id, is_read);
CREATE INDEX IF NOT EXISTS idx_notifications_group_id ON notifications(group_id);
CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON notifications(created_at);
CREATE INDEX IF NOT EXISTS idx_notifications_bird_id ON notifications(bird_id);
CREATE INDEX IF NOT EXISTS idx_notifications_story_id ON notifications(story_id);
CREATE INDEX IF NOT EXISTS idx_notifications_type ON notifications(type);
CREATE INDEX IF NOT EXISTS idx_notifications_priority ON notifications(priority);

-- Notification preferences indexes
CREATE INDEX IF NOT EXISTS idx_notification_preferences_user_id ON notification_preferences(user_id);
CREATE INDEX IF NOT EXISTS idx_notification_preferences_type ON notification_preferences(notification_type);

-- Notification settings indexes
CREATE INDEX IF NOT EXISTS idx_notification_settings_user_id ON notification_settings(user_id);
CREATE INDEX IF NOT EXISTS idx_notification_settings_digest ON notification_settings(enable_daily_digest);

-- User devices indexes
CREATE INDEX IF NOT EXISTS idx_user_devices_user_id ON user_devices(user_id);
CREATE INDEX IF NOT EXISTS idx_user_devices_is_active ON user_devices(user_id, is_active);
CREATE INDEX IF NOT EXISTS idx_user_devices_last_used ON user_devices(last_used_at);

-- =============================================
-- STEP 3: Initialize Default Data for Existing Users
-- =============================================

DO $$
DECLARE
    user_record RECORD;
    notification_types TEXT[] := ARRAY[
        'BirdLoved', 
        'BirdSupported', 
        'CommentAdded', 
        'NewStory', 
        'HealthUpdate', 
        'BirdMemorial', 
        'NewFollower', 
        'MilestoneAchieved',
        'BirdFeatured', 
        'PremiumExpiring', 
        'PaymentReceived', 
        'SecurityAlert',
        'SuggestedBirds', 
        'ReEngagement'
    ];
    n_type TEXT;
    high_priority_email_types TEXT[] := ARRAY[
        'BirdSupported', 
        'PaymentReceived', 
        'SecurityAlert', 
        'PremiumExpiring', 
        'BirdMemorial'
    ];
    user_count INTEGER := 0;
    pref_count INTEGER := 0;
    settings_count INTEGER := 0;
BEGIN
    -- Count existing users
    SELECT COUNT(*) INTO user_count FROM users;
    
    RAISE NOTICE 'Found % existing users. Initializing notification preferences...', user_count;
    
    -- For each existing user
    FOR user_record IN SELECT user_id FROM users LOOP
        -- For each notification type, create preference
        FOREACH n_type IN ARRAY notification_types LOOP
            INSERT INTO notification_preferences (
                preference_id,
                user_id,
                notification_type,
                in_app_enabled,
                push_enabled,
                email_enabled,
                sms_enabled,
                created_at,
                updated_at
            )
            VALUES (
                gen_random_uuid(),
                user_record.user_id,
                n_type,
                TRUE,
                TRUE,
                CASE 
                    WHEN n_type = ANY(high_priority_email_types) 
                    THEN TRUE 
                    ELSE FALSE 
                END,
                FALSE,
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            )
            ON CONFLICT (user_id, notification_type) DO NOTHING;
            
            pref_count := pref_count + 1;
        END LOOP;
        
        -- Create default notification settings for user
        INSERT INTO notification_settings (
            settings_id,
            user_id,
            quiet_hours_start,
            quiet_hours_end,
            quiet_hours_enabled,
            max_push_per_day,
            max_email_per_day,
            enable_notification_grouping,
            grouping_window_minutes,
            enable_daily_digest,
            daily_digest_time,
            time_zone,
            created_at,
            updated_at
        )
        VALUES (
            gen_random_uuid(),
            user_record.user_id,
            '22:00:00',
            '08:00:00',
            TRUE,
            5,
            2,
            TRUE,
            60,
            FALSE,
            '09:00:00',
            'UTC',
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        )
        ON CONFLICT (user_id) DO NOTHING;
        
        settings_count := settings_count + 1;
    END LOOP;
    
    RAISE NOTICE 'Created % notification preferences for % users', pref_count, user_count;
    RAISE NOTICE 'Created % notification settings records', settings_count;
END $$;

-- =============================================
-- STEP 4: Create Triggers for Updated_At Timestamps
-- =============================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger for notifications table
DROP TRIGGER IF EXISTS trigger_notifications_updated_at ON notifications;
CREATE TRIGGER trigger_notifications_updated_at
    BEFORE UPDATE ON notifications
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger for notification_preferences table
DROP TRIGGER IF EXISTS trigger_notification_preferences_updated_at ON notification_preferences;
CREATE TRIGGER trigger_notification_preferences_updated_at
    BEFORE UPDATE ON notification_preferences
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger for notification_settings table
DROP TRIGGER IF EXISTS trigger_notification_settings_updated_at ON notification_settings;
CREATE TRIGGER trigger_notification_settings_updated_at
    BEFORE UPDATE ON notification_settings
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- STEP 5: Verification Queries
-- =============================================

DO $$
DECLARE
    table_record RECORD;
    total_notifications INTEGER;
    total_preferences INTEGER;
    total_settings INTEGER;
    total_devices INTEGER;
    index_count INTEGER;
BEGIN
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'VERIFICATION RESULTS';
    RAISE NOTICE '==============================================';
    
    -- Check table row counts
    SELECT COUNT(*) INTO total_notifications FROM notifications;
    SELECT COUNT(*) INTO total_preferences FROM notification_preferences;
    SELECT COUNT(*) INTO total_settings FROM notification_settings;
    SELECT COUNT(*) INTO total_devices FROM user_devices;
    
    RAISE NOTICE 'Table: notifications - Rows: %', total_notifications;
    RAISE NOTICE 'Table: notification_preferences - Rows: %', total_preferences;
    RAISE NOTICE 'Table: notification_settings - Rows: %', total_settings;
    RAISE NOTICE 'Table: user_devices - Rows: %', total_devices;
    RAISE NOTICE '';
    
    -- Check indexes
    SELECT COUNT(*) INTO index_count 
    FROM pg_indexes 
    WHERE tablename IN ('notifications', 'notification_preferences', 'notification_settings', 'user_devices');
    
    RAISE NOTICE 'Total indexes created: %', index_count;
    RAISE NOTICE '';
    
    -- List all indexes
    RAISE NOTICE 'Indexes created:';
    FOR table_record IN 
        SELECT tablename, indexname 
        FROM pg_indexes 
        WHERE tablename IN ('notifications', 'notification_preferences', 'notification_settings', 'user_devices')
        ORDER BY tablename, indexname
    LOOP
        RAISE NOTICE '  - %.%', table_record.tablename, table_record.indexname;
    END LOOP;
    
    RAISE NOTICE '';
    RAISE NOTICE '==============================================';
    RAISE NOTICE 'MIGRATION COMPLETED SUCCESSFULLY!';
    RAISE NOTICE '==============================================';
    RAISE NOTICE '';
    RAISE NOTICE 'Next steps:';
    RAISE NOTICE '  1. Restart your .NET application';
    RAISE NOTICE '  2. Check Hangfire dashboard at /hangfire';
    RAISE NOTICE '  3. Test notification endpoints:';
    RAISE NOTICE '     - GET /api/notifications';
    RAISE NOTICE '     - POST /api/notifications/test';
    RAISE NOTICE '     - POST /api/notifications/devices';
    RAISE NOTICE '';
END $$;

COMMIT;

-- =============================================
-- OPTIONAL: Sample Queries for Testing
-- =============================================

-- Uncomment these to run verification queries after migration:

-- -- View all tables and row counts
-- SELECT 'notifications' as table_name, COUNT(*) as row_count FROM notifications
-- UNION ALL
-- SELECT 'notification_preferences', COUNT(*) FROM notification_preferences
-- UNION ALL
-- SELECT 'notification_settings', COUNT(*) FROM notification_settings
-- UNION ALL
-- SELECT 'user_devices', COUNT(*) FROM user_devices;

-- -- View sample notification preferences
-- SELECT 
--     u.name as user_name,
--     np.notification_type,
--     np.push_enabled,
--     np.email_enabled,
--     np.in_app_enabled
-- FROM notification_preferences np
-- JOIN users u ON u.user_id = np.user_id
-- ORDER BY u.name, np.notification_type
-- LIMIT 20;

-- -- View all notification settings
-- SELECT 
--     u.name as user_name,
--     ns.quiet_hours_enabled,
--     ns.max_push_per_day,
--     ns.max_email_per_day,
--     ns.enable_daily_digest,
--     ns.time_zone
-- FROM notification_settings ns
-- JOIN users u ON u.user_id = ns.user_id
-- ORDER BY u.name;

-- -- Check all indexes
-- SELECT 
--     schemaname,
--     tablename,
--     indexname,
--     indexdef
-- FROM pg_indexes
-- WHERE tablename IN ('notifications', 'notification_preferences', 'notification_settings', 'user_devices')
-- ORDER BY tablename, indexname;
