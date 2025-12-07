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
    }
}
