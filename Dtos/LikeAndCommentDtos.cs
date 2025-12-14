namespace Wihngo.Dtos
{
    using System;
    using System.ComponentModel.DataAnnotations;

    // Story Like DTOs
    public class StoryLikeCreateDto
    {
        [Required]
        public Guid StoryId { get; set; }
    }

    public class StoryLikeDto
    {
        public Guid LikeId { get; set; }
        public Guid StoryId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserProfileImage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Comment DTOs
    public class CommentCreateDto
    {
        [Required]
        public Guid StoryId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Parent comment ID for nested replies (null for top-level comments)
        /// </summary>
        public Guid? ParentCommentId { get; set; }
    }

    public class CommentUpdateDto
    {
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    public class CommentDto
    {
        public Guid CommentId { get; set; }
        public Guid StoryId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserProfileImage { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? ParentCommentId { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public int ReplyCount { get; set; }
    }

    public class CommentWithRepliesDto
    {
        public Guid CommentId { get; set; }
        public Guid StoryId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserProfileImage { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? ParentCommentId { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<CommentDto> Replies { get; set; } = new();
    }

    // Comment Like DTOs
    public class CommentLikeCreateDto
    {
        [Required]
        public Guid CommentId { get; set; }
    }

    public class CommentLikeDto
    {
        public Guid LikeId { get; set; }
        public Guid CommentId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserProfileImage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
