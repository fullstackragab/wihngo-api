namespace Wihngo.Dtos
{
    using System;

    public class BirdSummaryDto
    {
        public Guid BirdId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Species { get; set; }
        public string? ImageUrl { get; set; }
        public string? Tagline { get; set; }
        public int LovedBy { get; set; }
        public int SupportedBy { get; set; }
        public Guid OwnerId { get; set; }
    }
}
