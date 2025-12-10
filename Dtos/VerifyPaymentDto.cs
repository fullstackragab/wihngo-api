using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

public class VerifyPaymentDto
{
    [Required]
    [MinLength(32)]
    public string TransactionHash { get; set; } = string.Empty;

    public string? UserWalletAddress { get; set; }
}
