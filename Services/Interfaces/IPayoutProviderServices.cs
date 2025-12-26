using System;
using System.Threading.Tasks;

namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Wise API integration for IBAN/SEPA transfers
    /// </summary>
    public interface IWisePayoutService
    {
        Task<(bool Success, string? TransactionId, string? Error)> ProcessPayoutAsync(
            string iban,
            string bic,
            string accountHolderName,
            decimal amount,
            string reference);
    }

    /// <summary>
    /// PayPal Payouts API integration
    /// </summary>
    public interface IPayPalPayoutService
    {
        Task<(bool Success, string? TransactionId, string? Error)> ProcessPayoutAsync(
            string paypalEmail,
            decimal amount,
            string note);
    }
}
