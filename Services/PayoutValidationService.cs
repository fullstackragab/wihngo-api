using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class PayoutValidationService : IPayoutValidationService
    {
        public Task<(bool IsValid, string? Error)> ValidateIbanAsync(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
                return Task.FromResult<(bool, string?)>((false, "IBAN is required"));

            // Remove spaces and convert to uppercase
            iban = iban.Replace(" ", "").ToUpperInvariant();

            // IBAN length validation (15-34 characters)
            if (iban.Length < 15 || iban.Length > 34)
                return Task.FromResult<(bool, string?)>((false, "IBAN must be between 15 and 34 characters"));

            // IBAN format validation (2 letters + 2 digits + alphanumeric)
            if (!Regex.IsMatch(iban, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$"))
                return Task.FromResult<(bool, string?)>((false, "Invalid IBAN format"));

            // IBAN checksum validation (mod-97 algorithm)
            try
            {
                var rearranged = iban.Substring(4) + iban.Substring(0, 4);
                var numericIban = string.Concat(rearranged.Select(c => 
                    char.IsLetter(c) ? ((int)c - 55).ToString() : c.ToString()));

                var remainder = 0;
                foreach (var digit in numericIban)
                {
                    remainder = (remainder * 10 + (digit - '0')) % 97;
                }

                if (remainder != 1)
                    return Task.FromResult<(bool, string?)>((false, "Invalid IBAN checksum"));
            }
            catch
            {
                return Task.FromResult<(bool, string?)>((false, "Invalid IBAN format"));
            }

            return Task.FromResult<(bool, string?)>((true, null));
        }

        public Task<(bool IsValid, string? Error)> ValidatePayPalEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Task.FromResult<(bool, string?)>((false, "PayPal email is required"));

            // Email format validation
            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, emailRegex, RegexOptions.IgnoreCase))
                return Task.FromResult<(bool, string?)>((false, "Invalid email format"));

            // Email length validation
            if (email.Length > 255)
                return Task.FromResult<(bool, string?)>((false, "Email too long (max 255 characters)"));

            return Task.FromResult<(bool, string?)>((true, null));
        }

        public Task<(bool IsValid, string? Error)> ValidateSolanaAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return Task.FromResult<(bool, string?)>((false, "Solana wallet address is required"));

            // Solana addresses are base58 encoded and typically 32-44 characters
            if (address.Length < 32 || address.Length > 44)
                return Task.FromResult<(bool, string?)>((false, "Solana address must be between 32 and 44 characters"));

            // Base58 validation (no 0, O, I, l)
            var base58Regex = @"^[1-9A-HJ-NP-Za-km-z]+$";
            if (!Regex.IsMatch(address, base58Regex))
                return Task.FromResult<(bool, string?)>((false, "Invalid Solana address format (must be base58)"));

            return Task.FromResult<(bool, string?)>((true, null));
        }

        public Task<(bool IsValid, string? Error)> ValidateBaseAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return Task.FromResult<(bool, string?)>((false, "Base wallet address is required"));

            // EVM addresses start with 0x and are 42 characters (40 hex + 0x)
            if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<(bool, string?)>((false, "Base address must start with 0x"));

            if (address.Length != 42)
                return Task.FromResult<(bool, string?)>((false, "Base address must be 42 characters (0x + 40 hex)"));

            // Hex validation (0-9, a-f, A-F)
            var hexRegex = @"^0x[0-9a-fA-F]{40}$";
            if (!Regex.IsMatch(address, hexRegex))
                return Task.FromResult<(bool, string?)>((false, "Invalid Base address format (must be hex)"));

            return Task.FromResult<(bool, string?)>((true, null));
        }
    }
}
