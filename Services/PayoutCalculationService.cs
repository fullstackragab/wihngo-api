using System;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class PayoutCalculationService : IPayoutCalculationService
    {
        private const decimal PLATFORM_FEE_PERCENTAGE = 0.05m; // 5%
        private const decimal BIRD_OWNER_PERCENTAGE = 0.95m; // 95%
        private const decimal MINIMUM_PAYOUT_THRESHOLD = 20.00m; // €20

        // Provider fee percentages
        private const decimal IBAN_SEPA_FEE = 1.00m; // €1 flat fee for SEPA
        private const decimal PAYPAL_FEE_PERCENTAGE = 0.02m; // 2% for PayPal
        private const decimal CRYPTO_FEE = 0.01m; // ~€0.01 for crypto (very low)

        public decimal CalculatePlatformFee(decimal amount)
        {
            return Math.Round(amount * PLATFORM_FEE_PERCENTAGE, 2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateProviderFee(decimal amount, string methodType)
        {
            return methodType.ToLowerInvariant() switch
            {
                "iban" => IBAN_SEPA_FEE, // Flat €1 for IBAN/SEPA
                "paypal" => Math.Round(amount * PAYPAL_FEE_PERCENTAGE, 2, MidpointRounding.AwayFromZero), // 2% for PayPal
                "usdc-solana" or "eurc-solana" => CRYPTO_FEE, // Very low fee for Solana
                "usdc-base" or "eurc-base" => CRYPTO_FEE, // Very low fee for Base
                _ => 0m
            };
        }

        public decimal CalculateNetAmount(decimal amount, decimal platformFee, decimal providerFee)
        {
            var netAmount = amount - platformFee - providerFee;
            return Math.Max(0, Math.Round(netAmount, 2, MidpointRounding.AwayFromZero));
        }

        public decimal CalculateBirdOwnerEarnings(decimal supportAmount)
        {
            // Bird owner gets 95% of support (5% platform fee already deducted)
            return Math.Round(supportAmount * BIRD_OWNER_PERCENTAGE, 2, MidpointRounding.AwayFromZero);
        }

        public bool MeetsMinimumThreshold(decimal amount)
        {
            return amount >= MINIMUM_PAYOUT_THRESHOLD;
        }
    }
}
