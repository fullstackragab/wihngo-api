# Wihngo Database Setup Complete ?

## Connection Details
- **Host:** localhost
- **Port:** 5432
- **Database:** postgres
- **Username:** postgres
- **Password:** postgres
- **Connection String:** `Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres`

## Database Schema Created

### Total Tables: 29

#### Core Application Tables (9)
1. **users** - User accounts with authentication
2. **birds** - Bird profiles
3. **stories** - Updates and stories about birds
4. **support_transactions** - Donations to birds
5. **loves** - User-bird love relationships (many-to-many)
6. **support_usage** - How bird owners use donations
7. **bird_premium_subscriptions** - Premium subscriptions
8. **premium_styles** - Custom styling for premium birds
9. **charity_allocations** - Charity contribution tracking

#### Crypto Payment Tables (8)
10. **platform_wallets** - Merchant wallet addresses
11. **crypto_payment_requests** - Pending/completed crypto payments
12. **crypto_transactions** - Blockchain transactions
13. **crypto_exchange_rates** - USD exchange rates
14. **crypto_payment_methods** - User's saved wallets
15. **on_chain_deposits** - Direct wallet deposits
16. **token_configurations** - Supported tokens configuration
17. **charity_impact_stats** - Charity statistics

#### Notification Tables (4)
18. **notifications** - System notifications
19. **notification_preferences** - User notification preferences
20. **notification_settings** - Global notification settings
21. **user_devices** - Device tokens for push notifications

#### Invoice & Payment System Tables (8)
22. **invoices** - Payment invoices
23. **payments** - Payment transactions
24. **supported_tokens** - Accepted crypto tokens
25. **refund_requests** - Refund management
26. **payment_events** - Payment state audit log
27. **audit_logs** - System-wide audit trail
28. **webhook_received** - Incoming webhooks
29. **blockchain_cursors** - Blockchain scanning progress

## Seeded Data

### Production Data (Seeded in all environments)
? **4 Supported Tokens**
- USDC on Solana (EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v)
- EURC on Solana (HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr)
- USDC on Base (0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913)
- EURC on Base (0x60a3E35Cc302bFA44Cb288Bc5a4F316Fdb1adb42)

? **4 Platform Wallets**
- USDT on Tron
- USDT on Ethereum
- USDT on BSC
- ETH on Sepolia

? **7 Exchange Rates**
- BTC, ETH, USDT, USDC, BNB, SOL, DOGE

### Development Data (Seeded in Development environment only)
? **5 Users** - All with email/password authentication
- alice@example.com (Password: Password123!)
- bob@example.com (Password: Password123!)
- carol@example.com (Password: Password123!)
- david@example.com (Password: Password123!)
- eve@example.com (Password: Password123!)

? **10 Birds** - Various hummingbird species
- Sunny (Anna's Hummingbird)
- Flash (Ruby-throated Hummingbird)
- Bella (Black-chinned Hummingbird)
- Spike (Allen's Hummingbird)
- Luna (Calliope Hummingbird)
- Zippy (Rufous Hummingbird)
- Jewel (Costa's Hummingbird)
- Blaze (Broad-tailed Hummingbird)
- Misty (Buff-bellied Hummingbird)
- Emerald (Magnificent Hummingbird)

? **13 Stories** - Updates about the birds

? **19 Loves** - User-bird love relationships

? **22 Support Transactions** - Donations ($5-$100 each)

? **6 Notifications** - Sample notifications for users

? **5 Invoices** - Sample invoices in various states

? **8 Crypto Payment Requests** - Sample crypto payments

## Database Relations

### Primary Foreign Key Relationships
- **birds** ? users (owner_id)
- **stories** ? birds (bird_id) + users (author_id)
- **support_transactions** ? birds (bird_id) + users (supporter_id)
- **loves** ? users (user_id) + birds (bird_id)
- **bird_premium_subscriptions** ? birds (bird_id) + users (user_id)
- **premium_styles** ? birds (bird_id) UNIQUE
- **charity_allocations** ? bird_premium_subscriptions (subscription_id)
- **crypto_payment_requests** ? users (user_id) + birds (bird_id, nullable)
- **invoices** ? users (user_id) + birds (bird_id, nullable)
- **payments** ? invoices (invoice_id)
- **notifications** ? users (user_id)
- **refund_requests** ? invoices (invoice_id)

### Cascade Delete Rules
Most relations use `ON DELETE CASCADE` to maintain referential integrity.
Some use `ON DELETE SET NULL` for audit purposes (e.g., payments, audit logs).

## Indexes Created

### Performance Indexes (30+)
- User email lookups
- Bird owner and metrics lookups
- Story and transaction queries by bird/user
- Crypto payment status and hash lookups
- Notification queries by user and read status
- Invoice and payment lookups by various criteria
- Audit log queries by entity and time
- Blockchain cursor unique constraints

## Sequences

? **wihngo_invoice_seq** - For generating invoice numbers (WIH-1000, WIH-1001, etc.)

## Special Features

### JSON Fields (JSONB)
- invoices.metadata
- invoices.preferred_payment_methods
- invoices.base_payment_data
- payments.metadata
- notifications.data
- payment_events.metadata
- audit_logs.changes
- webhook_received.payload

### Enum Fields (String-based)
- Invoice states: CREATED, ISSUED, PENDING, PAID, FAILED, REFUNDED, EXPIRED
- Payment states: PENDING, DETECTED, CONFIRMED, SETTLED, FAILED, REFUNDED
- Notification types: BirdLoved, BirdSupported, StoryPosted, etc.
- Payment providers: SOLANA, BASE, PAYPAL, STRIPE, etc.

## Background Jobs (Hangfire)

? **Configured with PostgreSQL storage**

Jobs scheduled:
- ExchangeRateUpdateJob - Updates crypto exchange rates
- PaymentMonitorJob - Monitors pending crypto payments
- ReconciliationJob - Reconciles payments with blockchain
- NotificationCleanupJob - Cleans old notifications
- DailyDigestJob - Sends daily notification digests
- PremiumExpiryNotificationJob - Notifies about expiring subscriptions
- CharityAllocationJob - Processes charity allocations

## Verification Commands

### Check all tables
```bash
psql -h localhost -U postgres -d postgres -c "\dt"
```

### Count records in each table
```sql
SELECT 'users' AS table_name, COUNT(*) FROM users
UNION ALL SELECT 'birds', COUNT(*) FROM birds
UNION ALL SELECT 'stories', COUNT(*) FROM stories
UNION ALL SELECT 'support_transactions', COUNT(*) FROM support_transactions
UNION ALL SELECT 'loves', COUNT(*) FROM loves
UNION ALL SELECT 'notifications', COUNT(*) FROM notifications
UNION ALL SELECT 'invoices', COUNT(*) FROM invoices
UNION ALL SELECT 'crypto_payment_requests', COUNT(*) FROM crypto_payment_requests
UNION ALL SELECT 'supported_tokens', COUNT(*) FROM supported_tokens
UNION ALL SELECT 'platform_wallets', COUNT(*) FROM platform_wallets
UNION ALL SELECT 'crypto_exchange_rates', COUNT(*) FROM crypto_exchange_rates;
```

### View sample data
```sql
-- List all users
SELECT user_id, name, email, email_confirmed, created_at FROM users;

-- List all birds with owner names
SELECT b.name, b.species, u.name as owner_name, b.loved_count, b.donation_cents 
FROM birds b 
JOIN users u ON b.owner_id = u.user_id;

-- List support transactions
SELECT st.amount, b.name as bird_name, u.name as supporter_name, st.message, st.created_at
FROM support_transactions st
JOIN birds b ON st.bird_id = b.bird_id
JOIN users u ON st.supporter_id = u.user_id
ORDER BY st.created_at DESC;

-- List supported tokens
SELECT token_symbol, chain, mint_address, is_active FROM supported_tokens;
```

## Next Steps

### 1. Start the Application
```bash
dotnet run
```

### 2. Test Authentication
```bash
# Register new user
POST http://localhost:5162/api/auth/register

# Login existing user
POST http://localhost:5162/api/auth/login
{
  "email": "alice@example.com",
  "password": "Password123!"
}
```

### 3. Access Hangfire Dashboard
```
http://localhost:5162/hangfire
```

### 4. Explore the API
All endpoints are documented and accessible at:
```
http://localhost:5162/swagger
```

## Documentation Files

?? **Database/schema-documentation.sql** - Complete SQL schema with comments
?? **DATABASE_SETUP_SUMMARY.md** - This file

## Notes

- All passwords are hashed using BCrypt (work factor 12)
- JWT tokens expire after 24 hours
- Refresh tokens expire after 30 days
- Email confirmation required for new accounts
- Account lockout after 5 failed login attempts (30 minutes)
- Database uses snake_case naming convention
- All timestamps in UTC
- Soft deletes not implemented (hard deletes with cascades)

## Troubleshooting

### If database connection fails:
1. Ensure PostgreSQL is running: `pg_ctl status`
2. Check credentials in user secrets: `dotnet user-secrets list`
3. Test connection: `psql -h localhost -U postgres -d postgres`

### If seeding fails:
1. Drop and recreate schema: `psql -h localhost -U postgres -d postgres -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"`
2. Restart application

### If Hangfire fails:
1. Check Hangfire tables exist: `\dt hangfire.*`
2. Check connection string has proper permissions
3. Review logs for specific errors

---

**Database successfully created and seeded!** ??

Connection: `Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres`
