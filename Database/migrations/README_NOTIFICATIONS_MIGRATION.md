# ?? Wihngo Notifications System - Database Migration

## ? Single Script Execution

Execute this **ONE FILE** to set up the complete notifications system:

**File:** `Database/migrations/notifications_system.sql`

---

## ?? Execution Instructions

### Option 1: Command Line (psql)

```bash
# Windows PowerShell
$env:PGPASSWORD='your_password'; psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql

# Linux/Mac
PGPASSWORD='your_password' psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql

# Or without password in environment (will prompt)
psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql
```

### Option 2: pgAdmin (GUI)

1. Open **pgAdmin**
2. Connect to your PostgreSQL server
3. Navigate to: **Servers ? Your Server ? Databases ? wihngo**
4. Right-click on **wihngo** ? **Query Tool**
5. Click **Open File** (folder icon) ? Select `Database/migrations/notifications_system.sql`
6. Click **Execute** (?? play button) or press **F5**
7. Check the **Messages** tab for success confirmation

### Option 3: Visual Studio Code (SQL Extension)

1. Install **PostgreSQL** extension (if not installed)
2. Open `Database/migrations/notifications_system.sql`
3. Right-click ? **Run Query**
4. Or use command palette: `PostgreSQL: Execute Query`

---

## ? What This Script Does

### Tables Created:
- ? `notifications` - Stores all notifications
- ? `notification_preferences` - User preferences per notification type
- ? `notification_settings` - Global user notification settings
- ? `user_devices` - Push notification device tokens

### Indexes Created (16 total):
- Performance indexes on user_id, is_read, created_at, etc.
- Composite indexes for optimal query performance

### Default Data Initialized:
- ? All 15 notification types for each existing user
- ? Default notification settings for each user
- ? Proper email preferences (enabled for high-priority only)

### Triggers Created:
- ? Auto-update `updated_at` timestamps on record changes

---

## ?? Verification

The script will output verification results like:

```
NOTICE: Found 10 existing users. Initializing notification preferences...
NOTICE: Created 140 notification preferences for 10 users
NOTICE: Created 10 notification settings records
NOTICE: Total indexes created: 16
NOTICE: MIGRATION COMPLETED SUCCESSFULLY!
```

---

## ?? Test After Migration

### 1. Restart Your Application
```bash
# Stop and restart your .NET application
dotnet run
```

### 2. Check Hangfire Dashboard
Visit: `http://localhost:5000/hangfire`

Look for these new jobs:
- ? `cleanup-notifications` (Daily at 2 AM)
- ? `send-daily-digests` (Every hour)
- ? `check-premium-expiry` (Daily at 10 AM)

### 3. Test API Endpoints

```bash
# Get notifications
curl -X GET "http://localhost:5000/api/notifications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Send test notification
curl -X POST "http://localhost:5000/api/notifications/test" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Register device for push
curl -X POST "http://localhost:5000/api/notifications/devices" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pushToken": "ExponentPushToken[xxxxxx]",
    "deviceType": "ios",
    "deviceName": "iPhone 14"
  }'
```

---

## ?? Connection Parameters

Update these if your database is not on localhost:

- **Host**: `-h localhost` ? Change to your DB host
- **User**: `-U postgres` ? Change to your DB user
- **Database**: `-d wihngo` ? Your database name
- **Port**: Default 5432 (add `-p 5433` if different)

---

## ?? Troubleshooting

### "relation already exists"
Tables were already created. To re-run:
```sql
DROP TABLE IF EXISTS notifications CASCADE;
DROP TABLE IF EXISTS notification_preferences CASCADE;
DROP TABLE IF EXISTS notification_settings CASCADE;
DROP TABLE IF EXISTS user_devices CASCADE;
-- Then re-run the migration script
```

### "permission denied"
Ensure your database user has CREATE privileges:
```sql
GRANT CREATE ON DATABASE wihngo TO your_user;
```

### Script hangs or fails
- Check PostgreSQL is running
- Verify connection string
- Check database exists: `\l` in psql
- Check users table exists: `\dt` in psql

---

## ?? Expected Results

After successful execution:

| Table | Expected Rows |
|-------|---------------|
| `notifications` | 0 (empty, will fill as notifications are sent) |
| `notification_preferences` | `(number_of_users × 15)` |
| `notification_settings` | `(number_of_users)` |
| `user_devices` | 0 (will fill as devices register) |

---

## ?? Success!

Once executed successfully:
1. ? All notification tables created
2. ? All indexes optimized
3. ? Existing users initialized with preferences
4. ? Background jobs scheduled
5. ? API endpoints ready to use

**Ready to send notifications!** ??

---

## ?? Need Help?

- Check application logs for errors
- Verify Hangfire dashboard is accessible
- Test with `/api/notifications/test` endpoint
- Check PostgreSQL logs if migration fails
