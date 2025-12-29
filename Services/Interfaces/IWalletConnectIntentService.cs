using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing wallet connection intents.
///
/// Solves the Android browser-switch problem:
/// When Phantom redirects back after signing, Android may open a different browser,
/// losing the user's JWT session. This service provides:
///
/// 1. Intent creation with state token
/// 2. Public callback validation (no auth required)
/// 3. Signature verification
/// 4. Wallet linking with optional session recovery
///
/// Flow:
/// 1. Authenticated user creates intent → gets state + nonce
/// 2. Frontend redirects to Phantom with state
/// 3. Phantom signs nonce, redirects to callback
/// 4. Callback validates state + signature, links wallet
/// 5. Returns new session tokens if user was authenticated
/// </summary>
public interface IWalletConnectIntentService
{
    /// <summary>
    /// Creates a new wallet connection intent.
    /// Call this before redirecting to Phantom.
    /// </summary>
    /// <param name="userId">User ID (optional for anonymous flows)</param>
    /// <param name="request">Intent configuration</param>
    /// <param name="ipAddress">Client IP for audit</param>
    /// <param name="userAgent">Client user agent for audit</param>
    /// <returns>Intent with state token and nonce</returns>
    Task<WalletConnectIntentResponse> CreateIntentAsync(
        Guid? userId,
        CreateWalletConnectIntentRequest request,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Processes the callback from Phantom.
    /// This is the PUBLIC endpoint - no auth required.
    /// Validates state, verifies signature, links wallet.
    /// </summary>
    /// <param name="request">Callback data from Phantom</param>
    /// <returns>Result with wallet info and optional new tokens</returns>
    Task<WalletConnectCallbackResponse> ProcessCallbackAsync(WalletConnectCallbackRequest request);

    /// <summary>
    /// Gets the status of an intent.
    /// Can be used for polling or recovery flows.
    /// </summary>
    /// <param name="intentIdOrState">Intent ID or state token</param>
    /// <param name="userId">Optional user ID for ownership validation</param>
    /// <returns>Intent status or null if not found</returns>
    Task<WalletConnectIntentStatusResponse?> GetIntentStatusAsync(
        string intentIdOrState,
        Guid? userId = null);

    /// <summary>
    /// Gets any pending intent for a user.
    /// Useful for recovery/continuation flows.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Pending intent info</returns>
    Task<PendingWalletIntentResponse> GetPendingIntentAsync(Guid userId);

    /// <summary>
    /// Cancels a pending intent.
    /// </summary>
    /// <param name="intentIdOrState">Intent ID or state token</param>
    /// <param name="userId">User ID for ownership validation</param>
    /// <returns>True if cancelled</returns>
    Task<bool> CancelIntentAsync(string intentIdOrState, Guid userId);

    /// <summary>
    /// Gets the intent entity by state token.
    /// Internal use for validation.
    /// </summary>
    Task<WalletConnectIntent?> GetIntentByStateAsync(string state);

    /// <summary>
    /// Gets the intent entity by ID.
    /// </summary>
    Task<WalletConnectIntent?> GetIntentByIdAsync(Guid intentId);

    /// <summary>
    /// Expires old pending intents.
    /// Called by background job.
    /// </summary>
    /// <returns>Number of intents expired</returns>
    Task<int> ExpireOldIntentsAsync();

    /// <summary>
    /// Verifies a wallet signature.
    /// </summary>
    /// <param name="publicKey">Wallet public key</param>
    /// <param name="message">Original message that was signed</param>
    /// <param name="signature">Base58 encoded signature</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifySignatureAsync(string publicKey, string message, string signature);
}
