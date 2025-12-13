# ?? Local Database Setup - Complete Guide

## What This Does

Creates a fresh PostgreSQL database on your local machine (localhost) for development, completely separate from your Render.com production database.

## ? Quick Start (Recommended)

### Option 1: Automated Script (Easiest)

**Just double-click:**
```
setup-local-database.bat
```

This script will:
- ? Detect if you have Docker or PostgreSQL installed
- ? Create/start a local `wihngo` database
- ? Test the connection
- ? Show you the connection details

**That's it!** Then restart your app (F5).

---

## ?? Option 2: Docker (Recommended for Development)

### Prerequisites
- Docker Desktop installed and running
- https://www.docker.com/products/docker-desktop/

### Setup Commands

```powershell
# Create and start PostgreSQL container
docker run -d \
  --name wihngo-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=wihngo \
  -p 5432:5432 \
  postgres:14

# Wait a few seconds for startup
timeout /t 5

# Test connection
docker exec wihngo-postgres psql -U postgres -d wihngo -c "SELECT version();"
```

### Daily Usage

```powershell
# Start database (if stopped)
docker start wihngo-postgres

# Stop database
docker stop wihngo-postgres

# View logs
docker logs wihngo-postgres

# Connect with psql
docker exec -it wihngo-postgres psql -U postgres -d wihngo

# Remove database (WARNING: deletes all data)
docker stop wihngo-postgres
docker rm wihngo-postgres
```

---

## ?? Option 3: Local PostgreSQL Installation

### Prerequisites
- PostgreSQL installed locally
- Download: https://www.postgresql.org/download/windows/

### Setup Steps

1. **Install PostgreSQL** (if not already installed)
   - Download installer from link above
   - During installation, set password to `postgres`
   - Accept default port `5432`

2. **Create Database**

   Open **Command Prompt** or **PowerShell**:

   ```cmd
   # Set password
   set PGPASSWORD=postgres

   # Create database
   "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -c "CREATE DATABASE wihngo OWNER postgres;"
   ```

   > Note: Adjust PostgreSQL version (18) if you have a different version

3. **Verify**

   ```cmd
   "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -d wihngo -c "SELECT version();"
   ```

---

## ?? Application Configuration

Your `appsettings.Development.json` should already have:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres"
  }
}
```

If not, update it with the above connection string.

---

## ? Verification

### 1. Run the setup script
```cmd
setup-local-database.bat
```

You should see:
```
? SUCCESS! Database is ready

Database Details:
  Host:     localhost
  Port:     5432
  Database: wihngo
  Username: postgres
  Password: postgres
```

### 2. Restart Your Application

**In Visual Studio:**
- Press `Shift+F5` (Stop)
- Press `F5` (Start)

**Or in terminal:**
```powershell
dotnet run
```

### 3. Check Console Output

You should see:
```
???????????????????????????????????????????????
?? DATABASE CONNECTION DIAGNOSTICS
???????????????????????????????????????????????
?? Host: localhost
?? Port: 5432
?? Database: wihngo
?? Username: postgres
?? Password: ***configured***
?? SSL Mode: none
???????????????????????????????????????????????

?? Testing database connection...
? Database connection successful on attempt 1!
?? PostgreSQL Version: PostgreSQL 14.x...

[10:30:45] Checking database connection...
[10:30:45] ? Database connection verified
[10:30:45] ? Database created successfully!
[10:30:45] ? Invoice sequence created
[10:30:45] ? Seeded 4 supported tokens
[10:30:45] ? Database seeding complete

?? Database: ? Connected
```

### 4. Test API Endpoints

```powershell
# Health check
curl http://localhost:5000/health

# Expected response:
# {
#   "status": "healthy",
#   "database": "connected"
# }
```

---

## ??? Database Schema

When your app starts with a fresh database, it will automatically:

1. **Create all tables** using Entity Framework migrations
2. **Create sequences** (like `wihngo_invoice_seq`)
3. **Seed initial data** (supported tokens: USDC, EURC on Solana and Base)

Your database will have these main tables:
- `users` - User accounts
- `birds` - Bird profiles
- `loves` - User likes
- `support_transactions` - Donations
- `crypto_payment_requests` - Crypto payments
- `token_configurations` - Payment token settings
- `onchain_deposits` - Blockchain deposits
- `invoices` - Invoice system
- `notifications` - User notifications
- And many more...

---

## ?? Starting Fresh

If you want to **completely reset** your local database:

### Docker Method:
```powershell
docker stop wihngo-postgres
docker rm wihngo-postgres
.\setup-local-database.bat
```

### Local PostgreSQL Method:
```cmd
set PGPASSWORD=postgres
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -c "DROP DATABASE IF EXISTS wihngo;"
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -c "CREATE DATABASE wihngo OWNER postgres;"
```

Then restart your app - it will recreate everything.

---

## ?? Connecting to Database

### Using psql (Command Line)

**Docker:**
```powershell
docker exec -it wihngo-postgres psql -U postgres -d wihngo
```

**Local PostgreSQL:**
```cmd
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -d wihngo
```

### Using pgAdmin (GUI)

1. Open pgAdmin 4
2. Add New Server:
   - **Name:** Wihngo Local
   - **Host:** localhost
   - **Port:** 5432
   - **Database:** wihngo
   - **Username:** postgres
   - **Password:** postgres

### Using DBeaver or DataGrip

**Connection Details:**
- **Driver:** PostgreSQL
- **Host:** localhost
- **Port:** 5432
- **Database:** wihngo
- **Username:** postgres
- **Password:** postgres

---

## ?? Useful SQL Queries

```sql
-- Show all tables
\dt

-- Show table structure
\d users
\d birds
\d crypto_payment_requests

-- Count records
SELECT COUNT(*) FROM users;
SELECT COUNT(*) FROM birds;

-- View recent users
SELECT user_id, name, email, created_at 
FROM users 
ORDER BY created_at DESC 
LIMIT 10;

-- View database size
SELECT pg_size_pretty(pg_database_size('wihngo'));
```

---

## ?? Troubleshooting

### "Port 5432 is already in use"

**Check what's using the port:**
```powershell
netstat -ano | findstr :5432
```

**Solution 1: Stop other PostgreSQL**
```powershell
net stop postgresql-x64-18
```

**Solution 2: Use different port**
```powershell
# For Docker
docker run -d \
  --name wihngo-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=wihngo \
  -p 5433:5432 \
  postgres:14
```

Then update `appsettings.Development.json`:
```json
"DefaultConnection": "Host=localhost;Port=5433;Database=wihngo;Username=postgres;Password=postgres"
```

### "Database connection failed"

**Check if PostgreSQL is running:**

**Docker:**
```powershell
docker ps | findstr wihngo-postgres
```

**Windows Service:**
```powershell
sc query postgresql-x64-18
```

**Start if stopped:**
```powershell
# Docker
docker start wihngo-postgres

# Windows Service
net start postgresql-x64-18
```

### "Application still connects to Render.com"

Make sure `appsettings.Development.json` has localhost connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres"
  }
}
```

And check that no environment variables are overriding it:
```powershell
# Check environment variables
$env:ConnectionStrings__DefaultConnection
$env:DEFAULT_CONNECTION

# Clear them if set
$env:ConnectionStrings__DefaultConnection = $null
$env:DEFAULT_CONNECTION = $null
```

---

## ?? Backup & Restore

### Create Backup

**Docker:**
```powershell
docker exec wihngo-postgres pg_dump -U postgres wihngo > backup.sql
```

**Local PostgreSQL:**
```cmd
"C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" -U postgres -h localhost wihngo > backup.sql
```

### Restore Backup

**Docker:**
```powershell
cat backup.sql | docker exec -i wihngo-postgres psql -U postgres -d wihngo
```

**Local PostgreSQL:**
```cmd
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -d wihngo < backup.sql
```

---

## ?? Benefits of Local Database

? **Fast** - No network latency  
? **Free** - No cloud costs  
? **Reliable** - Always available  
? **Offline** - Works without internet  
? **Safe** - Can't accidentally affect production  
? **Full Control** - Can reset anytime  
? **Better for Development** - Faster iteration

---

## ?? Switching Between Databases

You can easily switch between local and production databases by changing `appsettings.Development.json`:

**Local Development:**
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres"
```

**Production/Render.com:**
```json
"DefaultConnection": "Host=***REMOVED***;Port=5432;Database=wihngo_kzno;Username=wihngo;Password=***REMOVED***;SSL Mode=Require;Trust Server Certificate=true"
```

> ?? **Warning:** Be very careful when connecting to production! Always use local database for development.

---

## ?? Daily Development Workflow

### Morning (Start Work)

```powershell
# 1. Start database (if using Docker)
docker start wihngo-postgres

# 2. Start your app
dotnet run
# or press F5 in Visual Studio
```

### Evening (End Work)

```powershell
# Just stop your app (Ctrl+C or stop debugger)
# PostgreSQL can keep running in background
```

### Weekly (Fresh Start)

```powershell
# Reset database for clean slate
docker stop wihngo-postgres
docker rm wihngo-postgres
.\setup-local-database.bat

# Restart app - creates fresh schema with seed data
```

---

## ? Success Checklist

- [ ] PostgreSQL running (Docker or local)
- [ ] Database `wihngo` created
- [ ] `appsettings.Development.json` configured
- [ ] Application starts without errors
- [ ] Console shows "? Database connection successful"
- [ ] `/health` endpoint returns `"database": "connected"`
- [ ] Can register a user
- [ ] Hangfire dashboard loads at `/hangfire`

---

## ?? Need Help?

If you're still having issues:

1. **Run the automated script first:**
   ```
   setup-local-database.bat
   ```

2. **Check the diagnostic output** when starting your app

3. **Review these files:**
   - `TROUBLESHOOTING.md` - Common issues
   - `DATABASE_FIX_INSTRUCTIONS.md` - Technical details
   - `SOLUTION_SUMMARY.md` - Complete overview

4. **Verify PostgreSQL is actually running:**
   ```powershell
   # Docker
   docker ps | findstr wihngo
   
   # Windows Service
   sc query postgresql-x64-18
   ```

---

## ?? You're All Set!

Just run:
```
setup-local-database.bat
```

Then restart your app (F5) and you're ready to develop with a local database! ??
