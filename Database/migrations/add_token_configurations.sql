-- Migration: Add token_configurations table and seed with Circle's canonical addresses
-- Date: 2025-01-20
-- Description: Stores canonical USDC/EURC token addresses for Ethereum, Polygon, Base, Solana, Stellar

-- Create token_configurations table
CREATE TABLE IF NOT EXISTS token_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    token VARCHAR(10) NOT NULL,
    chain VARCHAR(20) NOT NULL,
    token_address VARCHAR(255) NOT NULL,
    issuer VARCHAR(255),
    decimals INTEGER NOT NULL DEFAULT 6,
    required_confirmations INTEGER NOT NULL DEFAULT 12,
    is_active BOOLEAN NOT NULL DEFAULT true,
    derivation_path VARCHAR(100),
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(token, chain)
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_token_configurations_is_active ON token_configurations(is_active);

-- Seed USDC addresses (from Circle's official documentation)
INSERT INTO token_configurations (token, chain, token_address, decimals, required_confirmations, derivation_path, is_active) VALUES
    -- USDC on Ethereum
    ('USDC', 'ethereum', '0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48', 6, 12, 'm/44''/60''/0''/0/{index}', true),
    
    -- USDC on Polygon
    ('USDC', 'polygon', '0x3c499c542cEF5E3811e1192ce70d8cC03d5c3359', 6, 12, 'm/44''/60''/0''/0/{index}', true),
    
    -- USDC on Base
    ('USDC', 'base', '0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913', 6, 12, 'm/44''/60''/0''/0/{index}', true),
    
    -- USDC on Solana
    ('USDC', 'solana', 'EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v', 6, 1, 'm/44''/501''/0''/0''', true),
    
    -- USDC on Stellar (asset code + issuer)
    ('USDC', 'stellar', 'USDC', 6, 1, 'm/44''/148''/0''', true);

-- Update Stellar USDC with issuer
UPDATE token_configurations 
SET issuer = 'GA5ZSEJYB37JRC5AVCIA5CHE6DZE27NUYDRA5EJQ2TCBS2C3K3YVSZXG'
WHERE token = 'USDC' AND chain = 'stellar';

-- Seed EURC addresses (from Circle's official documentation)
INSERT INTO token_configurations (token, chain, token_address, decimals, required_confirmations, derivation_path, is_active) VALUES
    -- EURC on Ethereum
    ('EURC', 'ethereum', '0x1aBaEA1f7C830bD89Acc67eC4af516284b1bC33c', 6, 12, 'm/44''/60''/0''/0/{index}', true),
    
    -- EURC on Base
    ('EURC', 'base', '0x60a3E35Cc302bFA44Cb288Bc5a4F316Fdb1adb42', 6, 12, 'm/44''/60''/0''/0/{index}', true),
    
    -- EURC on Solana
    ('EURC', 'solana', 'HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr', 6, 1, 'm/44''/501''/0''/0''', true),
    
    -- EURC on Stellar (asset code + issuer)
    ('EURC', 'stellar', 'EURC', 6, 1, 'm/44''/148''/0''', true);

-- Update Stellar EURC with issuer
UPDATE token_configurations 
SET issuer = 'GDHU6WRG4IEQXM5NZ4BMPKOXHW76MZM4Y2IEMFDVXBSDP6SJY4ITNPP2'
WHERE token = 'EURC' AND chain = 'stellar';

-- Add comments
COMMENT ON TABLE token_configurations IS 'Canonical token addresses for USDC and EURC across supported chains';
COMMENT ON COLUMN token_configurations.token_address IS 'Contract address (EVM), mint address (Solana), or asset code (Stellar)';
COMMENT ON COLUMN token_configurations.issuer IS 'Asset issuer for Stellar tokens';
COMMENT ON COLUMN token_configurations.derivation_path IS 'HD derivation path template for generating addresses';

-- Verify the data
SELECT token, chain, token_address, issuer, decimals, required_confirmations, is_active 
FROM token_configurations 
ORDER BY token, chain;

SELECT 'Token configurations seeded successfully' AS result;
