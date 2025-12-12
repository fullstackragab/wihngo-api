using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("webhooks_received")]
public class WebhookReceived
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("provider")]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // paypal, solana, base

    [Required]
    [Column("provider_event_id")]
    [MaxLength(255)]
    public string ProviderEventId { get; set; } = string.Empty;

    [Required]
    [Column("event_type")]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("payload", TypeName = "jsonb")]
    public string Payload { get; set; } = string.Empty;

    [Column("processed")]
    public bool Processed { get; set; } = false;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
