using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Core service for P2P USDC payments on Solana
/// </summary>
public interface IP2PPaymentService
{
    /// <summary>
    /// Validates a payment before creating an intent
    /// </summary>
    Task<PreflightPaymentResponse> PreflightPaymentAsync(Guid senderUserId, PreflightPaymentRequest request);

    /// <summary>
    /// Creates a payment intent and builds the unsigned transaction
    /// </summary>
    Task<PaymentIntentResponse> CreatePaymentIntentAsync(Guid senderUserId, CreatePaymentIntentRequest request);

    /// <summary>
    /// Gets a payment intent by ID
    /// </summary>
    Task<PaymentStatusResponse?> GetPaymentIntentAsync(Guid paymentId, Guid userId);

    /// <summary>
    /// Submits a signed transaction to Solana
    /// </summary>
    Task<SubmitTransactionResponse> SubmitSignedTransactionAsync(Guid userId, SubmitTransactionRequest request);

    /// <summary>
    /// Cancels a pending payment intent
    /// </summary>
    Task<bool> CancelPaymentAsync(Guid paymentId, Guid userId);

    /// <summary>
    /// Gets paginated list of user's payments (sent and received)
    /// </summary>
    Task<PagedResult<PaymentSummary>> GetUserPaymentsAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Checks and updates payment confirmation status
    /// Called by background job
    /// </summary>
    Task<bool> CheckPaymentConfirmationAsync(Guid paymentId);

    /// <summary>
    /// Gets all pending payments that need confirmation checking
    /// </summary>
    Task<List<P2PPayment>> GetPendingConfirmationsAsync();

    /// <summary>
    /// Handles a payment that has timed out (submitted but never confirmed)
    /// Checks if transaction exists on-chain, and either waits longer or marks as timeout
    /// </summary>
    Task HandlePaymentTimeoutAsync(Guid paymentId);
}
