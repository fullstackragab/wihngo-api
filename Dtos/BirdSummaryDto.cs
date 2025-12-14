namespace Wihngo.Dtos
{
    using System;

    public class BirdSummaryDto
    {
        public Guid BirdId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Species { get; set; }
        
        /// <summary>
        /// S3 key for bird profile image
        /// </summary>
        public string? ImageS3Key { get; set; }
        
        /// <summary>
        /// Pre-signed download URL for bird profile image (expires in 10 minutes)
        /// </summary>
        public string? ImageUrl { get; set; }
        
        public string? Tagline { get; set; }
        public int LovedBy { get; set; }
        public int SupportedBy { get; set; }
        public Guid OwnerId { get; set; }
        public bool IsLoved { get; set; }
    }
}
