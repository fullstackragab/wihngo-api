namespace Wihngo.Configuration;

/// <summary>
/// Extended Solana configuration for the ulomira-style payment system.
/// </summary>
public class SolanaConfiguration
{
    public const string SectionName = "Solana";

    /// <summary>
    /// Solana RPC endpoint URL
    /// </summary>
    public string RpcUrl { get; set; } = "https://api.mainnet-beta.solana.com";

    /// <summary>
    /// Solana WebSocket endpoint URL
    /// </summary>
    public string WsUrl { get; set; } = "wss://api.mainnet-beta.solana.com";

    /// <summary>
    /// Commitment level for queries
    /// </summary>
    public string Commitment { get; set; } = "finalized";

    /// <summary>
    /// USDC SPL Token mint address on Solana
    /// </summary>
    public string UsdcMintAddress { get; set; } = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";

    /// <summary>
    /// Platform's Solana wallet address for receiving payments
    /// </summary>
    public string PlatformWalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// HD seed for deriving manual payment addresses (64 hex characters = 32 bytes)
    /// </summary>
    public string? HdSeed { get; set; }

    /// <summary>
    /// Minutes until a payment intent expires
    /// </summary>
    public int PaymentIntentExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Poll interval in seconds for manual payment monitoring
    /// </summary>
    public int ManualPaymentPollIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Whether manual payments are enabled
    /// </summary>
    public bool ManualPaymentsEnabled { get; set; } = false;
}

/// <summary>
/// Platform configuration for frontend URLs and general settings
/// </summary>
public class PlatformConfiguration
{
    public const string SectionName = "Platform";

    /// <summary>
    /// Frontend URL for generating claim links and return URLs
    /// </summary>
    public string FrontendUrl { get; set; } = "https://wihngo.com";
}
