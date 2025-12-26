# Wihngo Database Setup Guide

## ? Database Successfully Created!

Your local PostgreSQL database has been set up with **all tables, relations, and seed data**.

## ?? Quick Reference

### Connection Details
```
Host:     localhost
Port:     5432
Database: postgres
Username: postgres
Password: postgres

Connection String:
Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres
```

### Database Statistics
- **29 Tables** created with full relations
- **5 Users** seeded (Development)
- **10 Birds** seeded (Development)
- **13 Stories** seeded (Development)
- **22 Support Transactions** seeded (Development)
- **19 Love relationships** seeded (Development)
- **6 Notifications** seeded (Development)
- **5 Invoices** seeded (Development)
- **8 Crypto Payment Requests** seeded (Development)
- **4 Supported Tokens** (Production data)
- **4 Platform Wallets** (Production data)
- **7 Exchange Rates** (Production data)

## ?? Getting Started

### Start the Application
```bash
dotnet run
```

The application will:
- Connect to your local PostgreSQL database
- Verify all tables exist
- Start the API on http://localhost:5162
- Start Hangfire dashboard on http://localhost:5162/hangfire

### Test Login
Use any of these accounts:
```
Email: alice@example.com
Password: Password123!

Email: bob@example.com
Password: Password123!

Email: carol@example.com
Password: Password123!

Email: david@example.com
Password: Password123!

Email: eve@example.com
Password: Password123!
```

## ?? Documentation Files

### Created Files
1. **DATABASE_SETUP_SUMMARY.md** - Complete database setup documentation
2. **Database/schema-documentation.sql** - SQL schema with detailed comments
3. **reset-database.bat** - Script to reset and reseed database
4. **README-DATABASE.md** - This file

## ?? Useful Commands

### Connect to Database
```bash
psql -h localhost -U postgres -d postgres
```

### View All Tables
```sql
\dt
```

### Count Records
```sql
SELECT 'users' AS table, COUNT(*) FROM users
UNION ALL SELECT 'birds', COUNT(*) FROM birds
UNION ALL SELECT 'stories', COUNT(*) FROM stories
UNION ALL SELECT 'support_transactions', COUNT(*) FROM support_transactions;
```

### View Sample Data

#### List Users
```sql
SELECT user_id, name, email, email_confirmed, created_at 
FROM users 
ORDER BY created_at;
```

#### List Birds with Owners
```sql
SELECT 
    b.name as bird_name, 
    b.species, 
    u.name as owner_name, 
    b.loved_count, 
    b.donation_cents / 100.0 as donation_dollars,
    b.created_at
FROM birds b 
JOIN users u ON b.owner_id = u.user_id
ORDER BY b.created_at;
```

#### List Recent Support Transactions
```sql
SELECT 
    st.amount,
    b.name as bird_name,
    u.name as supporter_name,
    st.message,
    st.created_at
FROM support_transactions st
JOIN birds b ON st.bird_id = b.bird_id
JOIN users u ON st.supporter_id = u.user_id
ORDER BY st.created_at DESC
LIMIT 10;
```

#### List Supported Tokens
```sql
SELECT 
    token_symbol,
    chain,
    mint_address,
    decimals,
    is_active
FROM supported_tokens
ORDER BY chain, token_symbol;
```

## ?? Reset Database

If you need to reset the database and reseed all data:

### Option 1: Use the Reset Script
```bash
reset-database.bat
```

### Option 2: Manual Reset
```bash
# Drop and recreate schema
psql -h localhost -U postgres -d postgres -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

# Run application to recreate tables and seed data
dotnet run
```

## ?? Database Schema

### Core Tables
- **users** - User accounts with authentication
- **birds** - Bird profiles
- **stories** - Updates and stories about birds
- **support_transactions** - Donations to birds
- **loves** - User-bird love relationships
- **support_usage** - How bird owners use donations

### Premium Features
- **bird_premium_subscriptions** - Premium subscriptions
- **premium_styles** - Custom styling for premium birds
- **charity_allocations** - Charity contribution tracking
- **charity_impact_stats** - Charity statistics

### Crypto Payments
- **platform_wallets** - Merchant wallet addresses
- **crypto_payment_requests** - Pending/completed crypto payments
- **crypto_transactions** - Blockchain transactions
- **crypto_exchange_rates** - USD exchange rates
- **crypto_payment_methods** - User's saved wallets
- **on_chain_deposits** - Direct wallet deposits
- **token_configurations** - Supported tokens configuration

### Notifications
- **notifications** - System notifications
- **notification_preferences** - User notification preferences
- **notification_settings** - Global notification settings
- **user_devices** - Device tokens for push notifications

### Invoice & Payment System
- **invoices** - Payment invoices
- **payments** - Payment transactions
- **supported_tokens** - Accepted crypto tokens
- **refund_requests** - Refund management
- **payment_events** - Payment state audit log
- **audit_logs** - System-wide audit trail
- **webhook_received** - Incoming webhooks
- **blockchain_cursors** - Blockchain scanning progress

## ?? Security Features

- **Password Hashing**: BCrypt with work factor 12
- **JWT Authentication**: Tokens expire after 24 hours
- **Refresh Tokens**: Expire after 30 days
- **Account Lockout**: After 5 failed attempts (30 minutes)
- **Email Confirmation**: Required for new accounts
- **Password Reset**: Time-limited tokens (1 hour)

## ?? Key Relationships

```
users (1) ??> (many) birds
users (1) ??> (many) stories
users (1) ??> (many) support_transactions
users (many) ??> (many) birds [via loves]
users (1) ??> (many) notifications
users (1) ??> (many) invoices
users (1) ??> (many) crypto_payment_requests

birds (1) ??> (many) stories
birds (1) ??> (many) support_transactions
birds (1) ??> (1) premium_styles
birds (1) ??> (many) bird_premium_subscriptions

invoices (1) ??> (many) payments
invoices (1) ??> (many) payment_events
invoices (1) ??> (many) refund_requests

bird_premium_subscriptions (1) ??> (many) charity_allocations
```

## ?? API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh-token` - Refresh access token
- `POST /api/auth/logout` - Logout
- `POST /api/auth/confirm-email` - Confirm email
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password
- `POST /api/auth/change-password` - Change password

### Birds
- `GET /api/birds` - List all birds
- `GET /api/birds/{id}` - Get bird details
- `POST /api/birds` - Create bird
- `PUT /api/birds/{id}` - Update bird
- `DELETE /api/birds/{id}` - Delete bird

### Stories
- `GET /api/birds/{birdId}/stories` - List bird stories
- `POST /api/birds/{birdId}/stories` - Create story

### Support
- `POST /api/birds/{birdId}/support` - Support a bird
- `GET /api/birds/{birdId}/supporters` - List supporters

### Crypto Payments
- `POST /api/crypto-payments/request` - Create payment request
- `GET /api/crypto-payments/{id}` - Get payment status
- `GET /api/crypto-payments/rates` - Get exchange rates

### Invoices
- `POST /api/invoices` - Create invoice
- `GET /api/invoices/{id}` - Get invoice details
- `GET /api/invoices` - List user invoices

## ?? Troubleshooting

### Connection Refused
**Problem**: Can't connect to database
**Solution**: 
```bash
# Check if PostgreSQL is running
pg_ctl status

# Start PostgreSQL
pg_ctl start
```

### Wrong Credentials
**Problem**: Authentication failed
**Solution**:
```bash
# Check user secrets
dotnet user-secrets list

# Update if needed
dotnet user-secrets set "ConnectionStrings__DefaultConnection" "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
```

### Tables Not Created
**Problem**: Application starts but tables don't exist
**Solution**:
```bash
# Check environment
echo $env:ASPNETCORE_ENVIRONMENT

# Should be "Development" - set if not
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run application
dotnet run
```

### Data Not Seeded
**Problem**: Tables exist but no data
**Solution**:
```bash
# Drop schema and recreate
psql -h localhost -U postgres -d postgres -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

# Run application to seed
dotnet run
```

## ?? Need Help?

### View Logs
Check application console output for detailed error messages.

### Database Console
```bash
psql -h localhost -U postgres -d postgres
```

### Check Table Structure
```sql
\d+ users
\d+ birds
\d+ invoices
```

### View Recent Migrations
```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY migration_id;
```

## ?? Success!

Your Wihngo database is fully configured with:
- ? All 29 tables created
- ? All foreign key relationships established
- ? All indexes created for performance
- ? Development seed data populated
- ? Production configuration data loaded
- ? Hangfire background jobs configured
- ? Ready for development and testing!

**Happy coding!** ??
