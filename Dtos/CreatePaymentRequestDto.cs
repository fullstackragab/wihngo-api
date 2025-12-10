using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

public class CreatePaymentRequestDto
{
    public Guid? BirdId { get; set; }

    [Required]
    [Range(5, 100000)]
    public decimal AmountUsd { get; set; }

    [Required]
    [RegularExpression("^(BTC|ETH|USDT|USDC|BNB|SOL|DOGE)$")]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(bitcoin|ethereum|tron|binance-smart-chain|polygon|solana)$")]
    public string Network { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(premium_subscription|donation|purchase)$")]
    public string Purpose { get; set; } = string.Empty;

    [RegularExpression("^(monthly|yearly|lifetime)$")]
    public string? Plan { get; set; }
}
