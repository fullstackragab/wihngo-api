using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    /// <summary>
    /// Wise (formerly TransferWise) API integration for IBAN/SEPA transfers
    /// </summary>
    public class WisePayoutService : IWisePayoutService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WisePayoutService> _logger;
        private readonly WiseConfiguration _config;

        public WisePayoutService(
            HttpClient httpClient,
            ILogger<WisePayoutService> logger,
            IOptions<WiseConfiguration> config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;
        }

        public async Task<(bool Success, string? TransactionId, string? Error)> ProcessPayoutAsync(
            string iban,
            string bic,
            string accountHolderName,
            decimal amount,
            string reference)
        {
            try
            {
                // In test mode, simulate success
                if (_config.IsTestMode || string.IsNullOrEmpty(_config.ApiKey))
                {
                    _logger.LogWarning("WISE TEST MODE: Simulating payout for {Amount} EUR to {IBAN}", amount, iban);
                    var testTxId = $"WISE_TEST_{Guid.NewGuid().ToString()[..8]}";
                    await Task.Delay(500); // Simulate API delay
                    return (true, testTxId, null);
                }

                // Real Wise API implementation
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                // Step 1: Create recipient
                var recipientResponse = await _httpClient.PostAsJsonAsync(
                    $"{_config.BaseUrl}/v1/accounts",
                    new
                    {
                        currency = "EUR",
                        type = "iban",
                        profile = _config.ProfileId,
                        ownedByCustomer = false,
                        accountHolderName,
                        details = new
                        {
                            IBAN = iban,
                            BIC = bic
                        }
                    });

                if (!recipientResponse.IsSuccessStatusCode)
                {
                    var error = await recipientResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create Wise recipient: {Error}", error);
                    return (false, null, $"Failed to create recipient: {error}");
                }

                var recipientData = await recipientResponse.Content.ReadFromJsonAsync<JsonElement>();
                var recipientId = recipientData.GetProperty("id").GetInt64();

                // Step 2: Create quote
                var quoteResponse = await _httpClient.PostAsJsonAsync(
                    $"{_config.BaseUrl}/v3/profiles/{_config.ProfileId}/quotes",
                    new
                    {
                        sourceCurrency = "EUR",
                        targetCurrency = "EUR",
                        sourceAmount = amount,
                        targetAccount = recipientId
                    });

                if (!quoteResponse.IsSuccessStatusCode)
                {
                    var error = await quoteResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create Wise quote: {Error}", error);
                    return (false, null, $"Failed to create quote: {error}");
                }

                var quoteData = await quoteResponse.Content.ReadFromJsonAsync<JsonElement>();
                var quoteId = quoteData.GetProperty("id").GetString();

                // Step 3: Create transfer
                var transferResponse = await _httpClient.PostAsJsonAsync(
                    $"{_config.BaseUrl}/v1/transfers",
                    new
                    {
                        targetAccount = recipientId,
                        quoteUuid = quoteId,
                        customerTransactionId = Guid.NewGuid().ToString(),
                        details = new
                        {
                            reference
                        }
                    });

                if (!transferResponse.IsSuccessStatusCode)
                {
                    var error = await transferResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create Wise transfer: {Error}", error);
                    return (false, null, $"Failed to create transfer: {error}");
                }

                var transferData = await transferResponse.Content.ReadFromJsonAsync<JsonElement>();
                var transferId = transferData.GetProperty("id").GetInt64().ToString();

                _logger.LogInformation("Successfully created Wise payout: {TransferId}", transferId);
                return (true, transferId, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Wise payout");
                return (false, null, ex.Message);
            }
        }
    }
}
