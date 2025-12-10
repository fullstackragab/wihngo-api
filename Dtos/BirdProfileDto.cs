namespace Wihngo.Dtos
{
    using System;
    using System.Collections.Generic;

    public class BirdProfileDto
    {
        public string CommonName { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string> Personality { get; set; } = new();
        public ConservationDto Conservation { get; set; } = new();
        public List<string> FunFacts { get; set; } = new();
        public int LovedBy { get; set; }
        public int SupportedBy { get; set; }
        public UserSummaryDto? Owner { get; set; }
        public bool IsLoved { get; set; }
    }

    public class ConservationDto
    {
        public string Status { get; set; } = string.Empty;
        public string Needs { get; set; } = string.Empty;
    }
}
