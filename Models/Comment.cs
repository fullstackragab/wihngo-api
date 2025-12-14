namespace Wihngo.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Comment
    {
        [Key]
        public Guid CommentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StoryId { get; set; }

        [ForeignKey(nameof(StoryId))]
        public Story? Story { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Parent comment ID for nested replies (null for top-level comments)
        /// </summary>
        public Guid? ParentCommentId { get; set; }

        [ForeignKey(nameof(ParentCommentId))]
        public Comment? ParentComment { get; set; }

        // Navigation properties
        public List<Comment> Replies { get; set; } = new();
        
        public List<CommentLike> Likes { get; set; } = new();

        // Like count for efficient querying
        public int LikeCount { get; set; } = 0;
    }
}
