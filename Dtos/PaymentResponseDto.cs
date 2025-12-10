namespace Wihngo.Dtos;

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? BirdId { get; set; }
    public decimal AmountUsd { get; set; }
    public decimal AmountCrypto { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string? UserWalletAddress { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
    public string PaymentUri { get; set; } = string.Empty;
    public string? TransactionHash { get; set; }
    public int Confirmations { get; set; }
    public int RequiredConfirmations { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Plan { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
