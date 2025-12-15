using System;
using System.Threading.Tasks;

namespace Wihngo.Services.Interfaces
{
    public interface IPayoutCalculationService
    {
        /// <summary>
        /// Calculate platform fee (5% of amount)
        /// </summary>
        decimal CalculatePlatformFee(decimal amount);

        /// <summary>
        /// Calculate provider fee based on payout method
        /// </summary>
        decimal CalculateProviderFee(decimal amount, string methodType);

        /// <summary>
        /// Calculate net amount (amount - platform fee - provider fee)
        /// </summary>
        decimal CalculateNetAmount(decimal amount, decimal platformFee, decimal providerFee);

        /// <summary>
        /// Calculate bird owner earnings (95% of support amount)
        /// </summary>
        decimal CalculateBirdOwnerEarnings(decimal supportAmount);

        /// <summary>
        /// Check if amount meets minimum payout threshold (€20)
        /// </summary>
        bool MeetsMinimumThreshold(decimal amount);
    }
}
