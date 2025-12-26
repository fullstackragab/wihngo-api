namespace Wihngo.Dtos
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// DTO for creating a kind word on a bird's story
    /// </summary>
    public class KindWordCreateDto
    {
        /// <summary>
        /// The kind word text. Max 200 characters.
        /// No URLs, no emojis-only, no mentions.
        /// </summary>
        [Required(ErrorMessage = "Please write something kind")]
        [MaxLength(200, ErrorMessage = "Please keep messages kind and under 200 characters")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for displaying a kind word
    /// </summary>
    public class KindWordDto
    {
        public Guid Id { get; set; }
        public Guid BirdId { get; set; }
        public Guid AuthorUserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorProfileImage { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for kind words section on a bird story page
    /// </summary>
    public class KindWordsSectionDto
    {
        /// <summary>
        /// Whether kind words are enabled for this bird
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Whether the current user can post kind words
        /// (must have supported or follow the bird)
        /// </summary>
        public bool CanPost { get; set; }

        /// <summary>
        /// Reason why user cannot post (if applicable)
        /// </summary>
        public string? CannotPostReason { get; set; }

        /// <summary>
        /// The bird's name (for empty state messaging)
        /// </summary>
        public string BirdName { get; set; } = string.Empty;

        /// <summary>
        /// List of kind words, newest first
        /// </summary>
        public List<KindWordDto> KindWords { get; set; } = new();

        /// <summary>
        /// Total count of kind words
        /// </summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// DTO for bird owner to toggle kind words
    /// </summary>
    public class KindWordsSettingsDto
    {
        public bool KindWordsEnabled { get; set; }
    }

    /// <summary>
    /// Response DTO for kind word operations
    /// </summary>
    public class KindWordResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public KindWordDto? KindWord { get; set; }
    }
}
