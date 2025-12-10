# ?? Wihngo Notification Types Reference

## Complete List of Notification Types

### 1?? Engagement Notifications

#### `BirdLoved` - When someone loves your bird
- **Trigger:** User clicks love on a bird
- **Recipients:** Bird owner
- **Priority:** Medium
- **Channels:** InApp + Push
- **Grouping:** Yes (1 hour window)
- **Example:** "Heart Sarah loved Tweety!"

#### `BirdSupported` - When someone supports your bird
- **Trigger:** User sends financial support
- **Recipients:** Bird owner
- **Priority:** High
- **Channels:** InApp + Push + Email
- **Grouping:** No (individual)
- **Example:** "Corn John supported Tweety! $5.00 contributed"

#### `CommentAdded` - When someone comments on your bird/story
- **Trigger:** User posts a comment (future feature)
- **Recipients:** Bird owner / Story author
- **Priority:** Medium
- **Channels:** InApp + Push
- **Grouping:** Yes (30 min window)
- **Example:** "Chat Emily commented on your post"

---

### 2?? Content Notifications

#### `NewStory` - New story from followed bird
- **Trigger:** Bird owner posts new story
- **Recipients:** Users who loved the bird
- **Priority:** Low
- **Channels:** InApp + Push
- **Grouping:** Yes (daily digest if >5 stories)
- **Example:** "Book New story: Tweety has a new story to share!"

#### `HealthUpdate` - Bird health log update
- **Trigger:** Owner adds health log (future feature)
- **Recipients:** Bird supporters
- **Priority:** Medium
- **Channels:** InApp + Push + Email
- **Grouping:** No
- **Example:** "Hospital Health update for Tweety: Checkup completed"

#### `BirdMemorial` - Bird marked as memorial
- **Trigger:** Bird marked as deceased (future feature)
- **Recipients:** All who loved/supported the bird
- **Priority:** High
- **Channels:** InApp + Push + Email
- **Grouping:** No
- **Example:** "Broken Heart Remembering Tweety - Thank you for your support"

---

### 3?? Social Notifications

#### `NewFollower` - Someone follows your bird
- **Trigger:** User follows a bird (future feature)
- **Recipients:** Bird owner
- **Priority:** Low
- **Channels:** InApp only
- **Grouping:** Yes (daily digest)
- **Example:** "Person Alex is following Tweety"

#### `MilestoneAchieved` - Bird reaches love/support milestone
- **Trigger:** Bird hits 10, 50, 100, 500, 1000, or 5000 loves
- **Recipients:** Bird owner
- **Priority:** High
- **Channels:** InApp + Push
- **Grouping:** No
- **Example:** "Party Tweety reached 100 loves!"

#### `BirdFeatured` - Bird featured on platform
- **Trigger:** Admin features a bird (future feature)
- **Recipients:** Bird owner
- **Priority:** High
- **Channels:** InApp + Push + Email
- **Grouping:** No
- **Example:** "Star Tweety is featured on the Wihngo homepage!"

---

### 4?? System Notifications

#### `PremiumExpiring` - Premium expires in 7 days
- **Trigger:** Scheduled job checks subscriptions
- **Recipients:** Premium subscribers
- **Priority:** High
- **Channels:** InApp + Push + Email
- **Grouping:** No
- **Example:** "Warning Premium expires in 7 days - Renew now!"

#### `PaymentReceived` - Payment confirmation
- **Trigger:** Crypto/support payment confirmed
- **Recipients:** Both sender and receiver
- **Priority:** High
- **Channels:** InApp + Push + Email
- **Grouping:** No
- **Example:** "Checkmark Your $5.00 support for Tweety was processed!"

#### `SecurityAlert` - Security-related alerts
- **Trigger:** New device login, password change (future feature)
- **Recipients:** User affected
- **Priority:** Critical
- **Channels:** InApp + Push + Email + SMS
- **Grouping:** No
- **Example:** "Lock New login from iPhone at New York, USA"

---

### 5?? Recommendation Notifications

#### `SuggestedBirds` - Recommended birds
- **Trigger:** Weekly recommendation job (future feature)
- **Recipients:** Active users
- **Priority:** Low
- **Channels:** Push (with throttling)
- **Grouping:** No
- **Frequency:** Max 1 per week
- **Example:** "Parrot Birds you might love: Check out Charlie"

#### `ReEngagement` - Inactive user reminder
- **Trigger:** User hasn't opened app in 7/14/30 days (future feature)
- **Recipients:** Inactive users
- **Priority:** Low
- **Channels:** Push + Email
- **Grouping:** No
- **Frequency:** Day 7, 14, 30
- **Example:** "Star We miss you! 5 new stories from birds you love"

---

## ??? Notification Settings

### User Preferences (per notification type)
- ? InApp notifications (default: ON)
- ? Push notifications (default: ON)
- ? Email notifications (default: ON for high-priority, OFF for others)
- ? SMS notifications (default: OFF)

### Global Settings
- **Quiet Hours:** 10 PM - 8 AM (user configurable)
- **Max Push/Day:** 5 notifications
- **Max Email/Day:** 2 notifications
- **Grouping Window:** 60 minutes
- **Daily Digest:** OFF by default

---

## ?? Currently Active Notification Types

These are **already implemented** and working:

| Type | Status | Trigger Location |
|------|--------|-----------------|
| `BirdLoved` | ? Active | BirdsController.Love() |
| `BirdSupported` | ? Active | BirdsController.DonateToBird() |
| `NewStory` | ? Active | StoriesController.Post() |
| `MilestoneAchieved` | ? Active | BirdsController.Love() |
| `PaymentReceived` | ? Active | BirdsController.DonateToBird() |
| `PremiumExpiring` | ? Active | PremiumExpiryNotificationJob (scheduled) |

---

## ?? Notification Priority Levels

| Priority | Value | Use Case | Respects Quiet Hours? |
|----------|-------|----------|----------------------|
| Low | 0 | Suggestions, followers | ? Yes |
| Medium | 1 | Loves, comments | ? Yes |
| High | 2 | Support, milestones | ? Yes |
| Critical | 3 | Security, payments | ? No |

---

## ?? Notification Channels (Flags)

```csharp
[Flags]
public enum NotificationChannel
{
    None = 0,      // 0000
    InApp = 1,     // 0001
    Push = 2,      // 0010
    Email = 4,     // 0100
    Sms = 8        // 1000
}

// Example combinations:
InApp | Push = 3               // 0011
InApp | Push | Email = 7       // 0111
Push | Email = 6               // 0110
```

---

## ?? How to Trigger Notifications

### From Code (Example):
```csharp
await _notificationService.CreateNotificationAsync(new CreateNotificationDto
{
    UserId = userId,
    Type = NotificationType.BirdLoved,
    Title = "Heart Sarah loved Tweety!",
    Message = "Sarah loved your Parakeet. You now have 42 loves!",
    Priority = NotificationPriority.Medium,
    Channels = NotificationChannel.InApp | NotificationChannel.Push,
    DeepLink = "/birds/123",
    BirdId = birdId,
    ActorUserId = loverUserId
});
```

### From API (Test Endpoint):
```bash
POST /api/notifications/test
Authorization: Bearer {token}
```

---

## ?? Notification Grouping Examples

### Scenario 1: Multiple Loves
- User receives 5 loves within 1 hour
- **Result:** Single notification: "Heart 5 people loved Tweety!"

### Scenario 2: Multiple Stories
- User follows bird that posts 10 stories in a day
- **Result:** Daily digest: "Book Tweety has 10 new stories to share!"

### Scenario 3: Individual Support
- User receives 3 support donations
- **Result:** 3 separate notifications (high priority, no grouping)

---

## ?? Custom Emojis by Type

In the current implementation, emojis are represented as text:

| Type | Emoji Representation |
|------|---------------------|
| BirdLoved | "Heart" |
| BirdSupported | "Corn" |
| CommentAdded | "Chat" |
| NewStory | "Book" |
| HealthUpdate | "Hospital" |
| BirdMemorial | "Broken Heart" |
| NewFollower | "Person" |
| MilestoneAchieved | "Party" |
| BirdFeatured | "Star" |
| PremiumExpiring | "Warning" |
| PaymentReceived | "Checkmark" |
| SecurityAlert | "Lock" |
| SuggestedBirds | "Parrot" |
| ReEngagement | "Star" |

---

## ?? Next Steps to Implement

### Future Features:
1. **Comments System** ? Enable `CommentAdded` notifications
2. **Follow System** ? Enable `NewFollower` notifications
3. **Health Logs** ? Enable `HealthUpdate` notifications
4. **Memorial Status** ? Enable `BirdMemorial` notifications
5. **Featured Birds** ? Enable `BirdFeatured` notifications
6. **Security Events** ? Enable `SecurityAlert` notifications
7. **Recommendation Engine** ? Enable `SuggestedBirds` notifications
8. **Re-engagement Campaign** ? Enable `ReEngagement` notifications

---

## ??? Useful Queries

### See all notification types for a user:
```sql
SELECT notification_type, push_enabled, email_enabled
FROM notification_preferences
WHERE user_id = 'your-user-id'
ORDER BY notification_type;
```

### Check notification counts by type:
```sql
SELECT type, COUNT(*) as count
FROM notifications
GROUP BY type
ORDER BY count DESC;
```

### Find most active notification recipients:
```sql
SELECT u.name, COUNT(n.notification_id) as notification_count
FROM notifications n
JOIN users u ON u.user_id = n.user_id
GROUP BY u.user_id, u.name
ORDER BY notification_count DESC
LIMIT 10;
```

---

**For detailed implementation, see the notification service code!** ??
