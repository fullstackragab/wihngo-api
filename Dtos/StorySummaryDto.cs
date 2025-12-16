namespace Wihngo.Dtos
{
    using System;
    using Wihngo.Models.Enums;

    public class StorySummaryDto
    {
        public Guid StoryId { get; set; }
        public List<string> Birds { get; set; } = new List<string>();
        
        /// <summary>
        /// Optional mood/category. Null means no mood selected.
        /// </summary>
        public StoryMode? Mode { get; set; }
        
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

        /// <summary>
        /// S3 key for story video
        /// </summary>
        public string? VideoS3Key { get; set; }
        
        /// <summary>
        /// Pre-signed download URL for story video (expires in 10 minutes)
        /// </summary>
        public string? VideoUrl { get; set; }

        /// <summary>
        /// S3 key for story audio
        /// </summary>
        public string? AudioS3Key { get; set; }

        /// <summary>
        /// Pre-signed download URL for story audio (expires in 10 minutes)
        /// </summary>
        public string? AudioUrl { get; set; }
    }
}
