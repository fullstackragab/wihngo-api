# ?? Quick Start: Local PostgreSQL Setup

## Option 1: Docker (FASTEST - Recommended)

### Step 1: Start PostgreSQL Container

Open PowerShell or CMD and run:

```powershell
docker run -d `
  --name wihngo-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_DB=wihngo `
  -p 5432:5432 `
  postgres:14
```

**Linux/Mac:**
```bash
docker run -d \
  --name wihngo-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=wihngo \
  -p 5432:5432 \
  postgres:14
```

### Step 2: Verify Container is Running

```powershell
docker ps
```

You should see `wihngo-postgres` in the list.

### Step 3: Restart Your Application

Stop your current app (Ctrl+C) and run:

```powershell
dotnet run
```

You should see:
```
? Database connection successful on attempt 1!
?? PostgreSQL Version: PostgreSQL 14.x...
```

### Docker Commands (for later)

**Stop database:**
```powershell
docker stop wihngo-postgres
```

**Start database again:**
```powershell
docker start wihngo-postgres
```

**Remove database:**
```powershell
docker stop wihngo-postgres
docker rm wihngo-postgres
```

**View logs:**
```powershell
docker logs wihngo-postgres
```

---

## Option 2: Install PostgreSQL Locally

### Windows

#### Using Chocolatey (Recommended)

```powershell
# Install Chocolatey if you don't have it
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Install PostgreSQL
choco install postgresql14 -y

# Refresh environment
refreshenv
```

#### Using Official Installer

1. Download from: https://www.postgresql.org/download/windows/
2. Run the installer
3. Set password: `postgres`
4. Accept default port: `5432`
5. Complete installation

### Mac

```bash
# Using Homebrew
brew install postgresql@14

# Start PostgreSQL
brew services start postgresql@14

# Create database
createdb wihngo
```

### Linux (Ubuntu/Debian)

```bash
# Install PostgreSQL
sudo apt update
sudo apt install postgresql postgresql-contrib -y

# Start service
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Create user and database
sudo -u postgres psql -c "CREATE USER postgres WITH PASSWORD 'postgres' SUPERUSER;"
sudo -u postgres psql -c "CREATE DATABASE wihngo OWNER postgres;"
```

---

## ? Verify Installation

### Test Connection with psql

```powershell
psql -h localhost -U postgres -d wihngo
```

Enter password: `postgres`

If successful, you'll see:
```
wihngo=#
```

Type `\q` to exit.

### Test from Your Application

1. Stop your app (Ctrl+C)
2. Run: `dotnet run`
3. Look for:

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
```

---

## ?? Troubleshooting

### Error: "docker: command not found"

You need to install Docker Desktop:
- **Windows/Mac:** https://www.docker.com/products/docker-desktop/
- After installation, restart your computer

### Error: "Port 5432 is already in use"

Something else is using PostgreSQL's port.

**Option A: Stop existing PostgreSQL**
```powershell
# Windows
net stop postgresql-x64-14

# Mac
brew services stop postgresql

# Linux
sudo systemctl stop postgresql
```

**Option B: Use different port**

Change docker command:
```powershell
docker run -d `
  --name wihngo-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_DB=wihngo `
  -p 5433:5432 `
  postgres:14
```

And update `appsettings.Development.json`:
```json
"DefaultConnection": "Host=localhost;Port=5433;Database=wihngo;Username=postgres;Password=postgres"
```

### Error: "Connection refused"

PostgreSQL isn't running.

**Docker:**
```powershell
docker start wihngo-postgres
```

**Windows Service:**
```powershell
net start postgresql-x64-14
```

**Mac:**
```bash
brew services start postgresql@14
```

**Linux:**
```bash
sudo systemctl start postgresql
```

---

## ?? What About Your Render.com Database?

The error "Attempted to read past the end of the stream" means:

1. **Most likely:** Your Render.com free database expired (free tier = 90 days)
2. **Or:** The database was paused/suspended
3. **Or:** Network issues preventing connection

### Check Render.com Status

1. Go to https://dashboard.render.com
2. Sign in
3. Look for your PostgreSQL instance
4. Check if it shows "Expired", "Suspended", or "Inactive"

### If You Want to Keep Using Render.com

#### Option A: Upgrade to Paid Plan
- Render.com free PostgreSQL expires after 90 days
- Paid plans start at $7/month

#### Option B: Create New Free Instance
- Delete old database
- Create new one
- Update connection string
- Migrate your data

### Recommendation

**For Development:** Use local PostgreSQL (Docker)
- ? Free forever
- ? Faster (no network latency)
- ? Works offline
- ? Complete control

**For Production:** Use managed service
- Render.com (paid)
- AWS RDS
- DigitalOcean Managed Database
- Supabase
- Neon

---

## ?? Quick Summary

**Right now, do this:**

1. **Install Docker Desktop** (if not installed)
2. **Run this command:**
   ```powershell
   docker run -d --name wihngo-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=wihngo -p 5432:5432 postgres:14
   ```
3. **Restart your application:**
   ```powershell
   dotnet run
   ```

That's it! You should see:
```
? Database connection successful on attempt 1!
?? APPLICATION STARTED
?? Database: ? Connected
```

Your API will now work perfectly with a local database.
