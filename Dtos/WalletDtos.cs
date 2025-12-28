using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

// =============================================
// LINK WALLET
// =============================================

/// <summary>
/// Request to link a Phantom wallet
/// </summary>
public class LinkWalletRequest
{
    /// <summary>
    /// Solana public key (base58 encoded, 44 characters)
    /// </summary>
    [Required]
    [MaxLength(44)]
    [MinLength(32)]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Signature proving wallet ownership
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Optional: Message that was signed
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Wallet provider (default: phantom)
    /// </summary>
    public string WalletProvider { get; set; } = "phantom";
}

/// <summary>
/// Response from linking a wallet
/// </summary>
public class LinkWalletResponse
{
    public Guid WalletId { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public string WalletProvider { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}

// =============================================
// WALLET INFO
// =============================================

/// <summary>
/// Wallet information response
/// </summary>
public class WalletResponse
{
    public Guid Id { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public string WalletProvider { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// List of user's linked wallets
/// </summary>
public class UserWalletsResponse
{
    public List<WalletResponse> Wallets { get; set; } = new();
    public int Count { get; set; }
}

// =============================================
// ON-CHAIN BALANCE (PUBLIC)
// =============================================

/// <summary>
/// On-chain balance response for any wallet address
/// </summary>
public class OnChainBalanceResponse
{
    public string WalletAddress { get; set; } = string.Empty;
    public decimal SolBalance { get; set; }
    public decimal UsdcBalance { get; set; }
    public decimal MinimumSolRequired { get; set; }
}
