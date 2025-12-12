using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

[Table("payment_events")]
public class PaymentEvent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("payment_id")]
    public Guid? PaymentId { get; set; }

    [Required]
    [Column("event_type")]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Column("previous_state")]
    [MaxLength(50)]
    public string? PreviousState { get; set; }

    [Column("new_state")]
    [MaxLength(50)]
    public string? NewState { get; set; }

    [Column("actor")]
    [MaxLength(255)]
    public string? Actor { get; set; } // system, user_id, admin_id

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("raw_payload", TypeName = "jsonb")]
    public string? RawPayload { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
