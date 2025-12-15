using System;

namespace Wihngo.Dtos
{
    public class PayoutBalanceDto
    {
        public Guid UserId { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public string Currency { get; set; } = "EUR";
        public DateTime? NextPayoutDate { get; set; }
        public bool MinimumReached { get; set; }
        public PayoutSummaryDto? Summary { get; set; }
    }

    public class PayoutSummaryDto
    {
        public decimal TotalEarned { get; set; }
        public decimal TotalPaidOut { get; set; }
        public decimal PlatformFeePaid { get; set; }
        public decimal ProviderFeesPaid { get; set; }
    }
}
