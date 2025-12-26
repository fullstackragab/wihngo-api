using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Models.Entities;

/// <summary>
/// Platform wallets used for gas sponsorship and fee collection
/// </summary>
[Table("platform_hot_wallets")]
public class PlatformHotWallet
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Purpose of the wallet: gas_sponsor, fee_collection
    /// </summary>
    [Required]
    [MaxLength(30)]
    [Column("wallet_type")]
    public string WalletType { get; set; } = string.Empty;

    /// <summary>
    /// Solana public key (base58 encoded)
    /// </summary>
    [Required]
    [MaxLength(44)]
    [Column("public_key")]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted private key (DO NOT LOG OR EXPOSE)
    /// </summary>
    [Required]
    [Column("private_key_encrypted")]
    public string PrivateKeyEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Whether this wallet is currently active
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Platform wallet type constants
/// </summary>
public static class PlatformWalletType
{
    public const string GasSponsor = "gas_sponsor";
    public const string FeeCollection = "fee_collection";
}
