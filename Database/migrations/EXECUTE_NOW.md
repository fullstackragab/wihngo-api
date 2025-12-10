# ? EXECUTE THIS NOW - Complete Instructions

## ?? Single Command Execution

**Copy and paste this command in your terminal:**

### Windows PowerShell
```powershell
# Navigate to project directory
cd C:\.net\Wihngo

# Execute migration (will prompt for password)
psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql

# OR with password in command (less secure but convenient)
$env:PGPASSWORD='your_password'; psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql; $env:PGPASSWORD=$null
```

### Linux/Mac Terminal
```bash
# Navigate to project directory
cd /path/to/Wihngo

# Execute migration
PGPASSWORD='your_password' psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql
```

### pgAdmin (GUI Method)
1. **Open pgAdmin**
2. **Connect to PostgreSQL Server**
3. **Right-click on `wihngo` database** ? **Query Tool**
4. **File ? Open** ? Browse to: `C:\.net\Wihngo\Database\migrations\notifications_system.sql`
5. **Click Execute** (?? button) or press **F5**
6. **Wait for success message** in the Messages tab

---

## ?? Expected Output

You should see output like this:

```
BEGIN
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE INDEX
CREATE INDEX
... (more index creation messages)
NOTICE:  Found 10 existing users. Initializing notification preferences...
NOTICE:  Created 140 notification preferences for 10 users
NOTICE:  Created 10 notification settings records
NOTICE:  ==============================================
NOTICE:  VERIFICATION RESULTS
NOTICE:  ==============================================
NOTICE:  Table: notifications - Rows: 0
NOTICE:  Table: notification_preferences - Rows: 140
NOTICE:  Table: notification_settings - Rows: 10
NOTICE:  Table: user_devices - Rows: 0
NOTICE:  
NOTICE:  Total indexes created: 16
NOTICE:  
NOTICE:  Indexes created:
NOTICE:    - notifications.idx_notifications_bird_id
NOTICE:    - notifications.idx_notifications_created_at
... (more index names)
NOTICE:  
NOTICE:  ==============================================
NOTICE:  MIGRATION COMPLETED SUCCESSFULLY!
NOTICE:  ==============================================
NOTICE:  
NOTICE:  Next steps:
NOTICE:    1. Restart your .NET application
NOTICE:    2. Check Hangfire dashboard at /hangfire
NOTICE:    3. Test notification endpoints:
NOTICE:       - GET /api/notifications
NOTICE:       - POST /api/notifications/test
NOTICE:       - POST /api/notifications/devices
NOTICE:  
COMMIT
```

---

## ? Verification Steps

### 1. Check Tables Were Created
```sql
-- Run this in psql or pgAdmin
SELECT tablename 
FROM pg_tables 
WHERE schemaname = 'public' 
  AND tablename LIKE 'notification%' 
   OR tablename = 'user_devices';
```

**Expected result:**
```
      tablename          
-------------------------
 notifications
 notification_preferences
 notification_settings
 user_devices
(4 rows)
```

### 2. Check Data Was Initialized
```sql
-- Check notification preferences
SELECT COUNT(*) as total_preferences FROM notification_preferences;

-- Check notification settings
SELECT COUNT(*) as total_settings FROM notification_settings;

-- Should see (number_of_users * 15) preferences
-- Should see (number_of_users) settings
```

### 3. Restart Your Application
```bash
# Stop the application (Ctrl+C if running)

# Start it again
dotnet run

# OR in Visual Studio: Stop Debugging (Shift+F5) then Start (F5)
```

### 4. Test the API

#### Test 1: Get Notifications
```bash
curl -X GET "http://localhost:5000/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: {"items":[],"totalCount":0,"page":1,"pageSize":20}
```

#### Test 2: Send Test Notification
```bash
curl -X POST "http://localhost:5000/api/notifications/test" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: {"notificationId":"some-guid"}
```

#### Test 3: Get Notifications Again
```bash
curl -X GET "http://localhost:5000/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: Should now show the test notification
```

### 5. Check Hangfire Dashboard

**Open in browser:** `http://localhost:5000/hangfire`

**Look for these recurring jobs:**
- ? `cleanup-notifications` - Runs daily at 2 AM
- ? `send-daily-digests` - Runs every hour
- ? `check-premium-expiry` - Runs daily at 10 AM

---

## ?? Try It Out

### Trigger Real Notifications

#### 1. Love a Bird
```bash
# First, get a bird ID
curl -X GET "http://localhost:5000/api/birds"

# Love a bird (replace {birdId})
curl -X POST "http://localhost:5000/api/birds/{birdId}/love" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# As the bird owner, check notifications
curl -X GET "http://localhost:5000/api/notifications" \
  -H "Authorization: Bearer OWNER_JWT_TOKEN"

# Should see: "Heart [YourName] loved [BirdName]!"
```

#### 2. Support a Bird
```bash
# Donate 500 cents ($5.00) to a bird
curl -X POST "http://localhost:5000/api/birds/{birdId}/donate" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '500'

# Bird owner gets notification
# Supporter gets confirmation notification
```

#### 3. Create a Story
```bash
curl -X POST "http://localhost:5000/api/stories" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "birdId": "bird-guid-here",
    "authorId": "your-user-guid",
    "content": "My bird learned a new trick today!",
    "imageUrl": "https://example.com/image.jpg"
  }'

# All users who loved that bird get notified
```

---

## ?? Troubleshooting

### Problem: "psql: command not found"

**Solution:** Add PostgreSQL to PATH or use full path:
```bash
# Windows
"C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql

# Or install psql:
# Download from: https://www.postgresql.org/download/
```

### Problem: "relation 'users' does not exist"

**Solution:** The users table must exist first. Run your base migrations:
```bash
# Check if users table exists
psql -h localhost -U postgres -d wihngo -c "\dt users"

# If not, run base migrations first
```

### Problem: "relation already exists"

**Solution:** Tables were already created. To re-run:
```sql
-- Drop existing tables
DROP TABLE IF EXISTS notifications CASCADE;
DROP TABLE IF EXISTS notification_preferences CASCADE;
DROP TABLE IF EXISTS notification_settings CASCADE;
DROP TABLE IF EXISTS user_devices CASCADE;

-- Then re-run the migration script
```

### Problem: Application won't start after migration

**Solution:** Check for errors:
```bash
# Run with detailed logging
dotnet run --verbosity detailed

# Check specific error
# Usually: Missing service registration or DbContext issue
```

### Problem: Notifications not being created

**Checklist:**
1. ? Migration completed successfully
2. ? Application restarted
3. ? User is authenticated (valid JWT token)
4. ? INotificationService is registered in Program.cs
5. ? Check application logs for errors

---

## ?? Need Help?

### Check These First
1. **Application Logs** - Look for exceptions
2. **PostgreSQL Logs** - Check for database errors
3. **Hangfire Dashboard** - `/hangfire` - Check job status
4. **API Response** - Check error messages in responses

### Common Issues
- **401 Unauthorized**: Invalid or missing JWT token
- **500 Internal Server Error**: Check application logs
- **404 Not Found**: Wrong endpoint URL
- **Connection refused**: PostgreSQL not running

### Verification Commands
```bash
# Check PostgreSQL is running
pg_isready -h localhost

# Check database exists
psql -h localhost -U postgres -c "\l" | grep wihngo

# Check application is running
curl http://localhost:5000/health

# Check Hangfire
curl http://localhost:5000/hangfire
```

---

## ?? Success Indicators

? **SQL script completes without errors**  
? **4 tables created with correct schemas**  
? **16 indexes created**  
? **Users have 15 preferences each**  
? **Application starts without errors**  
? **Swagger shows notification endpoints**  
? **Hangfire shows 3 new jobs**  
? **Test notification works**  
? **Real actions create notifications**  
? **Preferences can be updated**  

---

## ?? You're Done!

Once all verification steps pass, your notification system is **fully operational**! ??

**Start receiving notifications by:**
1. Loving birds
2. Supporting birds
3. Creating stories
4. Reaching milestones

**Manage notifications via:**
- `/api/notifications` - View all
- `/api/notifications/preferences` - Update preferences
- `/api/notifications/settings` - Configure quiet hours, limits, etc.
- `/api/notifications/devices` - Register push notification tokens

---

## ?? Documentation

For more details, see:
- **QUICK_START.md** - 2-minute setup guide
- **README_NOTIFICATIONS_MIGRATION.md** - Detailed documentation
- **NOTIFICATION_TYPES_REFERENCE.md** - All notification types
- **PACKAGE_SUMMARY.md** - Complete package overview

---

**Happy Notifying! ???**
