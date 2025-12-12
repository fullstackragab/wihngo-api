using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Stores canonical token contract addresses for USDC and EURC across different chains
/// Based on Circle's official contract addresses
/// </summary>
[Table("token_configurations")]
public class TokenConfiguration
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Token symbol: 'USDC' or 'EURC'
    /// </summary>
    [Required]
    [Column("token")]
    [MaxLength(10)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Chain identifier: 'ethereum', 'polygon', 'base', 'solana', 'stellar'
    /// </summary>
    [Required]
    [Column("chain")]
    [MaxLength(20)]
    public string Chain { get; set; } = string.Empty;

    /// <summary>
    /// Token contract address (EVM), mint address (Solana), or asset code (Stellar)
    /// </summary>
    [Required]
    [Column("token_address")]
    [MaxLength(255)]
    public string TokenAddress { get; set; } = string.Empty;

    /// <summary>
    /// Asset issuer for Stellar (optional, only used for Stellar)
    /// </summary>
    [Column("issuer")]
    [MaxLength(255)]
    public string? Issuer { get; set; }

    /// <summary>
    /// Token decimals (typically 6 for USDC/EURC)
    /// </summary>
    [Required]
    [Column("decimals")]
    public int Decimals { get; set; } = 6;

    /// <summary>
    /// Required confirmations for this token/chain combination
    /// </summary>
    [Required]
    [Column("required_confirmations")]
    public int RequiredConfirmations { get; set; } = 12;

    /// <summary>
    /// Whether this token configuration is active
    /// </summary>
    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// HD derivation path for this chain
    /// e.g., "m/44'/60'/0'/0/{index}" for Ethereum
    /// </summary>
    [Column("derivation_path")]
    [MaxLength(100)]
    public string? DerivationPath { get; set; }

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
}
