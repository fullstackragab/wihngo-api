namespace Wihngo.Models.Enums
{
    public enum NotificationType
    {
        // Engagement Notifications
        BirdLoved,
        BirdSupported,
        CommentAdded,
        
        // Content Notifications
        NewStory,
        HealthUpdate,
        BirdMemorial,
        
        // Social Notifications
        NewFollower,
        MilestoneAchieved,
        BirdFeatured,
        
        // System Notifications
        PremiumExpiring,
        PaymentReceived,
        SecurityAlert,
        
        // Recommendation Notifications
        SuggestedBirds,
        ReEngagement,

        // Weekly Support Notifications
        WeeklySupportReminder,      // Reminder to approve weekly payment
        WeeklySupportCompleted,     // Confirmation that payment was processed
        WeeklySupportMissed,        // User missed a weekly payment
        WeeklySupportSubscribed     // New subscription created
    }
}
