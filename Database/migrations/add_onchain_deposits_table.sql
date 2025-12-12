-- Migration: Add onchain_deposits table for detecting USDC/EURC deposits across chains
-- Date: 2025-01-20
-- Description: Tracks on-chain deposits from Ethereum, Polygon, Base, Solana, and Stellar

-- Create onchain_deposits table
CREATE TABLE IF NOT EXISTS onchain_deposits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    chain VARCHAR(20) NOT NULL,
    token VARCHAR(10) NOT NULL,
    token_id VARCHAR(255) NOT NULL,
    address_or_account VARCHAR(255) NOT NULL,
    tx_hash_or_sig VARCHAR(255) NOT NULL UNIQUE,
    block_number_or_slot BIGINT,
    op_index_or_log_index INTEGER,
    from_address VARCHAR(255) NOT NULL,
    to_address VARCHAR(255) NOT NULL,
    raw_amount VARCHAR(100) NOT NULL,
    decimals INTEGER NOT NULL DEFAULT 6,
    amount_decimal DECIMAL(20,10) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    confirmations INTEGER NOT NULL DEFAULT 0,
    detected_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    credited_at TIMESTAMP,
    memo VARCHAR(100),
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_onchain_deposits_user_id ON onchain_deposits(user_id);
CREATE INDEX IF NOT EXISTS idx_onchain_deposits_status ON onchain_deposits(status);
CREATE INDEX IF NOT EXISTS idx_onchain_deposits_chain_address ON onchain_deposits(chain, address_or_account);
CREATE INDEX IF NOT EXISTS idx_onchain_deposits_detected_at ON onchain_deposits(detected_at);

-- Add comment to table
COMMENT ON TABLE onchain_deposits IS 'Tracks on-chain USDC and EURC deposits detected from blockchain networks';

-- Add comments to important columns
COMMENT ON COLUMN onchain_deposits.chain IS 'Chain identifier: ethereum, polygon, base, solana, stellar';
COMMENT ON COLUMN onchain_deposits.token IS 'Token symbol: USDC or EURC';
COMMENT ON COLUMN onchain_deposits.token_id IS 'Token contract address (EVM), mint address (Solana), or asset issuer (Stellar)';
COMMENT ON COLUMN onchain_deposits.tx_hash_or_sig IS 'Transaction hash (EVM/Solana) or transaction ID (Stellar)';
COMMENT ON COLUMN onchain_deposits.block_number_or_slot IS 'Block number (EVM), slot (Solana), or ledger (Stellar)';
COMMENT ON COLUMN onchain_deposits.raw_amount IS 'Amount in smallest unit (e.g., 1000000 for 1 USDC with 6 decimals)';
COMMENT ON COLUMN onchain_deposits.status IS 'Deposit status: pending, confirmed, failed, credited';

-- Verify the table was created
SELECT 'onchain_deposits table created successfully' AS result;
