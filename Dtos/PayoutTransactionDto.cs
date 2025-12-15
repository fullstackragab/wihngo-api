using System;

namespace Wihngo.Dtos
{
    public class PayoutTransactionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PayoutMethodId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Status { get; set; } = string.Empty;
        public string MethodType { get; set; } = string.Empty;

        // Fee breakdown
        public decimal PlatformFee { get; set; }
        public decimal ProviderFee { get; set; }
        public decimal NetAmount { get; set; }

        // Processing details
        public DateTime ScheduledAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? FailureReason { get; set; }
        public string? TransactionId { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class PayoutHistoryResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<PayoutTransactionDto> Items { get; set; } = new();
    }

    public class ProcessPayoutRequestDto
    {
        public Guid? UserId { get; set; }
    }

    public class ProcessPayoutResponseDto
    {
        public int Processed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PayoutProcessDetailDto> Details { get; set; } = new();
    }

    public class PayoutProcessDetailDto
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? Error { get; set; }
    }
}
