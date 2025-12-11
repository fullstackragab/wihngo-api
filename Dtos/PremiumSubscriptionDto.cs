namespace Wihngo.Dtos
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class SubscribeDto
    {
        [Required]
        public Guid BirdId { get; set; }

        public string? PaymentMethodId { get; set; }

        [Required]
        public string Provider { get; set; } = string.Empty; // stripe, apple, google, crypto

        [Required]
        public string Plan { get; set; } = string.Empty; // monthly, yearly, lifetime

        public string? CryptoCurrency { get; set; }
        public string? CryptoNetwork { get; set; }
    }

    public class SubscriptionResponseDto
    {
        public Guid SubscriptionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public DateTime CurrentPeriodEnd { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PremiumStatusResponseDto
    {
        public bool IsPremium { get; set; }
        public BirdPremiumSubscriptionDto? Subscription { get; set; }
    }

    public class BirdPremiumSubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid BirdId { get; set; }
        public Guid OwnerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? ProviderSubscriptionId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        public DateTime? CanceledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdatePremiumStyleDto
    {
        public string? FrameId { get; set; }
        public string? BadgeId { get; set; }
        public string? HighlightColor { get; set; }
        public string? ThemeId { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
