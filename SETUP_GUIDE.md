# ?? Quick Setup Guide - Crypto Payment Backend

## Prerequisites

- .NET 10 SDK installed
- PostgreSQL database running
- Git (to clone/push)

## Step 1: Configure Database Connection

Update `appsettings.json` (or create from `appsettings.Example.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_HOST;Port=5432;Database=wihngo;Username=YOUR_USER;Password=YOUR_PASSWORD"
  }
}
```

## Step 2: Configure JWT Secret

**Important**: Change the default JWT secret in `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your_secure_random_secret_at_least_32_characters"
  }
}
```

Generate a secure secret:
```bash
# PowerShell
$secret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | % {[char]$_})
Write-Host $secret

# Or online: https://generate-secret.vercel.app/32
```

## Step 3: Install Dependencies

All dependencies are already in the `.csproj` file. Just restore:

```bash
dotnet restore
```

## Step 4: Setup Database

Option A - Auto-create on first run (easiest):
```bash
dotnet run
```
Database will be created automatically with all tables and seed data.

Option B - Using migrations:
```bash
dotnet ef database update
```

## Step 5: Verify Installation

### 1. Check if API is running
```bash
curl http://localhost:5000/api/auth
# Should return: "Successful"
```

### 2. Check Hangfire Dashboard
Open browser: `http://localhost:5000/hangfire`

You should see 6 scheduled jobs:
- update-exchange-rates (every 5 min)
- monitor-payments (every 1 min)
- expire-payments (every 1 hour)
- cleanup-notifications
- send-daily-digests
- check-premium-expiry

### 3. Check Exchange Rates
```bash
curl http://localhost:5000/api/payments/crypto/rates
```

Should return JSON with rates for BTC, ETH, USDT, USDC, BNB, SOL, DOGE.

## Step 6: Test Payment Creation

### 1. Create a test user
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

Save the returned `token`.

### 2. Create a payment request
```bash
curl -X POST http://localhost:5000/api/payments/crypto/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "amountUsd": 4.99,
    "currency": "USDT",
    "network": "tron",
    "purpose": "premium_subscription",
    "plan": "monthly"
  }'
```

You should get a response with:
- `walletAddress`: Where to send payment
- `qrCodeData`: For QR code generation
- `paymentUri`: For wallet apps
- `expiresAt`: Payment expiration time

## Step 7: Optional - Configure Blockchain APIs

### TronGrid API Key (Recommended)
1. Go to https://www.trongrid.io/
2. Sign up for free account
3. Get API key
4. Add to `appsettings.json`:
```json
{
  "BlockchainSettings": {
    "TronGrid": {
      "ApiKey": "your-api-key-here"
    }
  }
}
```

### Infura API Key (For Ethereum)
1. Go to https://infura.io/
2. Create free account
3. Create new project
4. Copy Project ID
5. Add to `appsettings.json`:
```json
{
  "BlockchainSettings": {
    "Infura": {
      "ProjectId": "your-project-id"
    }
  }
}
```

### CoinGecko API Key (Optional)
For higher rate limits (default: 10-30 calls/minute free):
1. Go to https://www.coingecko.com/en/api
2. Get free or paid API key
3. Add to `appsettings.json`:
```json
{
  "ExchangeRateSettings": {
    "CoinGeckoApiKey": "your-api-key"
  }
}
```

## Step 8: Update Platform Wallet (Production Only)

The default TRON wallet is for testing. For production:

1. Generate new TRON wallet
2. Update in database:
```sql
UPDATE platform_wallets 
SET address = 'YOUR_PRODUCTION_WALLET_ADDRESS'
WHERE currency = 'USDT' AND network = 'tron';
```

Or add via code in `AppDbContext.cs` seed data.

## Step 9: Deploy

### Local Development
```bash
dotnet run
```

### Docker Deployment
```bash
docker build -t wihngo-api .
docker run -p 5000:8080 -e ConnectionStrings__DefaultConnection="YOUR_DB_CONNECTION" wihngo-api
```

### Azure/Cloud Deployment
1. Configure connection string in environment variables
2. Configure JWT secret in Key Vault
3. Deploy using CI/CD pipeline
4. Set environment to Production

## Troubleshooting

### Issue: Database connection failed
**Solution**: Check PostgreSQL is running and connection string is correct.

```bash
# Test PostgreSQL connection
psql -h localhost -U postgres -d wihngo
```

### Issue: Hangfire jobs not running
**Solution**: Check Hangfire dashboard for errors. Ensure Hangfire tables were created.

### Issue: Exchange rates not updating
**Solution**: Check logs for CoinGecko API errors. Free tier has rate limits.

### Issue: JWT token invalid
**Solution**: Ensure JWT secret is configured and is at least 16 characters.

## Default Configuration

### Default Admin Credentials
None - you must create users via `/api/auth/register`

### Default Ports
- HTTP: 5000
- HTTPS: 7000

### Default Database
- Name: `wihngo`
- Tables: Auto-created with seed data

### Default Wallet (TRON)
- Currency: USDT
- Network: TRON (TRC-20)
- Address: `TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA`
- **?? Change this for production!**

## Environment Variables (Alternative to appsettings.json)

```bash
# Connection String
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres"

# JWT Secret
export Jwt__Secret="your_secure_secret_here"

# TronGrid API
export BlockchainSettings__TronGrid__ApiKey="your-key"

# Infura
export BlockchainSettings__Infura__ProjectId="your-project-id"
```

## Verification Checklist

After setup, verify:

- [ ] Database created with tables
- [ ] API responds at `/api/auth`
- [ ] Hangfire dashboard accessible at `/hangfire`
- [ ] Exchange rates endpoint returns data
- [ ] Can create user account
- [ ] Can create payment request with JWT token
- [ ] Background jobs appear in Hangfire
- [ ] Logs show no critical errors

## Next Steps

1. ? Complete setup steps above
2. ? Test payment creation
3. ? Configure production wallets
4. ? Test with small real payment
5. ? Integrate with frontend
6. ? Deploy to production

## Support

For issues or questions:
1. Check logs in console or log files
2. Check Hangfire dashboard for job errors
3. Review `CRYPTO_PAYMENT_BACKEND_COMPLETE.md` for detailed documentation
4. Check database tables for data integrity

## Security Reminders

?? **Before Production:**
- Change JWT secret
- Update wallet addresses
- Configure CORS properly
- Enable HTTPS
- Secure Hangfire dashboard
- Review rate limiting
- Set up monitoring

---

**Setup Time**: ~10 minutes  
**Last Updated**: December 11, 2025  
**Version**: 1.0.0
