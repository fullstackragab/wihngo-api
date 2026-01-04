namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for deriving Solana addresses from an HD master seed.
/// Uses SLIP-0010 compatible derivation for ED25519.
/// </summary>
public interface ISolanaHdWalletService
{
    /// <summary>
    /// Derive a Solana public key (address) from the master seed at the given index.
    /// </summary>
    string DeriveAddress(long index);

    /// <summary>
    /// Derive a full keypair (for signing transactions) at the given index.
    /// </summary>
    SolanaKeypair DeriveKeypair(long index);

    /// <summary>
    /// Get the next available derivation index (atomic).
    /// </summary>
    Task<long> GetNextDerivationIndexAsync(CancellationToken ct = default);

    /// <summary>
    /// Whether the HD wallet is configured and ready for use.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Solana ED25519 keypair.
/// </summary>
public sealed record SolanaKeypair(
    /// <summary>Base58-encoded public key (Solana address).</summary>
    string PublicKey,
    /// <summary>32-byte private key seed for signing.</summary>
    byte[] PrivateKeySeed
);
