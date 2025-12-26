# P2P Payment System - Database Migration

## Execute Migration

Run the following SQL script against your PostgreSQL database:

```bash
# Option 1: Using psql
psql -h your-host -U your-user -d your-database -f Database/migrations/p2p_payment_system.sql

# Option 2: Using connection string
psql "postgres://user:password@host:5432/database" -f Database/migrations/p2p_payment_system.sql
```

## What This Migration Does

1. **Drops old tables** (if exist):
   - crypto_transactions
   - crypto_payment_requests
   - crypto_payment_methods
   - crypto_exchange_rates
   - payment_events
   - payments
   - invoices
   - platform_wallets

2. **Creates new tables**:
   - `wallets` - User wallet linking (Phantom)
   - `p2p_payments` - P2P payment records
   - `gas_sponsorships` - Gas sponsorship tracking
   - `ledger_entries` - Double-entry balance ledger
   - `platform_hot_wallets` - Platform wallets

3. **Creates functions**:
   - `get_user_balance(user_id)` - Returns user's USDC balance

4. **Creates triggers**:
   - `updated_at` auto-update triggers

## Verify Migration

After running, verify tables exist:

```sql
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
AND table_name IN ('wallets', 'p2p_payments', 'gas_sponsorships', 'ledger_entries', 'platform_hot_wallets');
```

Expected output: 5 rows

## Rollback (if needed)

```sql
DROP TABLE IF EXISTS gas_sponsorships CASCADE;
DROP TABLE IF EXISTS ledger_entries CASCADE;
DROP TABLE IF EXISTS p2p_payments CASCADE;
DROP TABLE IF EXISTS wallets CASCADE;
DROP TABLE IF EXISTS platform_hot_wallets CASCADE;
DROP FUNCTION IF EXISTS get_user_balance(UUID);
```
