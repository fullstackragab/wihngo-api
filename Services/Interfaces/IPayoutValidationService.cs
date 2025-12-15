using System.Threading.Tasks;

namespace Wihngo.Services.Interfaces
{
    public interface IPayoutValidationService
    {
        /// <summary>
        /// Validates IBAN format and checksum
        /// </summary>
        Task<(bool IsValid, string? Error)> ValidateIbanAsync(string iban);

        /// <summary>
        /// Validates PayPal email format
        /// </summary>
        Task<(bool IsValid, string? Error)> ValidatePayPalEmailAsync(string email);

        /// <summary>
        /// Validates Solana wallet address
        /// </summary>
        Task<(bool IsValid, string? Error)> ValidateSolanaAddressAsync(string address);

        /// <summary>
        /// Validates Base/EVM wallet address
        /// </summary>
        Task<(bool IsValid, string? Error)> ValidateBaseAddressAsync(string address);
    }
}
