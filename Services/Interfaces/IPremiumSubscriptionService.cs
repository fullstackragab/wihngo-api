namespace Wihngo.Services.Interfaces
{
    using Wihngo.Dtos;

    public interface IPremiumSubscriptionService
    {
        Task<SubscriptionResponseDto> CreateSubscriptionAsync(SubscribeDto request, Guid userId);
        Task<PremiumStatusResponseDto> GetPremiumStatusAsync(Guid birdId);
        Task CancelSubscriptionAsync(Guid birdId, Guid userId);
        Task<PremiumStyleDto> UpdatePremiumStyleAsync(Guid birdId, UpdatePremiumStyleDto request, Guid userId);
        Task<PremiumStyleDto?> GetPremiumStyleAsync(Guid birdId);
    }
}
