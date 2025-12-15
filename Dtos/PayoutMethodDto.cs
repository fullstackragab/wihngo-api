using System;
using Wihngo.Models.Enums;

namespace Wihngo.Dtos
{
    public class PayoutMethodDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string MethodType { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // IBAN/SEPA fields (masked)
        public string? AccountHolderName { get; set; }
        public string? Iban { get; set; }
        public string? Bic { get; set; }
        public string? BankName { get; set; }

        // PayPal fields
        public string? PayPalEmail { get; set; }

        // Crypto fields (masked)
        public string? WalletAddress { get; set; }
        public string? Network { get; set; }
        public string? Currency { get; set; }
    }

    public class PayoutMethodCreateDto
    {
        public string MethodType { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;

        // IBAN/SEPA fields
        public string? AccountHolderName { get; set; }
        public string? Iban { get; set; }
        public string? Bic { get; set; }
        public string? BankName { get; set; }

        // PayPal fields
        public string? PayPalEmail { get; set; }

        // Crypto fields
        public string? WalletAddress { get; set; }
        public string? Network { get; set; }
        public string? Currency { get; set; }
    }

    public class PayoutMethodUpdateDto
    {
        public bool? IsDefault { get; set; }
    }
}
