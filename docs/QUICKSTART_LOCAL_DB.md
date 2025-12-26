# ?? QUICKSTART - Local Database Setup

## ? Super Quick (30 seconds)

### Step 1: Run Setup
Double-click this file:
```
setup-local-database.bat
```

### Step 2: Restart App
- Stop debugger: `Shift+F5`
- Start debugger: `F5`

### Step 3: Verify
Console should show:
```
? Database connection successful on attempt 1!
?? Database: ? Connected
```

**Done!** ??

---

## ?? Alternative: Manual Docker Setup

```powershell
# Run PostgreSQL in Docker
docker run -d --name wihngo-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=wihngo -p 5432:5432 postgres:14

# Wait 5 seconds
timeout /t 5

# Restart your app (F5)
```

---

## ? What You Get

- **Host:** localhost
- **Port:** 5432
- **Database:** wihngo
- **Username:** postgres
- **Password:** postgres

Your `appsettings.Development.json` is already configured correctly!

---

## ?? Test It

```powershell
# Health check
curl http://localhost:5000/health

# Expected: {"status":"healthy","database":"connected"}
```

---

## ?? More Info

- Full guide: `LOCAL_DATABASE_COMPLETE_GUIDE.md`
- Troubleshooting: `TROUBLESHOOTING.md`
- Original setup: `QUICK_START_LOCAL_DATABASE.md`

---

## ?? Daily Usage

### Start work:
```powershell
docker start wihngo-postgres  # if using Docker
dotnet run  # or press F5
```

### Stop work:
```powershell
# Just stop your app (Ctrl+C or stop debugger)
# Database can keep running
```

### Reset database:
```powershell
docker stop wihngo-postgres
docker rm wihngo-postgres
.\setup-local-database.bat
# Restart app
```

---

## ?? Problems?

### "Docker not found"
Install Docker Desktop: https://www.docker.com/products/docker-desktop/

### "Port 5432 in use"
```powershell
# Use different port
docker run -d --name wihngo-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=wihngo -p 5433:5432 postgres:14

# Update appsettings.Development.json:
# "DefaultConnection": "Host=localhost;Port=5433;Database=wihngo;..."
```

### "Still connecting to Render.com"
Check that you stopped the app before making changes. Hot reload doesn't always work for connection strings.

---

**That's it! Run the script and you're done.** ??
