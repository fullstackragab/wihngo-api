using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing caretaker weekly support caps and eligibility.
///
/// Key invariants:
/// - Birds never multiply money
/// - One user = one wallet = capped baseline support
/// - Baseline support counts toward weekly cap
/// - Gifts are unlimited and do NOT count toward weekly cap
/// - Backend is the authority on eligibility
/// </summary>
public interface ICaretakerEligibilityService
{
    /// <summary>
    /// Gets the eligibility status for a caretaker (how much they can receive this week).
    /// </summary>
    /// <param name="caretakerUserId">The caretaker's user ID</param>
    /// <returns>Eligibility info including weekly cap, received this week, and remaining</returns>
    Task<CaretakerEligibilityResponse> GetEligibilityAsync(Guid caretakerUserId);

    /// <summary>
    /// Records a support transaction after it's been verified on Solana.
    /// Auto-classifies as "baseline" or "gift" based on remaining allowance.
    /// </summary>
    /// <param name="supporterUserId">The supporter who sent the transaction</param>
    /// <param name="request">Transaction details</param>
    /// <returns>Record result with updated eligibility</returns>
    Task<RecordSupportResponse> RecordSupportTransactionAsync(Guid supporterUserId, RecordSupportRequest request);

    /// <summary>
    /// Gets baseline support received by a caretaker for a specific week.
    /// </summary>
    /// <param name="caretakerUserId">The caretaker's user ID</param>
    /// <param name="weekId">ISO week (YYYY-WW)</param>
    /// <returns>Total baseline USDC received</returns>
    Task<decimal> GetBaselineReceivedForWeekAsync(Guid caretakerUserId, string weekId);

    /// <summary>
    /// Classifies a transaction as "baseline" or "gift" based on remaining allowance.
    /// </summary>
    /// <param name="caretakerUserId">The caretaker's user ID</param>
    /// <param name="amount">Transaction amount</param>
    /// <returns>"baseline" if caretaker has remaining allowance, "gift" otherwise</returns>
    Task<string> ClassifyTransactionTypeAsync(Guid caretakerUserId, decimal amount);

    /// <summary>
    /// Gets all support receipts for a caretaker within a date range.
    /// </summary>
    Task<List<CaretakerSupportReceipt>> GetSupportReceiptsAsync(
        Guid caretakerUserId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? transactionType = null);

    /// <summary>
    /// Validates that a user can add more birds (anti-abuse).
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="maxBirds">Maximum allowed birds (default 10)</param>
    /// <returns>True if user can add more birds</returns>
    Task<bool> CanAddMoreBirdsAsync(Guid userId, int maxBirds = 10);

    /// <summary>
    /// Gets the bird count for a user.
    /// </summary>
    Task<int> GetBirdCountAsync(Guid userId);
}
