namespace Wihngo.Services
{
    using Wihngo.Dtos;
    using Wihngo.Services.Interfaces;

    public class PaymentService : IPaymentService
    {
        private readonly ICryptoPaymentService _cryptoPaymentService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            ICryptoPaymentService cryptoPaymentService,
            ILogger<PaymentService> logger)
        {
            _cryptoPaymentService = cryptoPaymentService;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(SubscribeDto request)
        {
            try
            {
                switch (request.Provider.ToLower())
                {
                    case "crypto":
                        return await ProcessCryptoPaymentAsync(request);

                    case "stripe":
                    case "apple":
                    case "google":
                        return new PaymentResult
                        {
                            SubscriptionId = $"{request.Provider}_{Guid.NewGuid()}",
                            Success = true
                        };

                    default:
                        return new PaymentResult
                        {
                            Success = false,
                            ErrorMessage = $"Unsupported payment provider: {request.Provider}"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed for provider {Provider}", request.Provider);
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task CancelSubscriptionAsync(string provider, string subscriptionId)
        {
            _logger.LogInformation("Canceling subscription {SubscriptionId} for provider {Provider}", subscriptionId, provider);
            
            // For crypto payments, there's no recurring subscription to cancel
            // For other providers (Stripe, Apple, Google), implement their cancellation logic here
            
            await Task.CompletedTask;
        }

        private async Task<PaymentResult> ProcessCryptoPaymentAsync(SubscribeDto request)
        {
            if (string.IsNullOrEmpty(request.CryptoCurrency) || string.IsNullOrEmpty(request.CryptoNetwork))
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Crypto currency and network are required for crypto payments"
                };
            }

            // Crypto payments are handled separately via CryptoPaymentService
            // For premium subscriptions, we'll simulate successful payment
            // In production, you would create a payment request and wait for confirmation
            
            _logger.LogInformation("Processing crypto payment for plan {Plan}", request.Plan);

            return new PaymentResult
            {
                SubscriptionId = $"crypto_{Guid.NewGuid()}",
                Success = true
            };
        }
    }
}
