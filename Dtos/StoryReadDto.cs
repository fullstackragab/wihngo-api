namespace Wihngo.Dtos
{
    using System;

    public class StoryReadDto
    {
        public Guid StoryId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public BirdSummaryDto Bird { get; set; } = new BirdSummaryDto();
        public UserSummaryDto Author { get; set; } = new UserSummaryDto();
    }
}
