namespace Wihngo.Dtos
{
    using System;
    using System.Collections.Generic;

    public class UserProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string JoinedDate { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public ProfileStats Stats { get; set; } = new();
        public List<FavoriteBirdDto> FavoriteBirds { get; set; } = new();
        public List<StorySummaryDto> RecentStories { get; set; } = new();
    }

    public class ProfileStats
    {
        public int BirdsLoved { get; set; }
        public int StoriesShared { get; set; }
        public int Supported { get; set; }
    }

    public class FavoriteBirdDto
    {
        public string Name { get; set; } = string.Empty;
        public string Emoji { get; set; } = "??";
        public bool Loved { get; set; } = true;
    }
}
