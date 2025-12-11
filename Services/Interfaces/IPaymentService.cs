namespace Wihngo.Services.Interfaces
{
    using Wihngo.Dtos;

    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(SubscribeDto request);
        Task CancelSubscriptionAsync(string provider, string subscriptionId);
    }

    public class PaymentResult
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
