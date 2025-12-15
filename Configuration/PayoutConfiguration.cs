namespace Wihngo.Configuration
{
    public class WiseConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ProfileId { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.transferwise.com";
        public bool IsTestMode { get; set; } = true;
    }

    public class PayoutConfiguration
    {
        public decimal MinimumPayoutAmount { get; set; } = 20.00m;
        public decimal PlatformFeePercentage { get; set; } = 5.0m;
        public int PayoutDayOfMonth { get; set; } = 1;
        public string DefaultCurrency { get; set; } = "EUR";
    }
}
