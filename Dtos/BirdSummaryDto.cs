namespace Wihngo.Dtos
{
    using System;
    using Wihngo.Models.Enums;

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

        // Activity status fields
        /// <summary>
        /// Activity status of the bird (Active, Quiet, Inactive, Memorial)
        /// </summary>
        public BirdActivityStatus ActivityStatus { get; set; }

        /// <summary>
        /// Human-readable last seen text (e.g., "Recently active", "Last seen: 2 months ago")
        /// </summary>
        public string LastSeenText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the support button should be shown for this bird
        /// </summary>
        public bool CanSupport { get; set; }

        /// <summary>
        /// Whether the bird is in memorial status
        /// </summary>
        public bool IsMemorial { get; set; }
    }
}
