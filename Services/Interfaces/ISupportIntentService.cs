using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing support (donation) intents for birds.
///
/// MVP Architecture (Non-Custodial):
/// - Users must connect wallet to support a bird
/// - USDC transfers from user wallet → recipient wallet
/// - Platform may sponsor gas fees (SOL) for small fee in USDC
/// - All transactions are on-chain (Solana)
/// </summary>
public interface ISupportIntentService
{
    /// <summary>
    /// Check if user can support a bird with given amount.
    /// Returns wallet status, balances, and any issues.
    /// Frontend should call this before showing payment UI.
    /// </summary>
    Task<CheckSupportBalanceResponse> CheckSupportBalanceAsync(
        Guid userId,
        CheckSupportBalanceRequest request);

    /// <summary>
    /// Creates a support intent for a bird.
    /// Requires: connected wallet with sufficient USDC + SOL.
    /// Returns: unsigned Solana transaction for user to sign.
    /// </summary>
    Task<(SupportIntentResponse? Response, ValidationErrorResponse? Error)> CreateSupportIntentAsync(
        Guid supporterUserId,
        CreateSupportIntentRequest request);

    /// <summary>
    /// Submits a signed transaction for a support intent.
    /// Called after user signs the transaction in their wallet.
    /// </summary>
    Task<(SubmitTransactionResponse? Response, ValidationErrorResponse? Error)> SubmitSignedTransactionAsync(
        Guid userId,
        Guid intentId,
        string signedTransaction);

    /// <summary>
    /// Called by background job when transaction is confirmed on-chain.
    /// Records ledger entries and updates bird support count.
    /// </summary>
    Task<bool> ConfirmTransactionAsync(Guid intentId, int confirmations);

    /// <summary>
    /// Manual confirmation (admin use only in MVP).
    /// </summary>
    Task<(SupportIntentResponse? Response, ValidationErrorResponse? Error)> ConfirmSupportIntentAsync(
        Guid intentId,
        string? externalReference = null);

    /// <summary>
    /// Gets a support intent by ID
    /// </summary>
    Task<SupportIntent?> GetSupportIntentAsync(Guid intentId, Guid userId);

    /// <summary>
    /// Gets support intent response with all details
    /// </summary>
    Task<SupportIntentResponse?> GetSupportIntentResponseAsync(Guid intentId, Guid userId);

    /// <summary>
    /// Cancels a pending support intent
    /// </summary>
    Task<bool> CancelSupportIntentAsync(Guid intentId, Guid userId);

    /// <summary>
    /// Gets user's support history
    /// </summary>
    Task<PagedResult<SupportIntentResponse>> GetUserSupportHistoryAsync(Guid userId, int page = 1, int pageSize = 20);
}
