using Hangfire;
using Microsoft.EntityFrameworkCore;
using Wihngo.Data;
using Wihngo.Models.Entities;

namespace Wihngo.BackgroundJobs;

public class ExchangeRateUpdateJob
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExchangeRateUpdateJob> _logger;

    public ExchangeRateUpdateJob(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExchangeRateUpdateJob> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task UpdateExchangeRatesAsync()
    {
        _logger.LogInformation("Updating crypto exchange rates...");

        try
        {
            var currencies = new Dictionary<string, string>
            {
                { "bitcoin", "BTC" },
                { "ethereum", "ETH" },
                { "tether", "USDT" },
                { "usd-coin", "USDC" },
                { "binancecoin", "BNB" },
                { "solana", "SOL" },
                { "dogecoin", "DOGE" }
            };

            var client = _httpClientFactory.CreateClient();
            var apiKey = _configuration["ExchangeRateSettings:CoinGeckoApiKey"];

            var url = $"https://api.coingecko.com/api/v3/simple/price?ids={string.Join(",", currencies.Keys)}&vs_currencies=usd";

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("x-cg-pro-api-key", apiKey);
            }

            var response = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(url);

            if (response != null)
            {
                foreach (var (coinId, code) in currencies)
                {
                    if (response.TryGetValue(coinId, out var prices) && prices.TryGetValue("usd", out var usdRate))
                    {
                        var existingRate = await _context.CryptoExchangeRates
                            .FirstOrDefaultAsync(r => r.Currency == code);

                        if (existingRate != null)
                        {
                            existingRate.UsdRate = usdRate;
                            existingRate.LastUpdated = DateTime.UtcNow;
                        }
                        else
                        {
                            _context.CryptoExchangeRates.Add(new CryptoExchangeRate
                            {
                                Currency = code,
                                UsdRate = usdRate,
                                Source = "coingecko",
                                LastUpdated = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Exchange rates updated successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update exchange rates");
            throw;
        }
    }
}
