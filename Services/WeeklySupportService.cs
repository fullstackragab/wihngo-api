using Dapper;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for managing weekly support subscriptions (non-custodial recurring donations).
///
/// Flow:
/// 1. User subscribes to weekly bird support
/// 2. Hangfire job sends weekly reminders (push + email)
/// 3. User approves with 1-click, creating a SupportIntent
/// 4. User signs transaction in Phantom wallet
/// 5. Bird owner receives 100% of funds
/// </summary>
public class WeeklySupportService : IWeeklySupportService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ISupportIntentService _supportIntentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WeeklySupportService> _logger;

    // Payment reminder expires after 7 days
    private const int ReminderExpirationDays = 7;

    // Auto-pause after this many consecutive misses
    private const int MaxConsecutiveMisses = 3;

    private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public WeeklySupportService(
        IDbConnectionFactory dbFactory,
        ISupportIntentService supportIntentService,
        INotificationService notificationService,
        ILogger<WeeklySupportService> logger)
    {
        _dbFactory = dbFactory;
        _supportIntentService = supportIntentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    #region Subscription Management

    public async Task<(WeeklySupportSubscriptionResponse? Response, ValidationErrorResponse? Error)> CreateSubscriptionAsync(
        Guid userId,
        CreateWeeklySupportRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Validate bird exists and accepts support
        var bird = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT bird_id, owner_id, name, image_url, species, support_enabled FROM birds WHERE bird_id = @BirdId",
            new { request.BirdId });

        if (bird == null)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "BirdId",
                Message = "Bird not found"
            });
        }

        if (bird.support_enabled == false)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "BirdId",
                Message = "This bird is not accepting support"
            });
        }

        Guid ownerId = bird.owner_id;

        // 2. Check user isn't subscribing to own bird
        if (ownerId == userId)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "BirdId",
                Message = "You cannot subscribe to your own bird"
            });
        }

        // 3. Check for existing subscription
        var existingId = await conn.QueryFirstOrDefaultAsync<Guid?>(
            @"SELECT id FROM weekly_support_subscriptions
              WHERE subscriber_user_id = @UserId AND bird_id = @BirdId AND status != 'cancelled'",
            new { UserId = userId, request.BirdId });

        if (existingId.HasValue)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "BirdId",
                Message = "You already have an active subscription for this bird"
            });
        }

        // 4. Check user has wallet connected
        var hasWallet = await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM wallets WHERE user_id = @UserId AND is_primary = true)",
            new { UserId = userId });

        if (!hasWallet)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "Wallet",
                Message = "Please connect your Phantom wallet first"
            });
        }

        // 5. Calculate next reminder date
        var nextReminder = CalculateNextReminderDate(request.DayOfWeek, request.PreferredHour);

        // 6. Insert subscription
        var subscriptionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(@"
            INSERT INTO weekly_support_subscriptions (
                id, subscriber_user_id, bird_id, recipient_user_id,
                amount_usdc, wihngo_support_amount, currency, status,
                day_of_week, preferred_hour, next_reminder_at,
                created_at, updated_at
            ) VALUES (
                @Id, @SubscriberUserId, @BirdId, @RecipientUserId,
                @AmountUsdc, @WihngoSupportAmount, 'USDC', 'active',
                @DayOfWeek, @PreferredHour, @NextReminderAt,
                @Now, @Now
            )",
            new
            {
                Id = subscriptionId,
                SubscriberUserId = userId,
                request.BirdId,
                RecipientUserId = ownerId,
                request.AmountUsdc,
                request.WihngoSupportAmount,
                request.DayOfWeek,
                request.PreferredHour,
                NextReminderAt = nextReminder,
                Now = now
            });

        _logger.LogInformation(
            "User {UserId} subscribed to weekly support for bird {BirdId} at ${Amount}/week",
            userId, request.BirdId, request.AmountUsdc);

        // 7. Send confirmation notification
        await SendSubscriptionCreatedNotificationAsync(userId, (string)bird.name, subscriptionId);

        // 8. Return response
        var owner = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT name FROM users WHERE user_id = @OwnerId",
            new { OwnerId = ownerId });

        return (new WeeklySupportSubscriptionResponse
        {
            SubscriptionId = subscriptionId,
            BirdId = request.BirdId,
            BirdName = bird.name,
            BirdImageUrl = bird.image_url,
            BirdSpecies = bird.species,
            RecipientUserId = ownerId,
            RecipientName = owner?.name ?? "Unknown",
            AmountUsdc = request.AmountUsdc,
            WihngoSupportAmount = request.WihngoSupportAmount,
            TotalAmount = request.AmountUsdc + request.WihngoSupportAmount,
            Currency = "USDC",
            Status = WeeklySupportStatus.Active,
            DayOfWeek = request.DayOfWeek,
            DayOfWeekName = DayNames[request.DayOfWeek],
            PreferredHour = request.PreferredHour,
            NextReminderAt = nextReminder,
            CreatedAt = now
        }, null);
    }

    public async Task<WeeklySupportSubscriptionResponse?> GetSubscriptionAsync(Guid subscriptionId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT s.*, b.name as bird_name, b.image_url as bird_image_url, b.species,
                   u.name as recipient_name
            FROM weekly_support_subscriptions s
            JOIN birds b ON s.bird_id = b.bird_id
            JOIN users u ON s.recipient_user_id = u.user_id
            WHERE s.id = @SubscriptionId AND s.subscriber_user_id = @UserId",
            new { SubscriptionId = subscriptionId, UserId = userId });

        if (sub == null) return null;

        return MapToSubscriptionResponse(sub);
    }

    public async Task<List<WeeklySupportSummaryDto>> GetUserSubscriptionsAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var subs = await conn.QueryAsync<dynamic>(@"
            SELECT s.id, s.bird_id, s.amount_usdc, s.status, s.next_reminder_at,
                   s.total_payments_count, b.name as bird_name, b.image_url as bird_image_url
            FROM weekly_support_subscriptions s
            JOIN birds b ON s.bird_id = b.bird_id
            WHERE s.subscriber_user_id = @UserId AND s.status != 'cancelled'
            ORDER BY s.created_at DESC",
            new { UserId = userId });

        return subs.Select(s => new WeeklySupportSummaryDto
        {
            SubscriptionId = s.id,
            BirdId = s.bird_id,
            BirdName = s.bird_name,
            BirdImageUrl = s.bird_image_url,
            AmountUsdc = s.amount_usdc,
            Status = s.status,
            NextReminderAt = s.next_reminder_at,
            TotalPaymentsCount = s.total_payments_count
        }).ToList();
    }

    public async Task<BirdWeeklySupportersDto> GetBirdSubscribersAsync(Guid birdId, Guid requestingUserId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Verify requesting user is the bird owner
        var isOwner = await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM birds WHERE bird_id = @BirdId AND owner_id = @UserId)",
            new { BirdId = birdId, UserId = requestingUserId });

        if (!isOwner)
        {
            return new BirdWeeklySupportersDto { BirdId = birdId };
        }

        var stats = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT
                COUNT(*) FILTER (WHERE status = 'active') as active_count,
                COALESCE(SUM(amount_usdc) FILTER (WHERE status = 'active'), 0) as weekly_total,
                COALESCE(SUM(total_amount_paid), 0) as lifetime_total
            FROM weekly_support_subscriptions
            WHERE bird_id = @BirdId",
            new { BirdId = birdId });

        return new BirdWeeklySupportersDto
        {
            BirdId = birdId,
            ActiveSubscribers = (int)(stats?.active_count ?? 0),
            WeeklyAmountUsdc = stats?.weekly_total ?? 0m,
            TotalReceivedUsdc = stats?.lifetime_total ?? 0m
        };
    }

    public async Task<UserWeeklySupportSummaryDto> GetUserSummaryAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var stats = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT
                COUNT(*) FILTER (WHERE status = 'active') as active_count,
                COUNT(*) FILTER (WHERE status = 'paused') as paused_count,
                COALESCE(SUM(amount_usdc + wihngo_support_amount) FILTER (WHERE status = 'active'), 0) as weekly_total,
                COALESCE(SUM(total_amount_paid), 0) as lifetime_total
            FROM weekly_support_subscriptions
            WHERE subscriber_user_id = @UserId",
            new { UserId = userId });

        var pendingCount = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM weekly_support_payments p
            JOIN weekly_support_subscriptions s ON p.subscription_id = s.id
            WHERE s.subscriber_user_id = @UserId
              AND p.status IN ('reminder_sent', 'intent_created')
              AND (p.expires_at IS NULL OR p.expires_at > @Now)",
            new { UserId = userId, Now = DateTime.UtcNow });

        return new UserWeeklySupportSummaryDto
        {
            ActiveSubscriptions = (int)(stats?.active_count ?? 0),
            PausedSubscriptions = (int)(stats?.paused_count ?? 0),
            WeeklyTotalUsdc = stats?.weekly_total ?? 0m,
            LifetimeTotalPaidUsdc = stats?.lifetime_total ?? 0m,
            PendingApprovals = pendingCount
        };
    }

    public async Task<(WeeklySupportSubscriptionResponse? Response, ValidationErrorResponse? Error)> UpdateSubscriptionAsync(
        Guid subscriptionId,
        Guid userId,
        UpdateWeeklySupportRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM weekly_support_subscriptions WHERE id = @Id AND subscriber_user_id = @UserId",
            new { Id = subscriptionId, UserId = userId });

        if (sub == null)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "SubscriptionId",
                Message = "Subscription not found"
            });
        }

        var updates = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", subscriptionId);
        parameters.Add("Now", DateTime.UtcNow);

        if (request.AmountUsdc.HasValue)
        {
            updates.Add("amount_usdc = @AmountUsdc");
            parameters.Add("AmountUsdc", request.AmountUsdc.Value);
        }

        if (request.WihngoSupportAmount.HasValue)
        {
            updates.Add("wihngo_support_amount = @WihngoSupportAmount");
            parameters.Add("WihngoSupportAmount", request.WihngoSupportAmount.Value);
        }

        if (request.DayOfWeek.HasValue || request.PreferredHour.HasValue)
        {
            var dayOfWeek = request.DayOfWeek ?? (int)sub.day_of_week;
            var preferredHour = request.PreferredHour ?? (int)sub.preferred_hour;
            var nextReminder = CalculateNextReminderDate(dayOfWeek, preferredHour);

            updates.Add("day_of_week = @DayOfWeek");
            updates.Add("preferred_hour = @PreferredHour");
            updates.Add("next_reminder_at = @NextReminderAt");
            parameters.Add("DayOfWeek", dayOfWeek);
            parameters.Add("PreferredHour", preferredHour);
            parameters.Add("NextReminderAt", nextReminder);
        }

        if (updates.Count > 0)
        {
            updates.Add("updated_at = @Now");
            var sql = $"UPDATE weekly_support_subscriptions SET {string.Join(", ", updates)} WHERE id = @Id";
            await conn.ExecuteAsync(sql, parameters);
        }

        return (await GetSubscriptionAsync(subscriptionId, userId), null);
    }

    public async Task<bool> PauseSubscriptionAsync(Guid subscriptionId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var updated = await conn.ExecuteAsync(@"
            UPDATE weekly_support_subscriptions
            SET status = 'paused', paused_at = @Now, updated_at = @Now
            WHERE id = @Id AND subscriber_user_id = @UserId AND status = 'active'",
            new { Id = subscriptionId, UserId = userId, Now = DateTime.UtcNow });

        if (updated > 0)
        {
            _logger.LogInformation("User {UserId} paused subscription {SubscriptionId}", userId, subscriptionId);
            return true;
        }
        return false;
    }

    public async Task<bool> ResumeSubscriptionAsync(Guid subscriptionId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT day_of_week, preferred_hour FROM weekly_support_subscriptions WHERE id = @Id AND subscriber_user_id = @UserId AND status = 'paused'",
            new { Id = subscriptionId, UserId = userId });

        if (sub == null) return false;

        var nextReminder = CalculateNextReminderDate((int)sub.day_of_week, (int)sub.preferred_hour);

        var updated = await conn.ExecuteAsync(@"
            UPDATE weekly_support_subscriptions
            SET status = 'active', paused_at = NULL, next_reminder_at = @NextReminder,
                consecutive_missed_count = 0, updated_at = @Now
            WHERE id = @Id",
            new { Id = subscriptionId, NextReminder = nextReminder, Now = DateTime.UtcNow });

        if (updated > 0)
        {
            _logger.LogInformation("User {UserId} resumed subscription {SubscriptionId}", userId, subscriptionId);
            return true;
        }
        return false;
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var updated = await conn.ExecuteAsync(@"
            UPDATE weekly_support_subscriptions
            SET status = 'cancelled', cancelled_at = @Now, updated_at = @Now
            WHERE id = @Id AND subscriber_user_id = @UserId AND status != 'cancelled'",
            new { Id = subscriptionId, UserId = userId, Now = DateTime.UtcNow });

        if (updated > 0)
        {
            _logger.LogInformation("User {UserId} cancelled subscription {SubscriptionId}", userId, subscriptionId);
            return true;
        }
        return false;
    }

    #endregion

    #region Payment Flow

    public async Task<List<WeeklyPaymentReminderDto>> GetPendingPaymentsAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var payments = await conn.QueryAsync<dynamic>(@"
            SELECT p.*, s.bird_id, s.recipient_user_id,
                   b.name as bird_name, b.image_url as bird_image_url,
                   u.name as recipient_name
            FROM weekly_support_payments p
            JOIN weekly_support_subscriptions s ON p.subscription_id = s.id
            JOIN birds b ON s.bird_id = b.bird_id
            JOIN users u ON s.recipient_user_id = u.user_id
            WHERE s.subscriber_user_id = @UserId
              AND p.status IN ('reminder_sent', 'intent_created')
              AND (p.expires_at IS NULL OR p.expires_at > @Now)
            ORDER BY p.created_at DESC",
            new { UserId = userId, Now = DateTime.UtcNow });

        return payments.Select(p => new WeeklyPaymentReminderDto
        {
            PaymentId = p.id,
            SubscriptionId = p.subscription_id,
            BirdId = p.bird_id,
            BirdName = p.bird_name,
            BirdImageUrl = p.bird_image_url,
            RecipientName = p.recipient_name,
            AmountUsdc = p.amount_usdc,
            WihngoSupportAmount = p.wihngo_support_amount,
            TotalAmount = p.amount_usdc + p.wihngo_support_amount,
            WeekStartDate = p.week_start_date,
            WeekEndDate = p.week_end_date,
            Status = p.status,
            ReminderSentAt = p.reminder_sent_at,
            ExpiresAt = p.expires_at ?? DateTime.UtcNow.AddDays(ReminderExpirationDays),
            IsExpired = p.expires_at != null && DateTime.UtcNow > (DateTime)p.expires_at,
            ApproveDeepLink = $"/weekly-support/approve/{p.id}"
        }).ToList();
    }

    public async Task<(ApproveWeeklyPaymentResponse? Response, ValidationErrorResponse? Error)> CreateQuickPaymentIntentAsync(
        Guid userId,
        ApproveWeeklyPaymentRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Get payment and subscription details
        var payment = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT p.*, s.bird_id, s.subscriber_user_id, s.recipient_user_id
            FROM weekly_support_payments p
            JOIN weekly_support_subscriptions s ON p.subscription_id = s.id
            WHERE p.id = @PaymentId",
            new { request.PaymentId });

        if (payment == null)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "PaymentId",
                Message = "Payment not found"
            });
        }

        // 2. Validate user owns this payment
        if ((Guid)payment.subscriber_user_id != userId)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "PaymentId",
                Message = "Payment not found"
            });
        }

        // 3. Check payment status allows approval
        string status = payment.status;
        if (status != WeeklyPaymentStatus.ReminderSent && status != WeeklyPaymentStatus.IntentCreated)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "PaymentId",
                Message = $"Payment cannot be approved (status: {status})"
            });
        }

        // 4. Check not expired
        if (payment.expires_at != null && DateTime.UtcNow > (DateTime)payment.expires_at)
        {
            return (null, new ValidationErrorResponse
            {
                Field = "PaymentId",
                Message = "Payment reminder has expired"
            });
        }

        // 5. If already has intent, return it (idempotent)
        if (payment.support_intent_id != null)
        {
            var existingIntent = await _supportIntentService.GetSupportIntentResponseAsync(
                (Guid)payment.support_intent_id, userId);

            if (existingIntent != null)
            {
                return (new ApproveWeeklyPaymentResponse
                {
                    IntentId = existingIntent.IntentId,
                    PaymentId = request.PaymentId,
                    BirdWalletAddress = existingIntent.BirdWalletAddress ?? string.Empty,
                    WihngoWalletAddress = existingIntent.WihngoWalletAddress,
                    AmountUsdc = existingIntent.BirdAmount,
                    WihngoSupportAmount = existingIntent.WihngoSupportAmount,
                    TotalAmount = existingIntent.TotalAmount,
                    SerializedTransaction = existingIntent.SerializedTransaction ?? string.Empty,
                    ExpiresAt = existingIntent.ExpiresAt,
                    WasAlreadyCreated = true
                }, null);
            }
        }

        // 6. Create new support intent
        var intentRequest = new CreateSupportIntentRequest
        {
            BirdId = payment.bird_id,
            BirdAmount = payment.amount_usdc,
            WihngoSupportAmount = payment.wihngo_support_amount,
            Currency = "USDC",
            IdempotencyKey = request.IdempotencyKey
        };

        var (intentResponse, intentError) = await _supportIntentService.CreateSupportIntentAsync(userId, intentRequest);

        if (intentError != null)
        {
            return (null, intentError);
        }

        // 7. Link intent to weekly payment
        await conn.ExecuteAsync(@"
            UPDATE weekly_support_payments
            SET support_intent_id = @IntentId, intent_created_at = @Now, status = 'intent_created', updated_at = @Now
            WHERE id = @PaymentId",
            new { IntentId = intentResponse!.IntentId, request.PaymentId, Now = DateTime.UtcNow });

        _logger.LogInformation(
            "Created quick payment intent {IntentId} for weekly payment {PaymentId}",
            intentResponse.IntentId, request.PaymentId);

        return (new ApproveWeeklyPaymentResponse
        {
            IntentId = intentResponse.IntentId,
            PaymentId = request.PaymentId,
            BirdWalletAddress = intentResponse.BirdWalletAddress ?? string.Empty,
            WihngoWalletAddress = intentResponse.WihngoWalletAddress,
            AmountUsdc = intentResponse.BirdAmount,
            WihngoSupportAmount = intentResponse.WihngoSupportAmount,
            TotalAmount = intentResponse.TotalAmount,
            SerializedTransaction = intentResponse.SerializedTransaction ?? string.Empty,
            ExpiresAt = intentResponse.ExpiresAt,
            WasAlreadyCreated = false
        }, null);
    }

    public async Task<bool> MarkPaymentCompletedAsync(Guid supportIntentId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Find weekly payment linked to this intent
        var payment = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT p.id, p.subscription_id, p.amount_usdc, p.wihngo_support_amount
            FROM weekly_support_payments p
            WHERE p.support_intent_id = @IntentId",
            new { IntentId = supportIntentId });

        if (payment == null) return false;

        var now = DateTime.UtcNow;
        decimal totalAmount = payment.amount_usdc + payment.wihngo_support_amount;

        // Update payment status
        await conn.ExecuteAsync(@"
            UPDATE weekly_support_payments
            SET status = 'completed', completed_at = @Now, updated_at = @Now
            WHERE id = @PaymentId",
            new { PaymentId = payment.id, Now = now });

        // Update subscription stats
        await conn.ExecuteAsync(@"
            UPDATE weekly_support_subscriptions
            SET last_payment_at = @Now,
                total_payments_count = total_payments_count + 1,
                total_amount_paid = total_amount_paid + @Amount,
                consecutive_missed_count = 0,
                updated_at = @Now
            WHERE id = @SubscriptionId",
            new { payment.subscription_id, Amount = totalAmount, Now = now });

        _logger.LogInformation(
            "Weekly payment {PaymentId} completed for subscription {SubscriptionId}",
            payment.id, payment.subscription_id);

        return true;
    }

    public async Task<List<WeeklyPaymentHistoryDto>> GetPaymentHistoryAsync(
        Guid subscriptionId,
        Guid userId,
        int limit = 20)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Verify ownership
        var owns = await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM weekly_support_subscriptions WHERE id = @Id AND subscriber_user_id = @UserId)",
            new { Id = subscriptionId, UserId = userId });

        if (!owns) return new List<WeeklyPaymentHistoryDto>();

        var payments = await conn.QueryAsync<dynamic>(@"
            SELECT p.*, s.bird_id, b.name as bird_name
            FROM weekly_support_payments p
            JOIN weekly_support_subscriptions s ON p.subscription_id = s.id
            JOIN birds b ON s.bird_id = b.bird_id
            WHERE p.subscription_id = @SubscriptionId
            ORDER BY p.week_start_date DESC
            LIMIT @Limit",
            new { SubscriptionId = subscriptionId, Limit = limit });

        return payments.Select(p => new WeeklyPaymentHistoryDto
        {
            PaymentId = p.id,
            SubscriptionId = p.subscription_id,
            BirdId = p.bird_id,
            BirdName = p.bird_name,
            WeekStartDate = p.week_start_date,
            WeekEndDate = p.week_end_date,
            AmountUsdc = p.amount_usdc,
            TotalAmount = p.amount_usdc + p.wihngo_support_amount,
            Status = p.status,
            CompletedAt = p.completed_at,
            CreatedAt = p.created_at
        }).ToList();
    }

    #endregion

    #region Background Job Methods

    public async Task ProcessWeeklyRemindersAsync()
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var now = DateTime.UtcNow;

        // Find subscriptions due for reminder
        var dueSubscriptions = await conn.QueryAsync<dynamic>(@"
            SELECT s.*, b.name as bird_name, b.image_url as bird_image_url
            FROM weekly_support_subscriptions s
            JOIN birds b ON s.bird_id = b.bird_id
            WHERE s.status = 'active'
              AND s.next_reminder_at <= @Now",
            new { Now = now });

        var processed = 0;
        foreach (var sub in dueSubscriptions)
        {
            try
            {
                await ProcessSingleReminderAsync(conn, sub, now);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process reminder for subscription {SubscriptionId}",
                    sub.id);
            }
        }

        if (processed > 0)
        {
            _logger.LogInformation("Processed {Count} weekly support reminders", processed);
        }
    }

    public async Task ExpireOldPaymentRemindersAsync()
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var now = DateTime.UtcNow;

        // Find expired payments
        var expiredPayments = await conn.QueryAsync<dynamic>(@"
            SELECT p.id, p.subscription_id, s.subscriber_user_id, s.consecutive_missed_count,
                   b.name as bird_name
            FROM weekly_support_payments p
            JOIN weekly_support_subscriptions s ON p.subscription_id = s.id
            JOIN birds b ON s.bird_id = b.bird_id
            WHERE p.status IN ('reminder_sent', 'intent_created')
              AND p.expires_at < @Now",
            new { Now = now });

        foreach (var payment in expiredPayments)
        {
            try
            {
                // Mark payment as expired
                await conn.ExecuteAsync(@"
                    UPDATE weekly_support_payments
                    SET status = 'expired', updated_at = @Now
                    WHERE id = @PaymentId",
                    new { PaymentId = payment.id, Now = now });

                // Increment missed count on subscription
                int newMissedCount = payment.consecutive_missed_count + 1;

                if (newMissedCount >= MaxConsecutiveMisses)
                {
                    // Auto-pause subscription
                    await conn.ExecuteAsync(@"
                        UPDATE weekly_support_subscriptions
                        SET status = 'paused',
                            paused_at = @Now,
                            consecutive_missed_count = @MissedCount,
                            updated_at = @Now
                        WHERE id = @SubscriptionId",
                        new { payment.subscription_id, MissedCount = newMissedCount, Now = now });

                    _logger.LogInformation(
                        "Auto-paused subscription {SubscriptionId} after {Count} consecutive misses",
                        payment.subscription_id, newMissedCount);

                    // Send auto-pause notification
                    await SendAutoPausedNotificationAsync(
                        payment.subscriber_user_id,
                        (string)payment.bird_name,
                        payment.subscription_id);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE weekly_support_subscriptions
                        SET consecutive_missed_count = @MissedCount, updated_at = @Now
                        WHERE id = @SubscriptionId",
                        new { payment.subscription_id, MissedCount = newMissedCount, Now = now });

                    // Send missed payment notification
                    await SendMissedPaymentNotificationAsync(
                        payment.subscriber_user_id,
                        (string)payment.bird_name,
                        payment.subscription_id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire payment {PaymentId}", payment.id);
            }
        }

        if (expiredPayments.Any())
        {
            _logger.LogInformation("Expired {Count} weekly payment reminders", expiredPayments.Count());
        }
    }

    #endregion

    #region Private Helpers

    private async Task ProcessSingleReminderAsync(System.Data.IDbConnection conn, dynamic sub, DateTime now)
    {
        // Calculate week dates
        var weekStart = GetWeekStart(now);
        var weekEnd = weekStart.AddDays(6);

        // Check if payment already exists for this week
        var existingPayment = await conn.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT id FROM weekly_support_payments WHERE subscription_id = @SubId AND week_start_date = @WeekStart",
            new { SubId = sub.id, WeekStart = weekStart.Date });

        if (existingPayment.HasValue)
        {
            // Already processed this week, just update next_reminder_at
            var nextReminder = CalculateNextReminderDate((int)sub.day_of_week, (int)sub.preferred_hour);
            await conn.ExecuteAsync(
                "UPDATE weekly_support_subscriptions SET next_reminder_at = @Next, updated_at = @Now WHERE id = @Id",
                new { Next = nextReminder, Now = now, Id = sub.id });
            return;
        }

        // Create payment record
        var paymentId = Guid.NewGuid();
        var expiresAt = now.AddDays(ReminderExpirationDays);

        await conn.ExecuteAsync(@"
            INSERT INTO weekly_support_payments (
                id, subscription_id, week_start_date, week_end_date,
                status, amount_usdc, wihngo_support_amount,
                reminder_sent_at, expires_at, created_at, updated_at
            ) VALUES (
                @Id, @SubId, @WeekStart, @WeekEnd,
                'reminder_sent', @Amount, @WihngoAmount,
                @Now, @ExpiresAt, @Now, @Now
            )",
            new
            {
                Id = paymentId,
                SubId = sub.id,
                WeekStart = weekStart.Date,
                WeekEnd = weekEnd.Date,
                Amount = sub.amount_usdc,
                WihngoAmount = sub.wihngo_support_amount,
                Now = now,
                ExpiresAt = expiresAt
            });

        // Update next reminder date
        var nextReminderAt = CalculateNextReminderDate((int)sub.day_of_week, (int)sub.preferred_hour);
        await conn.ExecuteAsync(
            "UPDATE weekly_support_subscriptions SET next_reminder_at = @Next, updated_at = @Now WHERE id = @Id",
            new { Next = nextReminderAt, Now = now, Id = sub.id });

        // Send notification
        await SendWeeklyReminderNotificationAsync(
            (Guid)sub.subscriber_user_id,
            (string)sub.bird_name,
            paymentId,
            sub.amount_usdc);

        _logger.LogInformation(
            "Sent weekly reminder for subscription {SubscriptionId}, payment {PaymentId}",
            sub.id, paymentId);
    }

    private static DateTime CalculateNextReminderDate(int dayOfWeek, int preferredHour)
    {
        var now = DateTime.UtcNow;
        var targetDay = (DayOfWeek)dayOfWeek;

        // Find next occurrence of target day
        var daysUntilTarget = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;

        // If today is the target day but we're past the preferred hour, go to next week
        if (daysUntilTarget == 0 && now.Hour >= preferredHour)
        {
            daysUntilTarget = 7;
        }

        var nextDate = now.Date.AddDays(daysUntilTarget).AddHours(preferredHour);
        return nextDate;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        // Week starts on Sunday
        var diff = date.DayOfWeek - DayOfWeek.Sunday;
        return date.Date.AddDays(-diff);
    }

    private WeeklySupportSubscriptionResponse MapToSubscriptionResponse(dynamic sub)
    {
        return new WeeklySupportSubscriptionResponse
        {
            SubscriptionId = sub.id,
            BirdId = sub.bird_id,
            BirdName = sub.bird_name,
            BirdImageUrl = sub.bird_image_url,
            BirdSpecies = sub.species,
            RecipientUserId = sub.recipient_user_id,
            RecipientName = sub.recipient_name,
            AmountUsdc = sub.amount_usdc,
            WihngoSupportAmount = sub.wihngo_support_amount,
            TotalAmount = sub.amount_usdc + sub.wihngo_support_amount,
            Currency = sub.currency,
            Status = sub.status,
            DayOfWeek = sub.day_of_week,
            DayOfWeekName = DayNames[sub.day_of_week],
            PreferredHour = sub.preferred_hour,
            NextReminderAt = sub.next_reminder_at,
            LastPaymentAt = sub.last_payment_at,
            TotalPaymentsCount = sub.total_payments_count,
            TotalAmountPaid = sub.total_amount_paid,
            ConsecutiveMissedCount = sub.consecutive_missed_count,
            CreatedAt = sub.created_at,
            PausedAt = sub.paused_at
        };
    }

    #endregion

    #region Notifications

    private async Task SendSubscriptionCreatedNotificationAsync(Guid userId, string birdName, Guid subscriptionId)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                Type = NotificationType.WeeklySupportSubscribed,
                Title = "Weekly Support Started",
                Message = $"You're now supporting {birdName} every week! We'll remind you when it's time to approve.",
                Priority = NotificationPriority.Medium,
                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                DeepLink = $"/weekly-support/subscriptions/{subscriptionId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription created notification");
        }
    }

    private async Task SendWeeklyReminderNotificationAsync(Guid userId, string birdName, Guid paymentId, decimal amount)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                Type = NotificationType.WeeklySupportReminder,
                Title = $"Time to support {birdName}!",
                Message = $"Your weekly ${amount:F2} support is ready. Tap to approve in 1 click.",
                Priority = NotificationPriority.High,
                Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                DeepLink = $"/weekly-support/approve/{paymentId}",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { paymentId, amount })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send weekly reminder notification");
        }
    }

    private async Task SendMissedPaymentNotificationAsync(Guid userId, string birdName, Guid subscriptionId)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                Type = NotificationType.WeeklySupportMissed,
                Title = "Weekly Support Missed",
                Message = $"You missed supporting {birdName} this week. Don't worry - we'll remind you next week!",
                Priority = NotificationPriority.Medium,
                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                DeepLink = $"/weekly-support/subscriptions/{subscriptionId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send missed payment notification");
        }
    }

    private async Task SendAutoPausedNotificationAsync(Guid userId, string birdName, Guid subscriptionId)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                Type = NotificationType.WeeklySupportMissed,
                Title = "Weekly Support Paused",
                Message = $"Your weekly support for {birdName} has been paused after 3 missed weeks. Resume anytime!",
                Priority = NotificationPriority.High,
                Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                DeepLink = $"/weekly-support/subscriptions/{subscriptionId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send auto-paused notification");
        }
    }

    #endregion
}
