# ??? Database Setup Guide - Crypto Payment System

## Quick Answer

**You DON'T need to run any SQL scripts manually** ?

The application will automatically create all tables when you run `dotnet run` for the first time.

---

## Option 1: Automatic Setup (Recommended) ?

Your application is already configured to auto-create the database:

```bash
# Just run the application
dotnet run

# Database will be created automatically with:
# ? All tables
# ? Seed data (wallets & exchange rates)
# ? Indexes
# ? Constraints
```

The magic happens in `Program.cs`:
```csharp
db.Database.EnsureCreated();
```

---

## Option 2: Manual SQL Script ??

If you prefer manual control, use the provided SQL script:

### Step 1: Create Database
```sql
-- Connect to PostgreSQL as superuser
psql -U postgres

-- Create database
CREATE DATABASE wihngo;

-- Exit
\q
```

### Step 2: Run Setup Script
```bash
# Connect to wihngo database
psql -U postgres -d wihngo

# Run the setup script
\i Database/crypto_payment_setup.sql

# Or in one command:
psql -U postgres -d wihngo -f Database/crypto_payment_setup.sql
```

---

## Option 3: Using EF Migrations (Most Professional) ??

If you want to use proper migrations instead of `EnsureCreated()`:

### Step 1: Remove EnsureCreated
Comment out this in `Program.cs`:
```csharp
// db.Database.EnsureCreated();
```

### Step 2: Create Initial Migration
```bash
dotnet ef migrations add InitialCreate
```

### Step 3: Apply Migration
```bash
dotnet ef database update
```

### Step 4: Add Crypto Payment Migration
```bash
dotnet ef migrations add AddCryptoPayment
dotnet ef database update
```

---

## Verification Queries

After setup (automatic or manual), verify with these queries:

```sql
-- Check all crypto tables exist
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name LIKE '%crypto%'
ORDER BY table_name;

-- Should return:
-- crypto_exchange_rates
-- crypto_payment_methods
-- crypto_payment_requests
-- crypto_transactions
-- platform_wallets

-- Check platform wallet
SELECT * FROM platform_wallets;
-- Should have TRON USDT wallet

-- Check exchange rates
SELECT currency, usd_rate, source FROM crypto_exchange_rates;
-- Should have 7 currencies (BTC, ETH, USDT, USDC, BNB, SOL, DOGE)
```

---

## Database Connection String

Update in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

Or use environment variable:
```bash
# Windows PowerShell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=YOUR_PASSWORD"

# Linux/Mac
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=YOUR_PASSWORD"
```

---

## Common Database Operations

### Check Payment Status
```sql
SELECT id, status, currency, amount_usd, created_at 
FROM crypto_payment_requests 
ORDER BY created_at DESC 
LIMIT 10;
```

### Update Exchange Rate Manually
```sql
UPDATE crypto_exchange_rates 
SET usd_rate = 50000, last_updated = NOW() 
WHERE currency = 'BTC';
```

### Expire Old Payments
```sql
UPDATE crypto_payment_requests 
SET status = 'expired', updated_at = NOW() 
WHERE status = 'pending' AND expires_at < NOW();
```

### View Payment Statistics
```sql
SELECT 
    status,
    COUNT(*) as count,
    SUM(amount_usd) as total_usd,
    currency
FROM crypto_payment_requests
GROUP BY status, currency
ORDER BY status, currency;
```

### Add New Platform Wallet
```sql
INSERT INTO platform_wallets (currency, network, address, is_active)
VALUES ('BTC', 'bitcoin', 'YOUR_BTC_ADDRESS', true);
```

---

## Troubleshooting

### Issue: Database doesn't exist
```bash
# Create it manually
psql -U postgres -c "CREATE DATABASE wihngo;"
```

### Issue: Connection refused
```bash
# Check PostgreSQL is running
sudo systemctl status postgresql  # Linux
# or
Get-Service postgresql*  # Windows PowerShell

# Start PostgreSQL if needed
sudo systemctl start postgresql  # Linux
```

### Issue: Permission denied
```bash
# Grant permissions
psql -U postgres
GRANT ALL PRIVILEGES ON DATABASE wihngo TO your_username;
```

### Issue: Tables not created
Check logs for errors, then either:
1. Run the application again (auto-create)
2. Run the SQL script manually
3. Use EF migrations

---

## Production Recommendations

For production deployment:

1. **Use Migrations** instead of `EnsureCreated()`
2. **Backup Database** regularly
3. **Use Connection Pooling** (already configured in Npgsql)
4. **Monitor Query Performance** with indexes
5. **Secure Credentials** with Azure Key Vault or similar
6. **Use Read Replicas** for high traffic

---

## Database Schema Diagram

```
platform_wallets
?? id (PK)
?? currency (BTC, ETH, USDT, etc.)
?? network (bitcoin, ethereum, tron, etc.)
?? address
?? is_active

crypto_payment_requests
?? id (PK)
?? user_id (FK)
?? bird_id (FK, nullable)
?? amount_usd
?? amount_crypto
?? currency
?? network
?? wallet_address
?? transaction_hash
?? status
?? expires_at

crypto_transactions
?? id (PK)
?? payment_request_id (FK)
?? transaction_hash (UNIQUE)
?? from_address
?? to_address
?? amount
?? confirmations

crypto_exchange_rates
?? id (PK)
?? currency (UNIQUE)
?? usd_rate
?? last_updated
```

---

## Summary

? **Easiest**: Just run `dotnet run` - database auto-creates  
? **Professional**: Use EF migrations  
? **Manual Control**: Run SQL script from `Database/crypto_payment_setup.sql`  

**No manual SQL execution needed for basic operation!** ??

---

**Need Help?**
- Check logs if auto-creation fails
- Use `psql -U postgres -d wihngo` to connect manually
- Verify connection string in appsettings.json
- Ensure PostgreSQL is running
