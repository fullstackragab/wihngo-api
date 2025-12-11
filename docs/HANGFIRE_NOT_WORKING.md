# ?? Hangfire Dashboard Not Working - Troubleshooting

## Problem
Cannot access Hangfire dashboard at `http://localhost:5000/hangfire`

---

## ?? Common Causes & Quick Fixes

### 1?? Wrong Port Number

**Issue:** Your app might be running on a different port.

**Check Console Output:**
Look for this line when you start the app:
```
Now listening on: http://localhost:XXXX
```

**Fix:**
Use the correct port from the console output:
- If it says `http://localhost:5000` ? Use `http://localhost:5000/hangfire`
- If it says `http://localhost:5139` ? Use `http://localhost:5139/hangfire`
- If it says `https://localhost:7001` ? Use `https://localhost:7001/hangfire`

---

### 2?? HTTPS Redirect Issue

**Issue:** App redirects HTTP to HTTPS automatically.

**Current Code Has:**
```csharp
app.UseHttpsRedirection();
```

**Try These URLs:**
1. `https://localhost:7xxx/hangfire` (use HTTPS)
2. `http://localhost:5xxx/hangfire` (use HTTP)

**Check Properties/launchSettings.json** for the correct ports:
```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5000"
    },
    "https": {
      "applicationUrl": "https://localhost:7001;http://localhost:5000"
    }
  }
}
```

---

### 3?? Middleware Order Issue

**Issue:** `UseHangfireDashboard` might be in the wrong position.

**Current Order (CORRECT):**
```csharp
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", ...);  // ? Correct position
app.MapControllers();
```

This is already correct in your code! ?

---

### 4?? Database Connection Issue

**Issue:** Hangfire can't connect to PostgreSQL.

**Check Console for Errors:**
Look for error messages like:
```
Failed to connect to database
PostgreSqlDistributedLockException
```

**Fix:**
1. **Verify PostgreSQL is running:**
   ```powershell
   # Windows
   Get-Service postgresql*
   
   # Or check task manager for "postgres.exe"
   ```

2. **Test connection string:**
   ```csharp
   // Default in your code:
   Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres
   ```

3. **Verify database exists:**
   ```sql
   psql -U postgres -l
   # Should list "wihngo" database
   ```

---

### 5?? Hangfire Tables Not Created

**Issue:** Hangfire schema doesn't exist in database.

**Check Database:**
```sql
-- Connect to wihngo database
psql -U postgres -d wihngo

-- Check for Hangfire tables
\dt hangfire.*

-- Should see tables like:
-- hangfire.job
-- hangfire.state
-- hangfire.jobqueue
```

**Fix:**
Delete and recreate Hangfire schema:
```sql
-- BE CAREFUL: This will delete Hangfire data!
DROP SCHEMA IF EXISTS hangfire CASCADE;
```

Then restart your app. Hangfire will recreate the schema automatically (due to `PrepareSchemaIfNecessary = true`).

---

### 6?? Logging Suppressed Hangfire Errors

**Issue:** Hangfire errors are being hidden by crypto-only logging!

**Your Current Config:**
```csharp
builder.Logging.AddFilter("Hangfire", LogLevel.None);  // ? Hiding errors!
```

**Temporary Fix:** Enable Hangfire logs to see errors:

---

## ? Step-by-Step Diagnostic

### Step 1: Check What Port Your App is Running On

1. Start your app:
   ```sh
   dotnet run
   ```

2. Look at the console output - find this line:
   ```
   Now listening on: http://localhost:XXXX
   ```

3. Use that exact URL with `/hangfire`:
   ```
   http://localhost:XXXX/hangfire
   ```

---

### Step 2: Enable Hangfire Logging (Temporary)

Modify `Program.cs` **temporarily** to see Hangfire errors:

```csharp
// TEMPORARY: Enable Hangfire logs for debugging
builder.Logging.AddFilter("Hangfire", LogLevel.Warning);  // Changed from None
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);  // Show ASP.NET errors
```

Restart your app and look for Hangfire-related errors.

---

### Step 3: Check Database Connection

1. **Verify PostgreSQL is running:**
   ```powershell
   # Try connecting
   psql -U postgres
   ```

2. **Check if database exists:**
   ```sql
   \l
   -- Should see "wihngo" in the list
   ```

3. **Check Hangfire schema:**
   ```sql
   \c wihngo
   \dt hangfire.*
   ```

---

### Step 4: Test Direct Route

Add a test endpoint to verify routing works:

```csharp
// Add this right before app.Run() in Program.cs
app.MapGet("/hangfire-test", () => "Hangfire routing works!");
```

Then test:
- `http://localhost:XXXX/hangfire-test` should return "Hangfire routing works!"
- If this works but `/hangfire` doesn't ? Hangfire initialization issue

---

## ?? Quick Fix Code Changes

### Option 1: Enable Hangfire Logs Temporarily

Update your logging configuration:

```csharp
// TEMPORARY: Enable Hangfire and ASP.NET logs for debugging
builder.Logging.AddFilter("Hangfire", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
```

**After fixing, revert to:**
```csharp
builder.Logging.AddFilter("Hangfire", LogLevel.None);
```

---

### Option 2: Add Hangfire Startup Logging

Add this right after `var app = builder.Build();`:

```csharp
var app = builder.Build();

// ADD THIS: Test Hangfire connection
try
{
    using var scope = app.Services.CreateScope();
    var hangfireStorage = scope.ServiceProvider.GetService<Hangfire.Storage.JobStorage>();
    Console.WriteLine("? Hangfire storage initialized successfully");
    Console.WriteLine($"   Storage type: {hangfireStorage?.GetType().Name}");
}
catch (Exception ex)
{
    Console.WriteLine("? HANGFIRE INITIALIZATION FAILED!");
    Console.WriteLine($"   Error: {ex.Message}");
    Console.WriteLine($"   Make sure PostgreSQL is running and database 'wihngo' exists");
}
```

---

### Option 3: Add Test Endpoint

Add this before `app.Run()`:

```csharp
// Test endpoint to verify app is running
app.MapGet("/test", () => new
{
    status = "OK",
    timestamp = DateTime.UtcNow,
    message = "Backend is running",
    hangfireDashboard = "/hangfire"
});
```

Test: `http://localhost:XXXX/test`

---

## ?? Common Error Messages

### Error 1: "Cannot connect to PostgreSQL"
```
Npgsql.NpgsqlException: Failed to connect to server
```

**Fix:**
- Start PostgreSQL service
- Check connection string in `appsettings.json`

---

### Error 2: "Failed to acquire distributed lock"
```
PostgreSqlDistributedLockException
```

**Fix:**
- Your code already handles this with retry logic ?
- If persists, restart PostgreSQL

---

### Error 3: "404 Not Found" when accessing /hangfire
```
HTTP 404
```

**Causes:**
1. Wrong port number
2. App not running
3. Hangfire middleware not registered (but your code looks correct)

**Fix:**
- Double-check the port from console output
- Try `https://` instead of `http://`
- Check if `UseHangfireDashboard` is being called

---

## ?? Expected Console Output

When app starts correctly, you should see:

```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.

[HH:mm:ss] info: === MONITORING PENDING PAYMENTS ===
[HH:mm:ss] info: Found 0 payments with transaction hash to monitor
[HH:mm:ss] info: === PAYMENT MONITORING COMPLETED ===
```

Then you can access: `http://localhost:5000/hangfire`

---

## ?? Most Likely Issues (Ranked)

1. **Wrong Port** (80% of cases)
   - App is on port 5139 but you're trying port 5000
   - Use the port from console output

2. **HTTPS Redirect** (10%)
   - Try `https://` instead of `http://`

3. **Database Connection** (5%)
   - PostgreSQL not running
   - Database doesn't exist

4. **Logging Hiding Errors** (5%)
   - Hangfire logs are suppressed
   - Enable temporarily to see errors

---

## ? Verification Steps

After trying fixes:

1. ? Backend running (check console)
2. ? Correct port identified
3. ? PostgreSQL running
4. ? Database "wihngo" exists
5. ? No errors in console
6. ? Can access `/test` endpoint
7. ? Can access `/hangfire` dashboard

---

## ?? If Still Not Working

**Provide This Information:**

1. **Console output** (first 20 lines after starting app)
2. **Exact URL** you're trying to access
3. **Browser error** (404, 500, connection refused?)
4. **PostgreSQL status** (running or not?)
5. **Database exists?** (result of `psql -U postgres -l`)

---

**Most likely fix: Use the correct port from console output!** ??
