namespace Wihngo.Dtos
{
    public class PremiumPlanDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public string Interval { get; set; } = string.Empty;
        public string? Savings { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CharityAllocation { get; set; }
        public string[] Features { get; set; } = Array.Empty<string>();
    }
}
