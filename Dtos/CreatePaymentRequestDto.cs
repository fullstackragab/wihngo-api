using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

public class CreatePaymentRequestDto
{
    public Guid? BirdId { get; set; }

    [Required]
    [Range(5, 100000)]
    public decimal AmountUsd { get; set; }

    [Required]
    [RegularExpression("^(USDC|EURC)$", ErrorMessage = "Only USDC and EURC are supported currencies")]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(solana)$", ErrorMessage = "Only Solana network is supported")]
    public string Network { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(premium_subscription|donation|purchase)$")]
    public string Purpose { get; set; } = string.Empty;

    [RegularExpression("^(monthly|yearly|lifetime)$")]
    public string? Plan { get; set; }
}
