using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing gas sponsorship for P2P payments
/// </summary>
public interface IGasSponsorshipService
{
    /// <summary>
    /// Determines if gas sponsorship is needed for a sender
    /// </summary>
    /// <param name="senderPubkey">Sender's wallet public key</param>
    /// <returns>True if sponsorship is needed</returns>
    Task<bool> ShouldSponsorGasAsync(string senderPubkey);

    /// <summary>
    /// Gets the flat fee charged for gas sponsorship
    /// </summary>
    decimal GetSponsorshipFeeUsdc();

    /// <summary>
    /// Gets the minimum SOL threshold below which sponsorship is triggered
    /// </summary>
    decimal GetMinSolThreshold();

    /// <summary>
    /// Gets the platform sponsor wallet public key
    /// </summary>
    Task<string?> GetSponsorWalletPubkeyAsync();

    /// <summary>
    /// Gets the platform sponsor wallet with private key for signing
    /// Used internally when submitting sponsored transactions
    /// </summary>
    Task<PlatformHotWallet?> GetSponsorWalletAsync();

    /// <summary>
    /// Records a gas sponsorship event for a payment
    /// </summary>
    Task<GasSponsorship> RecordSponsorshipAsync(
        Guid paymentId,
        decimal sponsoredSolAmount,
        string sponsorWalletPubkey,
        bool ataCreated = false,
        string? ataAddress = null);

    /// <summary>
    /// Gets sponsorship record for a payment
    /// </summary>
    Task<GasSponsorship?> GetSponsorshipAsync(Guid paymentId);
}
