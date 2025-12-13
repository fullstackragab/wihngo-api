namespace Wihngo.Dtos
{
    using System;

    public class StorySummaryDto
    {
        public Guid StoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Bird { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        
        /// <summary>
        /// S3 key for story image
        /// </summary>
        public string? ImageS3Key { get; set; }
        
        /// <summary>
        /// Pre-signed download URL for story image (expires in 10 minutes)
        /// </summary>
        public string? ImageUrl { get; set; }
    }
}
