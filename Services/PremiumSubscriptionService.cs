namespace Wihngo.Services
{
    using System;
    using System.Collections.Generic;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Services.Interfaces;

    public class PremiumSubscriptionService : IPremiumSubscriptionService
    {
        private readonly AppDbContext _context;
        private readonly ICharityService _charityService;
        private readonly ILogger<PremiumSubscriptionService> _logger;

        public PremiumSubscriptionService(
            AppDbContext context,
            ICharityService charityService,
            ILogger<PremiumSubscriptionService> logger)
        {
            _context = context;
            _charityService = charityService;
            _logger = logger;
        }

        public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(SubscribeDto request, Guid userId)
        {
            var bird = await _context.Birds.FirstOrDefaultAsync(b => b.BirdId == request.BirdId);

            if (bird == null)
                throw new ArgumentException("Bird not found");

            if (bird.OwnerId != userId)
                throw new UnauthorizedAccessException("You don't own this bird");

            var existing = await _context.BirdPremiumSubscriptions
                .FirstOrDefaultAsync(s => s.BirdId == request.BirdId && s.Status == "active");

            if (existing != null)
                throw new ArgumentException("Bird already has an active subscription");

            // TODO: Integrate with new P2P payment system
            // For now, premium subscriptions are created after payment is confirmed externally
            var startDate = DateTime.UtcNow;
            var periodEnd = request.Plan switch
            {
                "monthly" => startDate.AddMonths(1),
                "yearly" => startDate.AddYears(1),
                "lifetime" => startDate.AddYears(100),
                _ => throw new ArgumentException("Invalid plan")
            };

            var subscription = new BirdPremiumSubscription
            {
                Id = Guid.NewGuid(),
                BirdId = request.BirdId,
                OwnerId = userId,
                Status = "active",
                Plan = request.Plan,
                Provider = request.Provider,
                ProviderSubscriptionId = null, // Set when payment is confirmed
                StartedAt = startDate,
                CurrentPeriodEnd = periodEnd,
                PriceCents = GetPlanPriceCents(request.Plan),
                DurationDays = GetPlanDurationDays(request.Plan),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BirdPremiumSubscriptions.Add(subscription);

            bird.IsPremium = true;
            bird.PremiumPlan = request.Plan;
            bird.PremiumExpiresAt = periodEnd;
            bird.MaxMediaCount = 50;

            var price = GetPlanPrice(request.Plan);
            var charityPercentage = GetCharityPercentage(request.Plan);
            var charityAmount = price * (charityPercentage / 100m);

            await _charityService.RecordCharityAllocationAsync(subscription.Id, charityAmount, charityPercentage);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Premium subscription created for bird {BirdId} with plan {Plan}", request.BirdId, request.Plan);

            return new SubscriptionResponseDto
            {
                SubscriptionId = subscription.Id,
                Status = subscription.Status,
                Plan = subscription.Plan,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                Message = "Premium subscription activated successfully!"
            };
        }

        public async Task<PremiumStatusResponseDto> GetPremiumStatusAsync(Guid birdId)
        {
            var subscription = await _context.BirdPremiumSubscriptions
                .Where(s => s.BirdId == birdId && s.Status == "active")
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            return new PremiumStatusResponseDto
            {
                IsPremium = subscription != null,
                Subscription = subscription != null ? MapToDto(subscription) : null
            };
        }

        public async Task CancelSubscriptionAsync(Guid birdId, Guid userId)
        {
            var subscription = await _context.BirdPremiumSubscriptions
                .Include(s => s.Bird)
                .FirstOrDefaultAsync(s => s.BirdId == birdId && s.Status == "active");

            if (subscription == null)
                throw new ArgumentException("No active subscription found");

            if (subscription.OwnerId != userId)
                throw new UnauthorizedAccessException("You don't own this subscription");

            // TODO: Handle external payment provider cancellation if needed
            // For now, subscription cancellation only updates local state

            subscription.Status = "canceled";
            subscription.CanceledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (subscription.Bird != null)
            {
                subscription.Bird.IsPremium = false;
                subscription.Bird.PremiumPlan = null;
                subscription.Bird.PremiumExpiresAt = null;
                subscription.Bird.MaxMediaCount = 5;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Premium subscription canceled for bird {BirdId}", birdId);
        }

        public async Task<PremiumStyleDto> UpdatePremiumStyleAsync(Guid birdId, UpdatePremiumStyleDto request, Guid userId)
        {
            var bird = await _context.Birds.FirstOrDefaultAsync(b => b.BirdId == birdId);

            if (bird == null)
                throw new ArgumentException("Bird not found");

            if (bird.OwnerId != userId)
                throw new UnauthorizedAccessException("You don't own this bird");

            if (!bird.IsPremium)
                throw new ArgumentException("Bird does not have premium");

            var style = await _context.Set<PremiumStyle>()
                .FirstOrDefaultAsync(s => s.BirdId == birdId);

            if (style == null)
            {
                style = new PremiumStyle
                {
                    Id = Guid.NewGuid(),
                    BirdId = birdId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Set<PremiumStyle>().Add(style);
            }

            if (request.FrameId != null) style.FrameId = request.FrameId;
            if (request.BadgeId != null) style.BadgeId = request.BadgeId;
            if (request.HighlightColor != null) style.HighlightColor = request.HighlightColor;
            if (request.ThemeId != null) style.ThemeId = request.ThemeId;
            if (request.CoverImageUrl != null) style.CoverImageUrl = request.CoverImageUrl;

            style.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Premium style updated for bird {BirdId}", birdId);

            return MapStyleToDto(style);
        }

        public async Task<PremiumStyleDto?> GetPremiumStyleAsync(Guid birdId)
        {
            var style = await _context.Set<PremiumStyle>()
                .FirstOrDefaultAsync(s => s.BirdId == birdId);

            return style != null ? MapStyleToDto(style) : null;
        }

        private long GetPlanPriceCents(string plan) => plan switch
        {
            "monthly" => 399,
            "yearly" => 3999,
            "lifetime" => 6999,
            _ => 0
        };

        private int GetPlanDurationDays(string plan) => plan switch
        {
            "monthly" => 30,
            "yearly" => 365,
            "lifetime" => 36500,
            _ => 0
        };

        private decimal GetPlanPrice(string plan) => plan switch
        {
            "monthly" => 3.99m,
            "yearly" => 39.99m,
            "lifetime" => 69.99m,
            _ => 0
        };

        private decimal GetCharityPercentage(string plan) => plan switch
        {
            "monthly" => 10,
            "yearly" => 15,
            "lifetime" => 20,
            _ => 0
        };

        private BirdPremiumSubscriptionDto MapToDto(BirdPremiumSubscription sub) =>
            new BirdPremiumSubscriptionDto
            {
                Id = sub.Id,
                BirdId = sub.BirdId,
                OwnerId = sub.OwnerId,
                Status = sub.Status,
                Plan = sub.Plan,
                Provider = sub.Provider,
                ProviderSubscriptionId = sub.ProviderSubscriptionId,
                StartedAt = sub.StartedAt,
                CurrentPeriodEnd = sub.CurrentPeriodEnd,
                CanceledAt = sub.CanceledAt,
                CreatedAt = sub.CreatedAt,
                UpdatedAt = sub.UpdatedAt
            };

        private PremiumStyleDto MapStyleToDto(PremiumStyle style) =>
            new PremiumStyleDto
            {
                FrameId = style.FrameId,
                BadgeId = style.BadgeId,
                HighlightColor = style.HighlightColor,
                CoverImageUrl = style.CoverImageUrl,
                FrameColor = null,
                BadgeText = null
            };
    }
}
