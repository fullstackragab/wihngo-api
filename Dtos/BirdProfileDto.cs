namespace Wihngo.Dtos
{
    using System;
    using System.Collections.Generic;
    using Wihngo.Models.Enums;

    public class BirdProfileDto
    {
        public Guid BirdId { get; set; }
        public string CommonName { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// S3 key for bird profile image
        /// </summary>
        public string? ImageS3Key { get; set; }

        /// <summary>
        /// Pre-signed download URL for bird profile image (expires in 10 minutes)
        /// </summary>
        public string? ImageUrl { get; set; }

        public List<string> Personality { get; set; } = new();
        public ConservationDto Conservation { get; set; } = new();
        public List<string> FunFacts { get; set; } = new();
        public int LovedBy { get; set; }
        public int SupportedBy { get; set; }
        public UserSummaryDto? Owner { get; set; }
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
        /// Message explaining why support is unavailable (null if support is available)
        /// </summary>
        public string? SupportUnavailableMessage { get; set; }

        /// <summary>
        /// Whether the bird is in memorial status
        /// </summary>
        public bool IsMemorial { get; set; }
    }

    public class ConservationDto
    {
        public string Status { get; set; } = string.Empty;
        public string Needs { get; set; } = string.Empty;
    }
}
