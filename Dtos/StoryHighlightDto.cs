using System;

namespace Wihngo.Dtos
{
    public class StoryHighlightDto
    {
        public Guid StoryId { get; set; }
        public bool IsHighlighted { get; set; }
        public int? HighlightOrder { get; set; }

        // If true, pin this highlight to profile top (used by UI)
        public bool PinToProfile { get; set; } = false;
    }
}
