namespace Wihngo.Dtos
{
    using System;

    public class SupportUsageDto
    {
        public Guid UsageId { get; set; }
        public Guid BirdId { get; set; }
        public Guid ReportedBy { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
