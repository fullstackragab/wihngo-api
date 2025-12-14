namespace Wihngo.Dtos
{
    using System;
    using System.Collections.Generic;
    using Wihngo.Models.Enums;

    public class StoryReadDto
    {
        public Guid StoryId { get; set; }
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional mood/category. Null means no mood selected.
        /// </summary>
        public StoryMode? Mode { get; set; }
        
        /// <summary>
        /// S3 key for story image
        /// </summary>
        public string? ImageS3Key { get; set; }
        
        /// <summary>
        /// Pre-signed download URL for story image (expires in 10 minutes)
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// S3 key for story video
        /// </summary>
        public string? VideoS3Key { get; set; }
        
        /// <summary>
        /// Pre-signed download URL for story video (expires in 10 minutes)
        /// </summary>
        public string? VideoUrl { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public List<BirdSummaryDto> Birds { get; set; } = new List<BirdSummaryDto>();
        public UserSummaryDto Author { get; set; } = new UserSummaryDto();
    }
}
