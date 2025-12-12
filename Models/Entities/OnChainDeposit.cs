using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Tracks on-chain deposits detected from blockchain networks
/// Supports USDC and EURC across Ethereum, Polygon, Base, Solana, and Stellar
/// </summary>
[Table("onchain_deposits")]
public class OnChainDeposit
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Chain identifier: 'ethereum', 'polygon', 'base', 'solana', 'stellar'
    /// </summary>
    [Required]
    [Column("chain")]
    [MaxLength(20)]
    public string Chain { get; set; } = string.Empty;

    /// <summary>
    /// Token symbol: 'USDC' or 'EURC'
    /// </summary>
    [Required]
    [Column("token")]
    [MaxLength(10)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token contract address (EVM), mint address (Solana), or asset issuer (Stellar)
    /// </summary>
    [Required]
    [Column("token_id")]
    [MaxLength(255)]
    public string TokenId { get; set; } = string.Empty;

    /// <summary>
    /// The deposit address we expected (user's derived address or shared deposit account)
    /// </summary>
    [Required]
    [Column("address_or_account")]
    [MaxLength(255)]
    public string AddressOrAccount { get; set; } = string.Empty;

    /// <summary>
    /// Transaction hash (EVM/Solana) or transaction ID (Stellar)
    /// </summary>
    [Required]
    [Column("tx_hash_or_sig")]
    [MaxLength(255)]
    public string TxHashOrSig { get; set; } = string.Empty;

    /// <summary>
    /// Block number (EVM), slot (Solana), or ledger (Stellar)
    /// </summary>
    [Column("block_number_or_slot")]
    public long? BlockNumberOrSlot { get; set; }

    /// <summary>
    /// Log index (EVM) or operation index (Stellar)
    /// </summary>
    [Column("op_index_or_log_index")]
    public int? OpIndexOrLogIndex { get; set; }

    /// <summary>
    /// Sender address
    /// </summary>
    [Required]
    [Column("from_address")]
    [MaxLength(255)]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Recipient address (should match AddressOrAccount)
    /// </summary>
    [Required]
    [Column("to_address")]
    [MaxLength(255)]
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>
    /// Raw amount in smallest unit (e.g., 1000000 for 1 USDC with 6 decimals)
    /// </summary>
    [Required]
    [Column("raw_amount")]
    [MaxLength(100)]
    public string RawAmount { get; set; } = string.Empty;

    /// <summary>
    /// Token decimals (typically 6 for USDC/EURC)
    /// </summary>
    [Required]
    [Column("decimals")]
    public int Decimals { get; set; } = 6;

    /// <summary>
    /// Human-readable amount (raw_amount / 10^decimals)
    /// </summary>
    [Required]
    [Column("amount_decimal", TypeName = "decimal(20,10)")]
    public decimal AmountDecimal { get; set; }

    /// <summary>
    /// Deposit status: 'pending', 'confirmed', 'failed', 'credited'
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Number of confirmations or commitment level
    /// </summary>
    [Column("confirmations")]
    public int Confirmations { get; set; } = 0;

    /// <summary>
    /// When the deposit was first detected
    /// </summary>
    [Required]
    [Column("detected_at")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the deposit was confirmed and credited to user
    /// </summary>
    [Column("credited_at")]
    public DateTime? CreditedAt { get; set; }

    /// <summary>
    /// Optional memo for Stellar deposits
    /// </summary>
    [Column("memo")]
    [MaxLength(100)]
    public string? Memo { get; set; }

    /// <summary>
    /// Additional metadata (JSON format)
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
