# ?? Application Successfully Started!

## ? Startup Summary

Your Wihngo API is now **running successfully** on your local machine!

```
???????????????????????????????????????????????
? Database connection successful on attempt 1!
?? PostgreSQL Version: PostgreSQL 18.1
? Database connection verified
? Database already exists
? Hangfire configured successfully
???????????????????????????????????????????????
```

## ?? Access Your Application

### Main Endpoints

| Service | URL | Description |
|---------|-----|-------------|
| **API Base** | http://localhost:5162 | Main API endpoint |
| **Swagger UI** | http://localhost:5162/swagger | Interactive API documentation |
| **Hangfire Dashboard** | http://localhost:5162/hangfire | Background jobs monitoring |

### Quick Links
- ?? [Open Swagger Documentation](http://localhost:5162/swagger)
- ?? [Open Hangfire Dashboard](http://localhost:5162/hangfire)
- ?? [Test Auth Endpoint](http://localhost:5162/api/auth)

## ?? Test the API

### Option 1: Run Test Script
```powershell
.\test-api.ps1
```

This will test:
- ? Auth endpoint
- ? Login with test user
- ? Get birds list
- ? Get supported tokens
- ? Swagger UI

### Option 2: Manual Testing with cURL

#### Test Auth Endpoint
```bash
curl http://localhost:5162/api/auth
```

#### Login
```bash
curl -X POST http://localhost:5162/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{\"email\":\"alice@example.com\",\"password\":\"Password123!\"}'
```

#### Get Birds (requires authentication)
```bash
$token = "YOUR_JWT_TOKEN_HERE"
curl http://localhost:5162/api/birds `
  -H "Authorization: Bearer $token"
```

### Option 3: Use Swagger UI (Recommended)
1. Open http://localhost:5162/swagger in your browser
2. Click on any endpoint to expand it
3. Click "Try it out"
4. Fill in parameters
5. Click "Execute"

## ?? Test Accounts

Use any of these pre-seeded accounts:

| Email | Password | Name |
|-------|----------|------|
| alice@example.com | Password123! | Alice Johnson |
| bob@example.com | Password123! | Bob Smith |
| carol@example.com | Password123! | Carol Williams |
| david@example.com | Password123! | David Brown |
| eve@example.com | Password123! | Eve Davis |

## ?? Database Status

Your local PostgreSQL database is connected and populated:

```
? Host: localhost:5432
? Database: postgres
? 29 Tables created
? 5 Users seeded
? 10 Birds seeded
? 13 Stories seeded
? 22 Support Transactions seeded
? 19 Love relationships seeded
? 6 Notifications seeded
? 5 Invoices seeded
? 8 Crypto Payment Requests seeded
? 4 Supported Tokens configured
```

## ?? Background Jobs (Hangfire)

The following background jobs are configured and running:

1. **ExchangeRateUpdateJob** - Updates crypto exchange rates
2. **PaymentMonitorJob** - Monitors pending crypto payments
3. **ReconciliationJob** - Reconciles payments with blockchain
4. **NotificationCleanupJob** - Cleans old notifications
5. **DailyDigestJob** - Sends daily notification digests
6. **PremiumExpiryNotificationJob** - Notifies about expiring subscriptions
7. **CharityAllocationJob** - Processes charity allocations

View them at: http://localhost:5162/hangfire

## ?? Available API Endpoints

### Authentication (`/api/auth`)
- `GET /api/auth` - Health check
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh-token` - Refresh access token
- `POST /api/auth/logout` - Logout (requires auth)
- `POST /api/auth/confirm-email` - Confirm email
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password
- `POST /api/auth/change-password` - Change password (requires auth)
- `GET /api/auth/validate` - Validate token (requires auth)

### Birds (`/api/birds`)
- `GET /api/birds` - List all birds (requires auth)
- `GET /api/birds/{id}` - Get bird details (requires auth)
- `POST /api/birds` - Create bird (requires auth)
- `PUT /api/birds/{id}` - Update bird (requires auth)
- `DELETE /api/birds/{id}` - Delete bird (requires auth)
- `POST /api/birds/{id}/love` - Love a bird (requires auth)
- `DELETE /api/birds/{id}/love` - Unlike a bird (requires auth)

### Stories (`/api/stories`)
- `GET /api/birds/{birdId}/stories` - List bird stories
- `POST /api/birds/{birdId}/stories` - Create story (requires auth)
- `PUT /api/stories/{id}` - Update story (requires auth)
- `DELETE /api/stories/{id}` - Delete story (requires auth)

### Support (`/api/support`)
- `POST /api/birds/{birdId}/support` - Support a bird (requires auth)
- `GET /api/birds/{birdId}/supporters` - List supporters

### Crypto Payments (`/api/crypto-payments`)
- `POST /api/crypto-payments/request` - Create payment request
- `GET /api/crypto-payments/{id}` - Get payment status
- `GET /api/crypto-payments/rates` - Get exchange rates
- `GET /api/crypto-payments/supported-tokens` - List supported tokens

### Invoices (`/api/invoices`)
- `POST /api/invoices` - Create invoice (requires auth)
- `GET /api/invoices/{id}` - Get invoice details (requires auth)
- `GET /api/invoices` - List user invoices (requires auth)

### Notifications (`/api/notifications`)
- `GET /api/notifications` - List user notifications (requires auth)
- `PUT /api/notifications/{id}/read` - Mark as read (requires auth)
- `PUT /api/notifications/read-all` - Mark all as read (requires auth)

## ?? Example Workflow

### 1. Register a New User
```http
POST http://localhost:5162/api/auth/register
Content-Type: application/json

{
  "name": "Test User",
  "email": "test@example.com",
  "password": "SecurePass123!"
}
```

### 2. Login
```http
POST http://localhost:5162/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "SecurePass123!"
}
```

Response will include:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123...",
  "expiresAt": "2024-01-...",
  "userId": "guid",
  "name": "Test User",
  "email": "test@example.com",
  "emailConfirmed": false
}
```

### 3. Get Birds (with token)
```http
GET http://localhost:5162/api/birds
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### 4. Love a Bird
```http
POST http://localhost:5162/api/birds/{birdId}/love
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### 5. Create a Story
```http
POST http://localhost:5162/api/birds/{birdId}/stories
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "content": "Today I saw this beautiful bird at my feeder!"
}
```

## ??? Development Tools

### View Logs
Watch the console where you ran `dotnet run` for real-time logs.

### Hot Reload
The application supports hot reload - make code changes and they'll be applied automatically (in most cases).

### Debug in Visual Studio
1. Stop the current `dotnet run` process (Ctrl+C)
2. Open Visual Studio
3. Press F5 to start debugging
4. Set breakpoints as needed

## ?? Database Queries

### Connect to Database
```bash
psql -h localhost -U postgres -d postgres
```

### View Tables
```sql
\dt
```

### Query Users
```sql
SELECT user_id, name, email, created_at FROM users;
```

### Query Birds with Owners
```sql
SELECT 
    b.name as bird_name,
    b.species,
    u.name as owner_name,
    b.loved_count,
    b.donation_cents / 100.0 as donation_dollars
FROM birds b
JOIN users u ON b.owner_id = u.user_id
ORDER BY b.created_at;
```

## ?? Stop and Restart

### Stop the Application
Press `Ctrl+C` in the terminal where the app is running.

### Restart the Application
```bash
dotnet run
```

### Reset Database (if needed)
```bash
.\reset-database.bat
```

## ?? Documentation Files

All documentation is in your workspace:

1. **README-DATABASE.md** - Database setup guide (this file)
2. **DATABASE_SETUP_SUMMARY.md** - Complete database documentation
3. **Database/schema-documentation.sql** - SQL schema reference
4. **test-api.ps1** - API testing script
5. **reset-database.bat** - Database reset script

## ?? You're All Set!

Your Wihngo API is fully operational with:
- ? Database connected and populated
- ? All 29 tables created
- ? Development seed data loaded
- ? API endpoints ready
- ? Swagger documentation available
- ? Hangfire background jobs running
- ? Authentication system configured
- ? Crypto payment system ready

**Start developing!** ??

---

### Quick Test
Open your browser and go to:
?? http://localhost:5162/swagger

Try logging in with:
- **Email:** alice@example.com
- **Password:** Password123!
