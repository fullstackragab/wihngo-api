# HD Wallet Database Migration Guide

## Overview

This migration adds support for Hierarchical Deterministic (HD) wallets to enable unique address generation per payment request. This is the **industry-standard approach** for cryptocurrency payment processing, providing robust transaction verification without depending on payment amounts.

## What This Migration Does

### 1. Adds Address Index Column
- **Table**: `crypto_payment_requests`
- **Column**: `address_index` (integer, nullable)
- **Purpose**: Stores the BIP44 derivation path index for HD wallet addresses
- **Example**: If `address_index = 42`, the derivation path is `m/44'/60'/0'/0/42`

### 2. Creates HD Address Index Sequences

The migration creates PostgreSQL sequences for atomic, collision-free index allocation:

- `hd_address_index_seq` - Global fallback sequence
- `hd_address_index_seq_ethereum` - Ethereum mainnet
- `hd_address_index_seq_sepolia` - Sepolia testnet
- `hd_address_index_seq_tron` - Tron mainnet
- `hd_address_index_seq_binance_smart_chain` - Binance Smart Chain
- `hd_address_index_seq_polygon` - Polygon
- `hd_address_index_seq_bitcoin` - Bitcoin
- `hd_address_index_seq_solana` - Solana

### 3. Creates Database Index
- Index on `address_index` for efficient querying

## How to Run This Migration

### Option 1: Using Entity Framework Core (Recommended)

```bash
# From the project root directory
dotnet ef migrations add AddHdWalletSupport
dotnet ef database update
```

### Option 2: Manual SQL Script

If you prefer to run the migration manually or if you're using a different database tool:

```sql
-- Add address_index column
ALTER TABLE crypto_payment_requests 
ADD COLUMN address_index INTEGER NULL;

-- Create index
CREATE INDEX ix_crypto_payment_requests_address_index 
ON crypto_payment_requests(address_index);

-- Create sequences
CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_ethereum
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_sepolia
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_tron
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_binance_smart_chain
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_polygon
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_bitcoin
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

CREATE SEQUENCE IF NOT EXISTS hd_address_index_seq_solana
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;

-- Add documentation comments
COMMENT ON COLUMN crypto_payment_requests.address_index IS 'HD wallet derivation path index (BIP44). Null for non-HD addresses.';
COMMENT ON SEQUENCE hd_address_index_seq IS 'Global HD wallet address index sequence (fallback)';
COMMENT ON SEQUENCE hd_address_index_seq_ethereum IS 'HD wallet address index sequence for Ethereum mainnet';
COMMENT ON SEQUENCE hd_address_index_seq_sepolia IS 'HD wallet address index sequence for Sepolia testnet';
COMMENT ON SEQUENCE hd_address_index_seq_tron IS 'HD wallet address index sequence for Tron mainnet';
COMMENT ON SEQUENCE hd_address_index_seq_binance_smart_chain IS 'HD wallet address index sequence for Binance Smart Chain';
COMMENT ON SEQUENCE hd_address_index_seq_polygon IS 'HD wallet address index sequence for Polygon';
COMMENT ON SEQUENCE hd_address_index_seq_bitcoin IS 'HD wallet address index sequence for Bitcoin';
COMMENT ON SEQUENCE hd_address_index_seq_solana IS 'HD wallet address index sequence for Solana';
```

## Configuration Required

After running the migration, you need to configure your HD wallet mnemonic:

### Using appsettings.json (Development Only - NOT SECURE)

```json
{
  "PlatformWallets": {
    "HdMnemonic": "your twelve word mnemonic phrase goes here for development only"
  }
}
```

### Using Environment Variables (Production - SECURE)

```bash
# Linux/Mac
export PlatformWallets__HdMnemonic="your twelve word mnemonic phrase"

# Windows PowerShell
$env:PlatformWallets__HdMnemonic = "your twelve word mnemonic phrase"

# Docker
docker run -e PlatformWallets__HdMnemonic="your twelve word mnemonic phrase" ...
```

### Using User Secrets (Development - SECURE)

```bash
dotnet user-secrets set "PlatformWallets:HdMnemonic" "your twelve word mnemonic phrase"
```

## Verification

After migration, verify the setup:

```sql
-- Check that the column exists
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'crypto_payment_requests' 
AND column_name = 'address_index';

-- Check that sequences exist
SELECT sequence_name 
FROM information_schema.sequences 
WHERE sequence_name LIKE 'hd_address_index_seq%';

-- Test sequence allocation
SELECT nextval('hd_address_index_seq_ethereum');
SELECT nextval('hd_address_index_seq_tron');

-- Check current values
SELECT sequence_name, last_value 
FROM hd_address_index_seq_ethereum;
```

## How HD Wallets Work

### BIP44 Standard
The system uses the BIP44 standard for HD wallet derivation:

```
m/44'/coin_type'/account'/change/address_index
```

**Examples:**
- Ethereum: `m/44'/60'/0'/0/0`, `m/44'/60'/0'/0/1`, `m/44'/60'/0'/0/2`, ...
- Bitcoin: `m/44'/0'/0'/0/0`, `m/44'/0'/0'/0/1`, `m/44'/0'/0'/0/2`, ...
- Tron: Uses Ethereum derivation (`m/44'/60'/0'/0/X`) then converts to Tron format

### Payment Flow

1. **User requests payment**
   - System allocates next index from sequence (e.g., `nextval('hd_address_index_seq_ethereum')` returns `5`)
   - Derives address using BIP44 path: `m/44'/60'/0'/0/5`
   - Stores `address_index = 5` in `crypto_payment_requests`
   - Returns unique address to user

2. **User sends cryptocurrency**
   - Sends payment to the unique HD-derived address
   - Each payment has its own address (no collisions)

3. **System verifies payment**
   - Background job scans HD-derived address for incoming transactions
   - Verifies amount and confirmations on blockchain
   - No need to rely on payment amount for identification

### Why HD Wallets?

**Problem with Static Addresses:**
- Multiple users pay to same address
- Cannot distinguish which payment belongs to which user
- Must rely on amount matching (unreliable due to exchange rate fluctuations)

**Solution with HD Wallets:**
- Each payment gets a unique address
- `address_index` provides 1-to-1 mapping
- Reliable transaction verification using blockchain data
- Industry standard used by all major exchanges and payment processors

## Rollback Instructions

If you need to rollback this migration:

```bash
# Using EF Core
dotnet ef database update PreviousMigrationName

# Or manually
```

```sql
-- Drop sequences
DROP SEQUENCE IF EXISTS hd_address_index_seq;
DROP SEQUENCE IF EXISTS hd_address_index_seq_ethereum;
DROP SEQUENCE IF EXISTS hd_address_index_seq_sepolia;
DROP SEQUENCE IF EXISTS hd_address_index_seq_tron;
DROP SEQUENCE IF EXISTS hd_address_index_seq_binance_smart_chain;
DROP SEQUENCE IF EXISTS hd_address_index_seq_polygon;
DROP SEQUENCE IF EXISTS hd_address_index_seq_bitcoin;
DROP SEQUENCE IF EXISTS hd_address_index_seq_solana;

-- Drop index
DROP INDEX IF EXISTS ix_crypto_payment_requests_address_index;

-- Drop column
ALTER TABLE crypto_payment_requests 
DROP COLUMN IF EXISTS address_index;
```

## Security Considerations

### CRITICAL: Protect Your Mnemonic Phrase

?? **NEVER** commit your mnemonic phrase to source control!

**Production Best Practices:**
1. Use environment variables or secure key management systems (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
2. Rotate mnemonics periodically
3. Use hardware security modules (HSM) for high-value operations
4. Implement multi-signature requirements for withdrawals
5. Monitor all HD-derived addresses for suspicious activity

### Mnemonic Storage Options

**? BAD (Never do this):**
```json
// appsettings.json
{
  "PlatformWallets": {
    "HdMnemonic": "actual mnemonic here"  // ? NEVER!
  }
}
```

**? GOOD:**
```bash
# Environment variable
export PlatformWallets__HdMnemonic="$(cat /secure/path/mnemonic.txt)"

# Or use secret management service
az keyvault secret show --name "HdMnemonic" --vault-name "YourVault"
```

## Monitoring

Monitor HD wallet usage:

```sql
-- Check HD address allocation by network
SELECT 
    network,
    COUNT(*) as total_payments,
    COUNT(address_index) as hd_payments,
    COUNT(*) - COUNT(address_index) as static_payments,
    MIN(address_index) as min_index,
    MAX(address_index) as max_index
FROM crypto_payment_requests
GROUP BY network
ORDER BY network;

-- Check sequence usage
SELECT 
    'ethereum' as network,
    last_value as current_index,
    (SELECT MAX(address_index) FROM crypto_payment_requests WHERE network = 'ethereum') as max_used
FROM hd_address_index_seq_ethereum
UNION ALL
SELECT 
    'tron',
    last_value,
    (SELECT MAX(address_index) FROM crypto_payment_requests WHERE network = 'tron')
FROM hd_address_index_seq_tron;

-- Find payments with HD addresses
SELECT 
    id,
    user_id,
    amount_crypto,
    currency,
    network,
    wallet_address,
    address_index,
    status,
    created_at
FROM crypto_payment_requests
WHERE address_index IS NOT NULL
ORDER BY created_at DESC
LIMIT 20;
```

## Troubleshooting

### Issue: Sequences not incrementing

**Symptom:** Multiple payments get the same address

**Solution:**
```sql
-- Check current sequence values
SELECT last_value FROM hd_address_index_seq_ethereum;

-- Manually set sequence if needed
ALTER SEQUENCE hd_address_index_seq_ethereum RESTART WITH 100;
```

### Issue: HD derivation failing

**Symptom:** All payments use static wallet address

**Check:**
1. Verify mnemonic is configured: Check logs for "[HD Wallet] HD mnemonic not configured"
2. Verify mnemonic is valid: Must be 12, 15, 18, 21, or 24 words
3. Check HdWalletService is registered in DI container

### Issue: Cannot find transactions

**Symptom:** Background job doesn't detect payments

**Check:**
1. Verify wallet scanning logic uses correct address from `wallet_address` column
2. Check blockchain explorer that address actually received payment
3. Verify network matches (mainnet vs testnet)

## Support

For issues with this migration:
1. Check application logs for HD wallet derivation errors
2. Verify database sequences are incrementing
3. Ensure mnemonic phrase is correctly configured
4. Test derivation manually using the HdWalletService

## References

- [BIP39 - Mnemonic Code](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)
- [BIP44 - Multi-Account Hierarchy](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki)
- [SLIP44 - Registered Coin Types](https://github.com/satoshilabs/slips/blob/master/slip-0044.md)
