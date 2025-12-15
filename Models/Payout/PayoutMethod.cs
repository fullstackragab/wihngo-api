using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wihngo.Models.Enums;

namespace Wihngo.Models.Payout
{
    public class PayoutMethod
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public PayoutMethodType MethodType { get; set; }

        public bool IsDefault { get; set; } = false;

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // IBAN/SEPA fields
        [MaxLength(255)]
        public string? AccountHolderName { get; set; }

        [MaxLength(34)]
        public string? Iban { get; set; }

        [MaxLength(11)]
        public string? Bic { get; set; }

        [MaxLength(255)]
        public string? BankName { get; set; }

        // PayPal fields
        [MaxLength(255)]
        [EmailAddress]
        public string? PayPalEmail { get; set; }

        // Crypto fields
        [MaxLength(255)]
        public string? WalletAddress { get; set; }

        [MaxLength(50)]
        public string? Network { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        // Navigation properties
        public List<PayoutTransaction> PayoutTransactions { get; set; } = new();

        // Helper method to get display name
        public string GetDisplayName()
        {
            return MethodType switch
            {
                PayoutMethodType.Iban => $"IBAN (***{Iban?[^4..]})",
                PayoutMethodType.PayPal => $"PayPal ({PayPalEmail})",
                PayoutMethodType.UsdcSolana => $"USDC (Solana)",
                PayoutMethodType.EurcSolana => $"EURC (Solana)",
                PayoutMethodType.UsdcBase => $"USDC (Base)",
                PayoutMethodType.EurcBase => $"EURC (Base)",
                _ => "Unknown"
            };
        }

        // Helper method to mask sensitive data
        public string GetMaskedIban()
        {
            if (string.IsNullOrEmpty(Iban) || Iban.Length < 8)
                return Iban ?? string.Empty;

            return $"{Iban[..4]}****{Iban[^4..]}";
        }

        public string GetMaskedWalletAddress()
        {
            if (string.IsNullOrEmpty(WalletAddress) || WalletAddress.Length < 12)
                return WalletAddress ?? string.Empty;

            return $"{WalletAddress[..6]}...{WalletAddress[^4..]}";
        }
    }
}
