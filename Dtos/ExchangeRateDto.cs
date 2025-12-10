namespace Wihngo.Dtos;

public class ExchangeRateDto
{
    public string Currency { get; set; } = string.Empty;
    public decimal UsdRate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Source { get; set; } = string.Empty;
}
