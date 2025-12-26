# ? INSTANT FIX - 3 Commands

## What I Fixed
? Connection string reading in `Program.cs`  
? Local database configuration  

## What You Do Now

### Open PowerShell and run:

```powershell
# 1. Start database
.\start-db.ps1

# 2. Restart your app
# Stop debugger (Shift+F5) and start again (F5)
```

### You should see:
```
? Database connection successful on attempt 1!
```

### Test it:
```powershell
curl http://localhost:5000/health
```

## No Docker? Install it:
https://www.docker.com/products/docker-desktop/

Then run `.\start-db.ps1` again.

---

**That's literally it. Your app will now work with local PostgreSQL! ??**
