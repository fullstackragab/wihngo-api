using Dapper;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

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
public class CaretakerEligibilityService : ICaretakerEligibilityService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IWalletService _walletService;
    private readonly ISolanaTransactionService _solanaService;
    private readonly ILogger<CaretakerEligibilityService> _logger;

    private const decimal DefaultWeeklyCap = 5.00m;

    public CaretakerEligibilityService(
        IDbConnectionFactory dbFactory,
        IWalletService walletService,
        ISolanaTransactionService solanaService,
        ILogger<CaretakerEligibilityService> logger)
    {
        _dbFactory = dbFactory;
        _walletService = walletService;
        _solanaService = solanaService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CaretakerEligibilityResponse> GetEligibilityAsync(Guid caretakerUserId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Get user info
        var user = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT user_id, name, weekly_cap FROM users WHERE user_id = @UserId",
            new { UserId = caretakerUserId });

        if (user == null)
        {
            return new CaretakerEligibilityResponse
            {
                UserId = caretakerUserId,
                WeeklyCap = 0,
                ReceivedThisWeek = 0,
                Remaining = 0,
                WeekId = CaretakerSupportReceipt.GetCurrentWeekId(),
                CanReceiveBaseline = false,
                HasWallet = false,
                ErrorCode = CaretakerEligibilityErrorCodes.UserNotFound
            };
        }

        // Get wallet
        var wallet = await _walletService.GetPrimaryWalletAsync(caretakerUserId);
        var hasWallet = wallet != null;

        // Get weekly cap (use default if not set)
        decimal weeklyCap = user.weekly_cap ?? DefaultWeeklyCap;

        // Get current week
        string currentWeekId = CaretakerSupportReceipt.GetCurrentWeekId();

        // Calculate received this week (BASELINE ONLY)
        decimal receivedThisWeek = await GetBaselineReceivedForWeekAsync(caretakerUserId, currentWeekId);

        // Calculate remaining
        decimal remaining = Math.Max(0, weeklyCap - receivedThisWeek);

        return new CaretakerEligibilityResponse
        {
            UserId = caretakerUserId,
            WeeklyCap = weeklyCap,
            ReceivedThisWeek = receivedThisWeek,
            Remaining = remaining,
            WeekId = currentWeekId,
            CanReceiveBaseline = remaining > 0 && hasWallet,
            HasWallet = hasWallet,
            WalletAddress = wallet?.PublicKey,
            CaretakerName = user.name
        };
    }

    /// <inheritdoc />
    public async Task<RecordSupportResponse> RecordSupportTransactionAsync(
        Guid supporterUserId,
        RecordSupportRequest request)
    {
        // Prevent self-support
        if (supporterUserId == request.ToUserId)
        {
            return new RecordSupportResponse
            {
                Success = false,
                ErrorCode = CaretakerEligibilityErrorCodes.CannotSupportSelf,
                ErrorMessage = "You cannot send support to yourself"
            };
        }

        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Check for duplicate transaction (idempotency by tx signature)
        var existingReceipt = await conn.QueryFirstOrDefaultAsync<CaretakerSupportReceipt>(
            @"SELECT * FROM caretaker_support_receipts WHERE tx_signature = @TxSignature",
            new { request.TxSignature });

        if (existingReceipt != null)
        {
            _logger.LogInformation(
                "Duplicate transaction detected: {TxSignature}, returning existing receipt {ReceiptId}",
                request.TxSignature, existingReceipt.Id);

            var existingEligibility = await GetEligibilityAsync(request.ToUserId);
            return new RecordSupportResponse
            {
                Success = true,
                ReceiptId = existingReceipt.Id,
                TransactionType = existingReceipt.TransactionType,
                VerifiedOnChain = existingReceipt.VerifiedOnChain,
                UpdatedEligibility = existingEligibility,
                WasAlreadyRecorded = true
            };
        }

        // Get caretaker info
        var caretaker = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT user_id, weekly_cap FROM users WHERE user_id = @UserId",
            new { UserId = request.ToUserId });

        if (caretaker == null)
        {
            return new RecordSupportResponse
            {
                Success = false,
                ErrorCode = CaretakerEligibilityErrorCodes.UserNotFound,
                ErrorMessage = "Caretaker not found"
            };
        }

        // Get caretaker wallet for verification
        var caretakerWallet = await _walletService.GetPrimaryWalletAsync(request.ToUserId);
        if (caretakerWallet == null)
        {
            return new RecordSupportResponse
            {
                Success = false,
                ErrorCode = CaretakerEligibilityErrorCodes.NoWallet,
                ErrorMessage = "Caretaker has no wallet linked"
            };
        }

        // Get supporter wallet for verification
        var supporterWallet = await _walletService.GetPrimaryWalletAsync(supporterUserId);
        if (supporterWallet == null)
        {
            return new RecordSupportResponse
            {
                Success = false,
                ErrorCode = CaretakerEligibilityErrorCodes.NoWallet,
                ErrorMessage = "Supporter has no wallet linked"
            };
        }

        // Verify transaction on Solana
        bool verifiedOnChain = false;
        try
        {
            var verification = await _solanaService.VerifyTransactionAsync(
                request.TxSignature,
                supporterWallet.PublicKey,
                caretakerWallet.PublicKey,
                request.Amount);

            if (!verification.Success)
            {
                _logger.LogWarning(
                    "Transaction verification failed for {TxSignature}: {Error}",
                    request.TxSignature, verification.Error);

                // Allow recording without verification for now (to handle delayed finalization)
                // The transaction will be marked as unverified
                verifiedOnChain = false;
            }
            else
            {
                verifiedOnChain = true;

                // Check amount mismatch
                if (!verification.AmountMatches)
                {
                    _logger.LogWarning(
                        "Amount mismatch for {TxSignature}: expected {Expected}, actual {Actual}",
                        request.TxSignature, request.Amount, verification.ActualAmount);

                    return new RecordSupportResponse
                    {
                        Success = false,
                        ErrorCode = CaretakerEligibilityErrorCodes.AmountMismatch,
                        ErrorMessage = $"Amount mismatch: expected {request.Amount} USDC, actual {verification.ActualAmount} USDC"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify transaction {TxSignature}", request.TxSignature);
            // Continue without verification - will be marked as unverified
        }

        // Determine transaction type
        string transactionType;
        if (!string.IsNullOrEmpty(request.Type) &&
            (request.Type == CaretakerSupportTransactionType.Baseline || request.Type == CaretakerSupportTransactionType.Gift))
        {
            // Use explicit type from request
            transactionType = request.Type;

            // If explicitly requesting baseline but no remaining capacity, reject
            if (request.Type == CaretakerSupportTransactionType.Baseline)
            {
                var eligibility = await GetEligibilityAsync(request.ToUserId);
                if (eligibility.Remaining < request.Amount)
                {
                    return new RecordSupportResponse
                    {
                        Success = false,
                        ErrorCode = CaretakerEligibilityErrorCodes.WeeklyCapReached,
                        ErrorMessage = $"Weekly baseline cap reached. Remaining: {eligibility.Remaining} USDC. Use type=gift to bypass cap.",
                        UpdatedEligibility = eligibility
                    };
                }
            }
        }
        else
        {
            // Auto-classify based on remaining allowance
            transactionType = await ClassifyTransactionTypeAsync(request.ToUserId, request.Amount);
        }

        // Get current week
        string currentWeekId = CaretakerSupportReceipt.GetCurrentWeekId();

        // Create receipt
        var receipt = new CaretakerSupportReceipt
        {
            Id = Guid.NewGuid(),
            CaretakerUserId = request.ToUserId,
            SupporterUserId = supporterUserId,
            BirdId = request.BirdId,
            TxSignature = request.TxSignature,
            Amount = request.Amount,
            TransactionType = transactionType,
            WeekId = currentWeekId,
            SupportIntentId = null, // Could be linked if coming from support intent flow
            VerifiedOnChain = verifiedOnChain,
            CreatedAt = DateTime.UtcNow
        };

        // Insert receipt
        await conn.ExecuteAsync(
            @"INSERT INTO caretaker_support_receipts
              (id, caretaker_user_id, supporter_user_id, bird_id, tx_signature, amount,
               transaction_type, week_id, support_intent_id, verified_on_chain, created_at)
              VALUES (@Id, @CaretakerUserId, @SupporterUserId, @BirdId, @TxSignature, @Amount,
               @TransactionType, @WeekId, @SupportIntentId, @VerifiedOnChain, @CreatedAt)",
            receipt);

        _logger.LogInformation(
            "Support receipt recorded: {ReceiptId}, Type: {Type}, Amount: {Amount} USDC, Caretaker: {CaretakerId}",
            receipt.Id, transactionType, request.Amount, request.ToUserId);

        // Get updated eligibility
        var updatedEligibility = await GetEligibilityAsync(request.ToUserId);

        return new RecordSupportResponse
        {
            Success = true,
            ReceiptId = receipt.Id,
            TransactionType = transactionType,
            VerifiedOnChain = verifiedOnChain,
            UpdatedEligibility = updatedEligibility
        };
    }

    /// <inheritdoc />
    public async Task<decimal> GetBaselineReceivedForWeekAsync(Guid caretakerUserId, string weekId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var result = await conn.ExecuteScalarAsync<decimal?>(
            @"SELECT COALESCE(SUM(amount), 0)
              FROM caretaker_support_receipts
              WHERE caretaker_user_id = @CaretakerId
                AND week_id = @WeekId
                AND transaction_type = @BaselineType",
            new
            {
                CaretakerId = caretakerUserId,
                WeekId = weekId,
                BaselineType = CaretakerSupportTransactionType.Baseline
            });

        return result ?? 0m;
    }

    /// <inheritdoc />
    public async Task<string> ClassifyTransactionTypeAsync(Guid caretakerUserId, decimal amount)
    {
        var eligibility = await GetEligibilityAsync(caretakerUserId);

        // If there's remaining baseline allowance, classify as baseline
        if (eligibility.Remaining >= amount)
        {
            return CaretakerSupportTransactionType.Baseline;
        }

        // Otherwise, classify as gift (does not count toward cap)
        return CaretakerSupportTransactionType.Gift;
    }

    /// <inheritdoc />
    public async Task<List<CaretakerSupportReceipt>> GetSupportReceiptsAsync(
        Guid caretakerUserId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? transactionType = null)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var sql = @"SELECT * FROM caretaker_support_receipts
                    WHERE caretaker_user_id = @CaretakerId";

        var parameters = new DynamicParameters();
        parameters.Add("CaretakerId", caretakerUserId);

        if (startDate.HasValue)
        {
            sql += " AND created_at >= @StartDate";
            parameters.Add("StartDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            sql += " AND created_at <= @EndDate";
            parameters.Add("EndDate", endDate.Value);
        }

        if (!string.IsNullOrEmpty(transactionType))
        {
            sql += " AND transaction_type = @TransactionType";
            parameters.Add("TransactionType", transactionType);
        }

        sql += " ORDER BY created_at DESC";

        var receipts = await conn.QueryAsync<CaretakerSupportReceipt>(sql, parameters);
        return receipts.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> CanAddMoreBirdsAsync(Guid userId, int maxBirds = 10)
    {
        var birdCount = await GetBirdCountAsync(userId);
        return birdCount < maxBirds;
    }

    /// <inheritdoc />
    public async Task<int> GetBirdCountAsync(Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var count = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM birds WHERE owner_id = @UserId AND status != 'archived'",
            new { UserId = userId });

        return count;
    }
}
