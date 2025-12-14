# Complete Migration Blueprint

This document provides EXACT code replacements for all remaining LINQ errors.
Copy-paste these solutions to eliminate all Entity Framework dependencies.

---

## 1. BirdsController.cs - Line 70

### Current (BROKEN):
```csharp
var lovedBirdIds = await _db.Loves
    .Where(l => l.UserId == userId.Value)
    .Select(l => l.BirdId)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = "SELECT bird_id FROM loves WHERE user_id = @UserId";
var lovedBirdIds = await _dbFactory.QueryListAsync<Guid>(sql, new { UserId = userId.Value });
```

---

## 2. BirdsController.cs - Line 89

### Current (BROKEN):
```csharp
var birds = await _db.Birds
    .Where(b => b.OwnerId == userId.Value)
    .Select(b => new
    {
        BirdId = b.BirdId,
        Name = b.Name,
        Species = b.Species,
        ImageUrl = b.ImageUrl,
        VideoUrl = b.VideoUrl,
        Tagline = b.Tagline,
        LovedCount = b.LovedCount,
        SupportedCount = b.SupportedCount,
        OwnerId = b.OwnerId
    })
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT 
        bird_id as BirdId,
        name as Name,
        species as Species,
        image_url as ImageUrl,
        video_url as VideoUrl,
        tagline as Tagline,
        loved_count as LovedCount,
        supported_count as SupportedCount,
        owner_id as OwnerId
    FROM birds
    WHERE owner_id = @OwnerId";

var birds = await _dbFactory.QueryListAsync<dynamic>(sql, new { OwnerId = userId.Value });
```

---

## 3. BirdsController.cs - Lines 531-534 (GetLovers)

### Current (BROKEN):
```csharp
var lovers = await _db.Loves
    .Where(l => l.BirdId == id)
    .Include(l => l.User)
    .Select(l => new UserSummaryDto { 
        UserId = l.UserId, 
        Name = l.User != null ? l.User.Name : string.Empty 
    })
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT 
        l.user_id as UserId,
        COALESCE(u.name, '') as Name
    FROM loves l
    LEFT JOIN users u ON l.user_id = u.user_id
    WHERE l.bird_id = @BirdId";

var lovers = await _dbFactory.QueryListAsync<UserSummaryDto>(sql, new { BirdId = id });
```

---

## 4. BirdsController.cs - Line 562

### Current (BROKEN):
```csharp
_db.Add(usage);
await _db.SaveChangesAsync();
```

### Replace With:
```csharp
var sql = @"
    INSERT INTO support_usages (
        usage_id, bird_id, supporter_id, amount_cents, created_at
    ) VALUES (
        @UsageId, @BirdId, @SupporterId, @AmountCents, @CreatedAt
    )";

await _dbFactory.ExecuteAsync(sql, new
{
    UsageId = usage.UsageId,
    BirdId = usage.BirdId,
    SupporterId = usage.SupporterId,
    AmountCents = usage.AmountCents,
    CreatedAt = usage.CreatedAt
});
```

---

## 5. BirdsController.cs - Line 723

### Current (BROKEN):
```csharp
var birdIds = await _db.SupportTransactions
    .Where(t => t.BirdId == id)
    .Select(t => t.BirdId)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = "SELECT DISTINCT bird_id FROM support_transactions WHERE bird_id = @BirdId";
var birdIds = await _dbFactory.QueryListAsync<Guid>(sql, new { BirdId = id });
```

---

## 6. StoriesController.cs - Line 550

### Current (BROKEN):
```csharp
var birdIds = await _db.StoryBirds
    .Where(sb => sb.StoryId == story.StoryId)
    .Select(sb => sb.BirdId)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = "SELECT bird_id FROM story_birds WHERE story_id = @StoryId";
var birdIds = await _dbFactory.QueryListAsync<Guid>(sql, new { StoryId = story.StoryId });
```

---

## 7. StoriesController.cs - Line 974

### Current (BROKEN):
```csharp
var hasActiveSubscription = await _db.BirdPremiumSubscriptions
    .AnyAsync(s => birdIds.Contains(s.BirdId) && s.Status == "active");
```

### Replace With:
```csharp
var sql = @"
    SELECT EXISTS(
        SELECT 1 FROM bird_premium_subscriptions 
        WHERE bird_id = ANY(@BirdIds) AND status = 'active'
    )";

var hasActiveSubscription = await _dbFactory.ExecuteScalarAsync<bool>(sql, new { BirdIds = birdIds.ToArray() });
```

---

## 8. StoriesController.cs - Line 1021

### Current (BROKEN):
```csharp
var currentOrders = await _db.Stories
    .Where(s => s.StoryBirds.Any(sb => sb.BirdId == birdId) && 
                s.IsHighlighted && 
                s.HighlightOrder != null)
    .Select(s => s.HighlightOrder!.Value)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT DISTINCT s.highlight_order
    FROM stories s
    JOIN story_birds sb ON s.story_id = sb.story_id
    WHERE sb.bird_id = @BirdId 
      AND s.is_highlighted = true 
      AND s.highlight_order IS NOT NULL";

var currentOrders = await _dbFactory.QueryListAsync<int>(sql, new { BirdId = birdId });
```

---

## 9. NotificationService.cs - Line 73

### Current (BROKEN):
```csharp
var existingNotification = await _db.Notifications
    .Where(n => n.GroupId == groupId && n.UserId == dto.UserId && !n.IsRead)
    .OrderByDescending(n => n.CreatedAt)
    .FirstOrDefaultAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT * FROM notifications
    WHERE group_id = @GroupId 
      AND user_id = @UserId 
      AND is_read = false
    ORDER BY created_at DESC
    LIMIT 1";

var existingNotification = await _dbFactory.QuerySingleOrDefaultAsync<Notification>(sql, new 
{ 
    GroupId = groupId, 
    UserId = dto.UserId 
});
```

---

## 10. NotificationService.cs - Line 130

### Current (BROKEN):
```csharp
var totalCount = await query.CountAsync();
```

### Replace With:
```csharp
var countSql = @"
    SELECT COUNT(*) FROM notifications
    WHERE user_id = @UserId";
    
// Add additional WHERE conditions based on filters
if (unreadOnly)
{
    countSql += " AND is_read = false";
}

var totalCount = await _dbFactory.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
```

---

## 11. NotificationService.cs - Line 152

### Current (BROKEN):
```csharp
var notifications = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(n => new NotificationDto { ... })
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT 
        notification_id as NotificationId,
        user_id as UserId,
        type as Type,
        title as Title,
        message as Message,
        is_read as IsRead,
        created_at as CreatedAt
    FROM notifications
    WHERE user_id = @UserId";

// Add filters
if (unreadOnly)
{
    sql += " AND is_read = false";
}

sql += " ORDER BY created_at DESC OFFSET @Offset LIMIT @Limit";

var notifications = await _dbFactory.QueryListAsync<NotificationDto>(sql, new 
{ 
    UserId = userId,
    Offset = (page - 1) * pageSize,
    Limit = pageSize
});
```

---

## 12. NotificationCleanupJob.cs - Lines 33, 60

### Current (BROKEN):
```csharp
var oldReadNotifications = await _db.Notifications
    .Where(n => n.IsRead && n.ReadAt < cutoffDate)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT * FROM notifications
    WHERE is_read = true AND read_at < @CutoffDate";

var oldReadNotifications = await _dbFactory.QueryListAsync<Notification>(sql, new { CutoffDate = cutoffDate });
```

---

## 13. DailyDigestJob.cs - Line 40

### Current (BROKEN):
```csharp
var userIds = await _db.NotificationSettings
    .Where(ns => ns.EnableDailyDigest && ns.DigestTime == currentHour)
    .Select(ns => ns.UserId)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT user_id FROM notification_settings
    WHERE enable_daily_digest = true AND digest_time = @CurrentHour";

var userIds = await _dbFactory.QueryListAsync<Guid>(sql, new { CurrentHour = currentHour });
```

---

## 14. ReconciliationJob.cs - Line 73

### Current (BROKEN):
```csharp
var duplicates = await _context.Payments
    .GroupBy(p => p.TxHash)
    .Where(g => g.Count() > 1)
    .Select(g => new { TxHash = g.Key, Count = g.Count(), Payments = g.ToList() })
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT tx_hash, COUNT(*) as count
    FROM payments
    GROUP BY tx_hash
    HAVING COUNT(*) > 1";

var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var duplicateHashes = await connection.QueryAsync<(string TxHash, int Count)>(sql);
    
    var duplicates = new List<dynamic>();
    foreach (var dup in duplicateHashes)
    {
        var paymentsSql = "SELECT * FROM payments WHERE tx_hash = @TxHash";
        var payments = await connection.QueryAsync<Payment>(paymentsSql, new { TxHash = dup.TxHash });
        
        duplicates.Add(new 
        { 
            TxHash = dup.TxHash, 
            Count = dup.Count, 
            Payments = payments.ToList() 
        });
    }
    
    return duplicates;
}
finally
{
    await connection.DisposeAsync();
}
```

---

## 15. ReconciliationJob.cs - Line 147

### Current (BROKEN):
```csharp
var todayRevenue = await _context.Invoices
    .Where(i => i.State == "PAID" && i.IssuedAt >= today)
    .SumAsync(i => i.AmountFiat);
```

### Replace With:
```csharp
var sql = @"
    SELECT COALESCE(SUM(amount_fiat), 0)
    FROM invoices
    WHERE state = 'PAID' AND issued_at >= @Today";

var todayRevenue = await _dbFactory.ExecuteScalarAsync<decimal>(sql, new { Today = today });
```

---

## 16. PaymentMonitorJob.cs - Line 465

### Current (BROKEN):
```csharp
await _context.CryptoPaymentRequests
    .Where(r => r.Id == expiredRequest.Id)
    .ExecuteUpdateAsync(p => p.SetProperty(x => x.Status, "expired"));
```

### Replace With:
```csharp
var sql = "UPDATE crypto_payment_requests SET status = 'expired' WHERE id = @Id";
await _dbFactory.ExecuteAsync(sql, new { Id = expiredRequest.Id });
```

---

## 17. PremiumExpiryNotificationJob.cs - Line 45

### Current (BROKEN):
```csharp
var expiring = await _context.BirdPremiumSubscriptions
    .Where(s => s.Status == "active" && s.ExpiresAt <= checkDate)
    .Include(s => s.Bird)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT 
        s.*,
        b.bird_id, b.name, b.owner_id
    FROM bird_premium_subscriptions s
    JOIN birds b ON s.bird_id = b.bird_id
    WHERE s.status = 'active' AND s.expires_at <= @CheckDate";

var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var expiring = new List<BirdPremiumSubscription>();
    
    await connection.QueryAsync<BirdPremiumSubscription, Bird, BirdPremiumSubscription>(
        sql,
        (subscription, bird) =>
        {
            subscription.Bird = bird;
            expiring.Add(subscription);
            return subscription;
        },
        new { CheckDate = checkDate },
        splitOn: "bird_id");
    
    return expiring;
}
finally
{
    await connection.DisposeAsync();
}
```

---

## 18. PushNotificationService.cs - Line 47

### Current (BROKEN):
```csharp
var devices = await _db.UserDevices
    .Where(d => d.UserId == userId && d.IsActive)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT * FROM user_devices
    WHERE user_id = @UserId AND is_active = true";

var devices = await _dbFactory.QueryListAsync<UserDevice>(sql, new { UserId = userId });
```

---

## 19. EmailNotificationService.cs - Line 88

### Current (BROKEN):
```csharp
var recentNotifications = await _db.Notifications
    .Where(n => n.UserId == userId && n.CreatedAt >= since)
    .OrderBy(n => n.CreatedAt)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT * FROM notifications
    WHERE user_id = @UserId AND created_at >= @Since
    ORDER BY created_at ASC";

var recentNotifications = await _dbFactory.QueryListAsync<Notification>(sql, new 
{ 
    UserId = userId, 
    Since = since 
});
```

---

## 20. NotificationsController.cs - Line 265

### Current (BROKEN):
```csharp
var devices = await _db.UserDevices
    .Where(d => d.UserId == userId.Value)
    .Select(d => new UserDeviceDto { ... })
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT 
        device_id as DeviceId,
        user_id as UserId,
        device_type as DeviceType,
        push_token as PushToken,
        is_active as IsActive,
        created_at as CreatedAt
    FROM user_devices
    WHERE user_id = @UserId";

var devices = await _dbFactory.QueryListAsync<UserDeviceDto>(sql, new { UserId = userId.Value });
```

---

## 21. DevController.cs - Line 221

### Current (BROKEN):
```csharp
var users = await _db.Users
    .Select(u => new { ... })
    .OrderBy(u => u.name)
    .ThenByDescending(u => u.storiesCount)
    .ToListAsync();
```

### Replace With:
```csharp
var sql = @"
    SELECT 
        u.name,
        u.email,
        '***' as password,
        COUNT(DISTINCT b.bird_id) as birds_count,
        COUNT(DISTINCT s.story_id) as stories_count,
        CASE WHEN COUNT(DISTINCT b.bird_id) > 0 THEN true ELSE false END as has_data
    FROM users u
    LEFT JOIN birds b ON u.user_id = b.owner_id
    LEFT JOIN stories s ON u.user_id = s.author_id
    GROUP BY u.user_id, u.name, u.email
    ORDER BY u.name ASC, stories_count DESC";

var users = await _dbFactory.QueryListAsync<dynamic>(sql);
```

---

## General Pattern for All Migrations

1. **Identify the LINQ chain**
2. **Convert to SQL:**
   - `Where()` ? `WHERE`
   - `Select()` ? `SELECT columns`
   - `OrderBy()` ? `ORDER BY`
   - `Include()` ? `JOIN`
   - `GroupBy()` ? `GROUP BY`
   - `Count()` ? `COUNT(*)`
   - `Sum()` ? `SUM(column)`
   - `Any()` ? `EXISTS()` or `COUNT(*) > 0`
3. **Use helper methods:**
   - `QueryListAsync<T>()` for lists
   - `QuerySingleOrDefaultAsync<T>()` for single items
   - `ExecuteScalarAsync<T>()` for aggregates
   - `ExecuteAsync()` for INSERT/UPDATE/DELETE
4. **Use parameterized queries** - Always @Parameters, never string interpolation
5. **Test!**

---

## After Migration Checklist

- [ ] Remove `using System.Linq;` if not needed
- [ ] Remove any EF Core using statements
- [ ] Verify all queries use @Parameters
- [ ] Test the endpoint/method
- [ ] Update related tests
- [ ] Mark task complete in IMPLEMENTATION_STATUS.md

---

## Need Help?

- See `SQL_EXAMPLES.md` for detailed patterns
- See `MIGRATION_GUIDE.md` for conceptual guidance
- See `Helpers/SqlQueryHelper.cs` for utility methods
