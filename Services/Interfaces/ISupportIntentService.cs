using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing support (donation) intents for birds.
///
/// Bird-First Payment Model:
/// - 100% of bird amount goes to bird owner (NEVER deducted)
/// - Wihngo support is OPTIONAL and ADDITIVE (not a percentage)
/// - Minimum Wihngo support: $0.05 (if > 0)
/// - Two separate on-chain USDC transfers
///
/// MVP Architecture (Non-Custodial):
/// - Users must connect Phantom wallet to support a bird
/// - USDC transfers from user wallet → recipient wallet(s)
/// - All transactions are on Solana mainnet
/// </summary>
public interface ISupportIntentService
{
    /// <summary>
    /// Preflight check before creating a support intent.
    /// Returns wallet status, balances, and validates the support can proceed.
    /// Frontend should call this before showing payment UI.
    /// </summary>
    Task<SupportPreflightResponse> PreflightAsync(
        Guid userId,
        SupportPreflightRequest request);

    /// <summary>
    /// Alias for PreflightAsync - checks if user can support a bird.
    /// </summary>
    Task<CheckSupportBalanceResponse> CheckSupportBalanceAsync(
        Guid userId,
        CheckSupportBalanceRequest request);

    /// <summary>
    /// Creates a support intent for a bird.
    /// Requires: connected wallet with sufficient USDC + SOL for gas.
    /// Returns: unsigned Solana transaction(s) for user to sign.
    ///
    /// Bird-first model:
    /// - birdAmount → 100% to bird owner wallet
    /// - wihngoSupportAmount → optional, to Wihngo treasury (if > 0)
    /// </summary>
    Task<(SupportIntentResponse? Response, ValidationErrorResponse? Error)> CreateSupportIntentAsync(
        Guid supporterUserId,
        CreateSupportIntentRequest request);

    /// <summary>
    /// Creates an intent to support Wihngo independently (no bird involved).
    /// </summary>
    Task<(SupportWihngoResponse? Response, ValidationErrorResponse? Error)> CreateWihngoSupportIntentAsync(
        Guid userId,
        SupportWihngoRequest request);

    /// <summary>
    /// Submits a signed transaction for a support intent.
    /// Called after user signs the transaction in Phantom wallet.
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
    /// Gets user's support history (as supporter)
    /// </summary>
    Task<PagedResult<SupportIntentResponse>> GetUserSupportHistoryAsync(Guid userId, int page = 1, int pageSize = 20);
}
