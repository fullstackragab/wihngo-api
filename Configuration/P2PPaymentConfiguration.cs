namespace Wihngo.Configuration;

/// <summary>
/// Configuration for P2P USDC payments on Solana
/// </summary>
public class P2PPaymentConfiguration
{
    public const string SectionName = "P2PPayment";

    /// <summary>
    /// USDC SPL Token mint address on Solana mainnet
    /// </summary>
    public string UsdcMintAddress { get; set; } = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";

    /// <summary>
    /// Minutes until a payment intent expires
    /// </summary>
    public int PaymentExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Required confirmations for finality (32 for Solana finalized)
    /// </summary>
    public int RequiredConfirmations { get; set; } = 32;

    /// <summary>
    /// Minimum payment amount in USDC
    /// </summary>
    public decimal MinPaymentUsdc { get; set; } = 0.01m;

    /// <summary>
    /// Maximum payment amount in USDC
    /// </summary>
    public decimal MaxPaymentUsdc { get; set; } = 10000m;

    /// <summary>
    /// Gas sponsorship configuration
    /// </summary>
    public GasSponsorshipConfig GasSponsorship { get; set; } = new();
}

/// <summary>
/// Configuration for gas sponsorship
/// </summary>
public class GasSponsorshipConfig
{
    /// <summary>
    /// Whether gas sponsorship is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum SOL balance threshold - sponsor gas if user has less
    /// </summary>
    public decimal MinSolThreshold { get; set; } = 0.00001m;

    /// <summary>
    /// Flat fee in USDC charged for gas sponsorship
    /// </summary>
    public decimal FlatFeeUsdc { get; set; } = 0.01m;

    /// <summary>
    /// Platform wallet public key used for sponsoring gas
    /// </summary>
    public string SponsorWalletPubkey { get; set; } = string.Empty;
}

/// <summary>
/// Solana network configuration
/// </summary>
public class SolanaConfig
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
}
