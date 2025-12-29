using System;
using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos
{
    // Request DTO for marking bird as memorial
    public class MemorialRequestDto
    {
        public DateTime? MemorialDate { get; set; }

        [MaxLength(500)]
        public string? MemorialReason { get; set; }
    }

    // Response DTO for memorial bird details
    public class MemorialBirdDto
    {
        public Guid BirdId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Species { get; set; }
        public bool IsMemorial { get; set; }
        public DateTime? MemorialDate { get; set; }
        public string? MemorialReason { get; set; }
        public string? ImageUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public MemorialStatsDto Stats { get; set; } = new();
        public string? OwnerMessage { get; set; }
        public int MessagesCount { get; set; }
    }

    public class MemorialStatsDto
    {
        public int LovedBy { get; set; }
        public int SupportedBy { get; set; }
        public decimal TotalSupportReceived { get; set; }
    }

    // DTO for memorial message
    public class MemorialMessageDto
    {
        public Guid MessageId { get; set; }
        public Guid BirdId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Request DTO for creating memorial message
    public class CreateMemorialMessageDto
    {
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
    }

    // Paginated response for memorial messages
    public class MemorialMessagePageDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<MemorialMessageDto> Messages { get; set; } = new();
    }

    // Response DTO when marking bird as memorial
    public class MarkMemorialResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public MemorialBirdDto? Bird { get; set; }
    }
}
