-- Memorial Bird Feature Migration
-- This migration adds support for marking birds as deceased (memorial) and handling remaining funds

-- ============================================================================
-- 1. Add Memorial Columns to Birds Table
-- ============================================================================

-- Add memorial status columns
ALTER TABLE birds ADD COLUMN is_memorial BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE birds ADD COLUMN memorial_date TIMESTAMP NULL;
ALTER TABLE birds ADD COLUMN memorial_reason VARCHAR(500) NULL;
ALTER TABLE birds ADD COLUMN funds_redirection_choice VARCHAR(50) NULL;

-- Add index for querying memorial birds
CREATE INDEX idx_birds_is_memorial ON birds(is_memorial);

-- Add comment for documentation
COMMENT ON COLUMN birds.is_memorial IS 'Indicates if the bird has passed away';
COMMENT ON COLUMN birds.memorial_date IS 'Date when the bird passed away';
COMMENT ON COLUMN birds.memorial_reason IS 'Optional reason/message about passing (e.g., "Passed away peacefully")';
COMMENT ON COLUMN birds.funds_redirection_choice IS 'Owner choice for remaining funds: emergency_fund, owner_keeps, charity';

-- ============================================================================
-- 2. Create Memorial Messages Table
-- ============================================================================

CREATE TABLE memorial_messages (
    message_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL,
    user_id UUID NOT NULL,
    message VARCHAR(500) NOT NULL,
    is_approved BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_memorial_messages_birds FOREIGN KEY (bird_id) 
        REFERENCES birds(bird_id) ON DELETE CASCADE,
    CONSTRAINT fk_memorial_messages_users FOREIGN KEY (user_id) 
        REFERENCES users(user_id) ON DELETE CASCADE
);

-- Indexes for better query performance
CREATE INDEX idx_memorial_messages_bird_id ON memorial_messages(bird_id);
CREATE INDEX idx_memorial_messages_created_at ON memorial_messages(created_at DESC);
CREATE INDEX idx_memorial_messages_user_id ON memorial_messages(user_id);

-- Comments
COMMENT ON TABLE memorial_messages IS 'Condolence and tribute messages for memorial birds';
COMMENT ON COLUMN memorial_messages.is_approved IS 'Whether message is approved for display (for moderation)';

-- ============================================================================
-- 3. Create Memorial Fund Redirections Table
-- ============================================================================

CREATE TABLE memorial_fund_redirections (
    redirection_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bird_id UUID NOT NULL,
    previous_owner_id UUID NOT NULL,
    remaining_balance DECIMAL(18, 2) NOT NULL,
    redirection_type VARCHAR(50) NOT NULL,
    charity_name VARCHAR(255) NULL,
    processed_at TIMESTAMP NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    transaction_id VARCHAR(255) NULL,
    notes TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_memorial_fund_redirections_birds FOREIGN KEY (bird_id) 
        REFERENCES birds(bird_id) ON DELETE CASCADE,
    CONSTRAINT fk_memorial_fund_redirections_users FOREIGN KEY (previous_owner_id) 
        REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT chk_redirection_type CHECK (redirection_type IN ('emergency_fund', 'owner_keeps', 'charity')),
    CONSTRAINT chk_status CHECK (status IN ('pending', 'processing', 'completed', 'failed'))
);

-- Indexes
CREATE INDEX idx_memorial_fund_redirections_bird_id ON memorial_fund_redirections(bird_id);
CREATE INDEX idx_memorial_fund_redirections_status ON memorial_fund_redirections(status);
CREATE INDEX idx_memorial_fund_redirections_owner ON memorial_fund_redirections(previous_owner_id);

-- Comments
COMMENT ON TABLE memorial_fund_redirections IS 'Tracks how remaining funds are redirected when a bird passes away';
COMMENT ON COLUMN memorial_fund_redirections.redirection_type IS 'emergency_fund, owner_keeps, or charity';
COMMENT ON COLUMN memorial_fund_redirections.status IS 'pending, processing, completed, or failed';

-- ============================================================================
-- 4. Create Function to Update Memorial Messages timestamp
-- ============================================================================

CREATE OR REPLACE FUNCTION update_memorial_message_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_memorial_messages_updated_at
    BEFORE UPDATE ON memorial_messages
    FOR EACH ROW
    EXECUTE FUNCTION update_memorial_message_timestamp();

-- ============================================================================
-- 5. Create Function to Update Memorial Fund Redirections timestamp
-- ============================================================================

CREATE OR REPLACE FUNCTION update_memorial_fund_redirection_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_memorial_fund_redirections_updated_at
    BEFORE UPDATE ON memorial_fund_redirections
    FOR EACH ROW
    EXECUTE FUNCTION update_memorial_fund_redirection_timestamp();

-- ============================================================================
-- Migration Complete
-- ============================================================================

-- Verify tables exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'birds' AND column_name = 'is_memorial') THEN
        RAISE EXCEPTION 'Memorial columns not added to birds table';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'memorial_messages') THEN
        RAISE EXCEPTION 'memorial_messages table not created';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'memorial_fund_redirections') THEN
        RAISE EXCEPTION 'memorial_fund_redirections table not created';
    END IF;
    
    RAISE NOTICE 'Memorial bird migration completed successfully';
END $$;
