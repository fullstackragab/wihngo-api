using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// PayPal Payouts API integration
    /// </summary>
    public class PayPalPayoutService : IPayPalPayoutService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PayPalPayoutService> _logger;
        private readonly PayPalConfiguration _config;

        public PayPalPayoutService(
            HttpClient httpClient,
            ILogger<PayPalPayoutService> logger,
            IOptions<PayPalConfiguration> config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;
        }

        public async Task<(bool Success, string? TransactionId, string? Error)> ProcessPayoutAsync(
            string paypalEmail,
            decimal amount,
            string note)
        {
            try
            {
                // In test mode, simulate success
                var isSandbox = _config.BaseUrl?.Contains("sandbox") ?? true;
                if (isSandbox || string.IsNullOrEmpty(_config.ClientId))
                {
                    _logger.LogWarning("PAYPAL TEST MODE: Simulating payout for {Amount} EUR to {Email}", amount, paypalEmail);
                    var testTxId = $"PAYPAL_TEST_{Guid.NewGuid().ToString()[..8]}";
                    await Task.Delay(500); // Simulate API delay
                    return (true, testTxId, null);
                }

                // Get access token
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return (false, null, "Failed to obtain PayPal access token");
                }

                // Create payout batch
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var payoutRequest = new
                {
                    sender_batch_header = new
                    {
                        sender_batch_id = Guid.NewGuid().ToString(),
                        email_subject = "You have a payment from Wihngo",
                        email_message = "You have received a payout for your bird's earnings on Wihngo."
                    },
                    items = new[]
                    {
                        new
                        {
                            recipient_type = "EMAIL",
                            amount = new
                            {
                                value = amount.ToString("F2"),
                                currency = "EUR"
                            },
                            receiver = paypalEmail,
                            note,
                            sender_item_id = Guid.NewGuid().ToString()
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_config.BaseUrl}/v1/payments/payouts",
                    payoutRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create PayPal payout: {Error}", error);
                    return (false, null, $"PayPal API error: {error}");
                }

                var payoutData = await response.Content.ReadFromJsonAsync<JsonElement>();
                var batchId = payoutData.GetProperty("batch_header").GetProperty("payout_batch_id").GetString();

                _logger.LogInformation("Successfully created PayPal payout: {BatchId}", batchId);
                return (true, batchId, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal payout");
                return (false, null, ex.Message);
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var authString = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = await _httpClient.PostAsync(
                    $"{_config.BaseUrl}/v1/oauth2/token",
                    requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get PayPal access token");
                    return null;
                }

                var tokenData = await response.Content.ReadFromJsonAsync<JsonElement>();
                return tokenData.GetProperty("access_token").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal access token");
                return null;
            }
        }
    }
}
