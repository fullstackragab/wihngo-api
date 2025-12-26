# ?? FINAL MIGRATION INSTRUCTIONS

## ? Clean SQL Scripts Ready

I've created **clean, production-ready SQL scripts** with all syntax issues fixed.

---

## ?? Available Scripts

### **Option 1: Full Migration with Verification (RECOMMENDED)**
**File:** `Database/migrations/WIHNGO_FINAL_MIGRATION.sql`
- Updates old wallets to inactive
- Inserts your 10 wallet addresses
- Shows verification output
- Wrapped in transaction (BEGIN/COMMIT)

### **Option 2: Simple Migration Only**
**File:** `Database/migrations/WIHNGO_SIMPLE_MIGRATION.sql`
- Just the INSERT statements
- No verification queries
- Faster execution
- Wrapped in transaction

### **Option 3: Verification Only**
**File:** `Database/migrations/VERIFY_MIGRATION.sql`
- Run this AFTER migration
- Checks all wallets
- Verifies counts
- Shows inactive wallets

---

## ?? RECOMMENDED: Run Option 1

```bash
# Connect to database
psql "postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require"

# Run the migration
\i Database/migrations/WIHNGO_FINAL_MIGRATION.sql
```

---

## ? Expected Output

After running, you should see:

```
BEGIN
UPDATE 0  (or some number if old wallets exist)
INSERT 0 5
INSERT 0 5
```

Then verification tables showing:

**Active Wallets (10 rows):**
```
 status        | currency | network  | address
---------------+----------+----------+-------------------------------------------------
 Active Wallets| EURC     | base     | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 Active Wallets| EURC     | ethereum | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 Active Wallets| EURC     | polygon  | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 Active Wallets| EURC     | solana   | AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn
 Active Wallets| EURC     | stellar  | GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG
 Active Wallets| USDC     | base     | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 Active Wallets| USDC     | ethereum | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 Active Wallets| USDC     | polygon  | 0xfcc173a7569492439ec3df467d0ec0c05c0f541c
 Active Wallets| USDC     | solana   | AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn
 Active Wallets| USDC     | stellar  | GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG
```

**Summary (2 rows):**
```
 status  | currency | wallet_count
---------+----------+--------------
 Summary | EURC     |            5
 Summary | USDC     |            5
```

```
COMMIT
```

---

## ?? Manual Verification (Optional)

If you want to verify separately:

```bash
\i Database/migrations/VERIFY_MIGRATION.sql
```

---

## ?? Troubleshooting

### If you see "relation platform_wallets does not exist"
Your table hasn't been created yet. First run:
```sql
-- Create the table if it doesn't exist
CREATE TABLE IF NOT EXISTS platform_wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    currency VARCHAR(10) NOT NULL,
    network VARCHAR(50) NOT NULL,
    address VARCHAR(255) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    derivation_path VARCHAR(100),
    UNIQUE (currency, network, address)
);
```

### If you see "duplicate key violation"
This is **OKAY** - it means wallets already exist. The script uses `ON CONFLICT DO UPDATE` to handle this.

### If transaction fails
The entire migration will rollback automatically. Fix the issue and re-run.

---

## ?? Your Wallet Addresses (Reference)

| Network | Address |
|---------|---------|
| **Solana** | `AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn` |
| **Ethereum** | `0xfcc173a7569492439ec3df467d0ec0c05c0f541c` |
| **Polygon** | `0xfcc173a7569492439ec3df467d0ec0c05c0f541c` |
| **Base** | `0xfcc173a7569492439ec3df467d0ec0c05c0f541c` |
| **Stellar** | `GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG` |

---

## ? What These Scripts Do

1. **Deactivate** old wallets (USDT, ETH, BNB, Tron, BSC, Avalanche)
2. **Insert** 5 USDC wallet entries
3. **Insert** 5 EURC wallet entries
4. **Verify** all wallets are active and correct

**Total: 10 active wallet configurations**

---

## ?? Quick Command

Copy and paste this entire command:

```bash
psql "postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require" -f Database/migrations/WIHNGO_FINAL_MIGRATION.sql
```

---

**Status:** ? Scripts Ready  
**Syntax:** ? All Fixed  
**Action:** ?? Run the command above!
