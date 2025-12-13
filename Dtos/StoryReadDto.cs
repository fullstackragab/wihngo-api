namespace Wihngo.Dtos
{
    using System;

    public class StoryReadDto
    {
        public Guid StoryId { get; set; }
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// S3 key for story image
        /// </summary>
        public string? ImageS3Key { get; set; }
        
        /// <summary>
        /// Pre-signed download URL for story image (expires in 10 minutes)
        /// </summary>
        public string? ImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public BirdSummaryDto Bird { get; set; } = new BirdSummaryDto();
        public UserSummaryDto Author { get; set; } = new UserSummaryDto();
    }
}
