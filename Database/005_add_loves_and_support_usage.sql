-- 005_add_loves_and_support_usage.sql
-- Add a table to record which users 'love' which birds and a support_usage table for owners to report how funds were used

BEGIN;

-- Loves table (many-to-many between users and birds, allows tracking when user loved a bird)
CREATE TABLE IF NOT EXISTS loves (
  user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, bird_id)
);

-- Support usage reports: owner reports how funds used for a support transaction (optional)
CREATE TABLE IF NOT EXISTS support_usage (
  usage_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  bird_id UUID NOT NULL REFERENCES birds(bird_id) ON DELETE CASCADE,
  reported_by UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  amount NUMERIC(12,2) NOT NULL CHECK (amount >= 0),
  description TEXT NOT NULL,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_loves_bird ON loves (bird_id);
CREATE INDEX IF NOT EXISTS idx_support_usage_bird ON support_usage (bird_id);

COMMIT;
