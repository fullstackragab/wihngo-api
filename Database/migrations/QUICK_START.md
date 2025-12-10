# ?? Quick Start - Wihngo Notifications System

## ? TL;DR - Execute This ONE Command

```bash
# Windows PowerShell (from project root: C:\.net\Wihngo)
$env:PGPASSWORD='your_password'; psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql
```

That's it! ??

---

## ?? Step-by-Step (3 Steps)

### Step 1: Execute the SQL Script ?? ~10 seconds

Choose your method:

#### Method A: Command Line
```bash
psql -h localhost -U postgres -d wihngo -f Database/migrations/notifications_system.sql
```

#### Method B: pgAdmin
1. Open **pgAdmin**
2. Query Tool ? Open File ? `Database/migrations/notifications_system.sql`
3. Click Execute (F5)

### Step 2: Restart Your Application ?? ~5 seconds

```bash
# Stop current instance (Ctrl+C)
# Start again
dotnet run
```

### Step 3: Test It Works ?? ~30 seconds

```bash
# Test 1: Get notifications (should return empty array)
curl http://localhost:5000/api/notifications \
  -H "Authorization: Bearer YOUR_TOKEN"

# Test 2: Send test notification
curl -X POST http://localhost:5000/api/notifications/test \
  -H "Authorization: Bearer YOUR_TOKEN"

# Test 3: Check Hangfire Dashboard
# Open: http://localhost:5000/hangfire
# Look for: cleanup-notifications, send-daily-digests, check-premium-expiry
```

? **If all three work ? You're done!**

---

## ?? What You Get

After execution, your app can:

? Send notifications when users love birds  
? Send notifications when users support birds  
? Send notifications when new stories are posted  
? Track milestone achievements (10, 50, 100, 500, 1000, 5000 loves)  
? Send push notifications via Expo  
? Send email notifications (configure SendGrid later)  
? Respect user preferences and quiet hours  
? Group similar notifications  
? Daily email digests (if user enables)  
? Auto-cleanup old notifications  
? Alert before premium expires  

---

## ?? Expected Output from SQL Script

```
NOTICE: Found 10 existing users. Initializing notification preferences...
NOTICE: Created 140 notification preferences for 10 users
NOTICE: Created 10 notification settings records
NOTICE: Total indexes created: 16
NOTICE: ==============================================
NOTICE: MIGRATION COMPLETED SUCCESSFULLY!
NOTICE: ==============================================
```

---

## ?? Quick Test Scenarios

### Scenario 1: Love a Bird
1. POST `/api/birds/{birdId}/love` (with auth token)
2. GET `/api/notifications` (as the bird owner)
3. Should see notification: "Heart [User] loved [Bird]!"

### Scenario 2: Support a Bird
1. POST `/api/birds/{birdId}/donate` with amount in cents
2. GET `/api/notifications` (as the bird owner)
3. Should see notification: "Corn [User] supported [Bird]!"

### Scenario 3: Create a Story
1. POST `/api/stories` with birdId and content
2. GET `/api/notifications` (as someone who loved that bird)
3. Should see notification: "Book New story: [Bird Name]"

### Scenario 4: Register Device for Push
```bash
curl -X POST http://localhost:5000/api/notifications/devices \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pushToken": "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
    "deviceType": "ios",
    "deviceName": "My iPhone"
  }'
```

---

## ?? If Something Goes Wrong

### Error: "relation already exists"
```sql
-- Run this first, then re-run migration:
DROP TABLE IF EXISTS notifications CASCADE;
DROP TABLE IF EXISTS notification_preferences CASCADE;
DROP TABLE IF EXISTS notification_settings CASCADE;
DROP TABLE IF EXISTS user_devices CASCADE;
```

### Error: "could not connect to server"
- Check PostgreSQL is running
- Verify connection string in `appsettings.json`
- Try: `pg_isready -h localhost`

### Error: API returns 500
- Check application logs
- Verify all services registered in `Program.cs`
- Ensure migration completed successfully

### Notifications Not Sending
1. Check Hangfire dashboard: `http://localhost:5000/hangfire`
2. Verify jobs are scheduled
3. Check application logs for errors
4. Test with `/api/notifications/test` endpoint

---

## ?? Files Included

| File | Purpose |
|------|---------|
| `notifications_system.sql` | **Main migration script** (execute this) |
| `README_NOTIFICATIONS_MIGRATION.md` | Detailed instructions |
| `NOTIFICATION_TYPES_REFERENCE.md` | Complete reference guide |
| `QUICK_START.md` | This file |

---

## ?? Success Checklist

- [ ] SQL script executed without errors
- [ ] Application restarted successfully
- [ ] Hangfire dashboard shows 3 new jobs
- [ ] `/api/notifications` returns data (or empty array)
- [ ] `/api/notifications/test` creates a test notification
- [ ] Loving a bird creates a notification
- [ ] Supporting a bird creates a notification

**All checked? Congratulations! ?? Your notifications system is live!**

---

## ?? API Endpoints Reference

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/notifications` | Get all notifications |
| GET | `/api/notifications/unread-count` | Get unread count |
| PATCH | `/api/notifications/{id}/read` | Mark as read |
| POST | `/api/notifications/mark-all-read` | Mark all as read |
| DELETE | `/api/notifications/{id}` | Delete notification |
| GET | `/api/notifications/preferences` | Get preferences |
| PUT | `/api/notifications/preferences` | Update preference |
| GET | `/api/notifications/settings` | Get settings |
| PUT | `/api/notifications/settings` | Update settings |
| POST | `/api/notifications/devices` | Register device |
| GET | `/api/notifications/devices` | Get devices |
| DELETE | `/api/notifications/devices/{id}` | Remove device |
| POST | `/api/notifications/test` | Send test notification |

---

## ?? Pro Tips

1. **Use Hangfire Dashboard** to monitor notification jobs
2. **Check user preferences** before enabling emails in production
3. **Test with `/api/notifications/test`** before going live
4. **Configure SendGrid** for production email (placeholder now)
5. **Monitor quiet hours** to avoid disturbing users at night
6. **Review grouping settings** to prevent notification spam

---

## ?? Need Help?

1. Check the detailed guide: `README_NOTIFICATIONS_MIGRATION.md`
2. Review notification types: `NOTIFICATION_TYPES_REFERENCE.md`
3. Check application logs for errors
4. Verify PostgreSQL logs if migration fails

---

**Ready to notify your users! ??**
