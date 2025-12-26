using Dapper;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for managing support (donation) intents for birds.
///
/// MVP Architecture (Non-Custodial):
/// - Users must connect wallet to support a bird
/// - USDC transfers from user wallet → recipient wallet
/// - Platform may sponsor gas fees (SOL) for small fee in USDC
/// - All transactions are on-chain (Solana)
/// </summary>
public class SupportIntentService : ISupportIntentService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IWalletService _walletService;
    private readonly ISolanaTransactionService _solanaService;
    private readonly IGasSponsorshipService _gasSponsorshipService;
    private readonly ILedgerService _ledgerService;
    private readonly P2PPaymentConfiguration _config;
    private readonly ILogger<SupportIntentService> _logger;

    // Minimum SOL required for transaction fees
    private const decimal MinSolForGas = 0.001m;

    public SupportIntentService(
        IDbConnectionFactory dbFactory,
        IWalletService walletService,
        ISolanaTransactionService solanaService,
        IGasSponsorshipService gasSponsorshipService,
        ILedgerService ledgerService,
        IOptions<P2PPaymentConfiguration> config,
        ILogger<SupportIntentService> logger)
    {
        _dbFactory = dbFactory;
        _walletService = walletService;
        _solanaService = solanaService;
        _gasSponsorshipService = gasSponsorshipService;
        _ledgerService = ledgerService;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Check if user can support a bird with given amount.
    /// Returns wallet status, balances, and any issues.
    /// </summary>
    public async Task<CheckSupportBalanceResponse> CheckSupportBalanceAsync(
        Guid userId,
        CheckSupportBalanceRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Validate bird
        var bird = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT bird_id, owner_id, name, image_url FROM birds WHERE bird_id = @BirdId",
            new { request.BirdId });

        if (bird == null)
        {
            return new CheckSupportBalanceResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.BirdNotFound,
                Message = "Bird not found"
            };
        }

        Guid recipientUserId = bird.owner_id;

        // Cannot support own bird
        if (recipientUserId == userId)
        {
            return new CheckSupportBalanceResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.CannotSupportOwnBird,
                Message = "You cannot support your own bird"
            };
        }

        // 2. Get recipient info
        var recipient = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE user_id = @UserId",
            new { UserId = recipientUserId });

        var recipientWallet = await _walletService.GetPrimaryWalletAsync(recipientUserId);
        if (recipientWallet == null)
        {
            return new CheckSupportBalanceResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.RecipientNoWallet,
                Message = "Bird owner has not set up a wallet to receive support yet",
                Bird = new BirdSupportInfo
                {
                    BirdId = request.BirdId,
                    Name = bird.name
                }
            };
        }

        // 3. Check user's wallet
        var userWallet = await _walletService.GetPrimaryWalletAsync(userId);
        if (userWallet == null)
        {
            return new CheckSupportBalanceResponse
            {
                CanSupport = false,
                HasWallet = false,
                ErrorCode = SupportIntentErrorCodes.WalletRequired,
                Message = "Connect your wallet to support this bird",
                Bird = new BirdSupportInfo
                {
                    BirdId = request.BirdId,
                    Name = bird.name
                },
                Recipient = new RecipientInfo
                {
                    UserId = recipientUserId,
                    Name = recipient?.Name ?? "Unknown"
                }
            };
        }

        // 4. Get balances from Solana
        decimal usdcBalance = 0;
        decimal solBalance = 0;
        try
        {
            usdcBalance = await _solanaService.GetUsdcBalanceAsync(userWallet.PublicKey);
            solBalance = await _solanaService.GetSolBalanceAsync(userWallet.PublicKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get balances for wallet {Wallet}", userWallet.PublicKey);
        }

        // 5. Calculate requirements (5% platform fee if included)
        const decimal PlatformFeePercent = 5m;
        var platformFee = request.IncludePlatformFee ? request.SupportAmount * (PlatformFeePercent / 100m) : 0m;
        var totalUsdc = request.SupportAmount + platformFee;
        var gasSponsored = await _gasSponsorshipService.ShouldSponsorGasAsync(userWallet.PublicKey);
        var gasFeeUsdc = gasSponsored ? _config.GasSponsorship.FlatFeeUsdc : 0;
        var usdcRequired = totalUsdc + gasFeeUsdc;
        var solRequired = gasSponsored ? 0 : MinSolForGas;

        // 6. Check balances
        var hasEnoughUsdc = usdcBalance >= usdcRequired;
        var hasEnoughSol = gasSponsored || solBalance >= solRequired;

        string? errorCode = null;
        string? message = null;

        if (!hasEnoughUsdc)
        {
            errorCode = SupportIntentErrorCodes.InsufficientUsdc;
            message = $"Insufficient USDC. You have {usdcBalance:F2} USDC but need {usdcRequired:F2} USDC";
        }
        else if (!hasEnoughSol)
        {
            errorCode = SupportIntentErrorCodes.InsufficientSol;
            message = $"Insufficient SOL for gas. You have {solBalance:F6} SOL but need {solRequired:F6} SOL";
        }

        return new CheckSupportBalanceResponse
        {
            CanSupport = hasEnoughUsdc && hasEnoughSol,
            HasWallet = true,
            UsdcBalance = usdcBalance,
            SolBalance = solBalance,
            SupportAmount = request.SupportAmount,
            PlatformFee = platformFee,
            PlatformFeePercent = request.IncludePlatformFee ? PlatformFeePercent : 0,
            UsdcRequired = usdcRequired,
            SolRequired = solRequired,
            GasSponsored = gasSponsored,
            GasFeeUsdc = gasFeeUsdc,
            ErrorCode = errorCode,
            Message = message,
            Bird = new BirdSupportInfo
            {
                BirdId = request.BirdId,
                Name = bird.name,
                ImageUrl = bird.image_url
            },
            Recipient = new RecipientInfo
            {
                UserId = recipientUserId,
                Name = recipient?.Name ?? "Unknown",
                WalletAddress = recipientWallet.PublicKey
            }
        };
    }

    /// <inheritdoc />
    public async Task<(SupportIntentResponse? Response, ValidationErrorResponse? Error)> CreateSupportIntentAsync(
        Guid supporterUserId,
        CreateSupportIntentRequest request)
    {
        // First do a balance check
        var balanceCheck = await CheckSupportBalanceAsync(supporterUserId, new CheckSupportBalanceRequest
        {
            BirdId = request.BirdId,
            SupportAmount = request.SupportAmount,
            IncludePlatformFee = request.IncludePlatformFee
        });

        if (!balanceCheck.CanSupport)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = balanceCheck.ErrorCode ?? SupportIntentErrorCodes.InternalError,
                Message = balanceCheck.Message ?? "Cannot create support intent",
                FieldErrors = balanceCheck.ErrorCode == SupportIntentErrorCodes.WalletRequired
                    ? new Dictionary<string, string[]> { ["wallet"] = new[] { "Wallet connection required" } }
                    : new Dictionary<string, string[]>()
            });
        }

        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Get bird and recipient info
        var bird = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT bird_id, owner_id, name FROM birds WHERE bird_id = @BirdId",
            new { request.BirdId });

        Guid recipientUserId = bird.owner_id;
        string birdName = bird.name;

        var recipient = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE user_id = @UserId",
            new { UserId = recipientUserId });

        var supporterWallet = await _walletService.GetPrimaryWalletAsync(supporterUserId);
        var recipientWallet = await _walletService.GetPrimaryWalletAsync(recipientUserId);

        // Calculate amounts using balance check results
        var platformFee = balanceCheck.PlatformFee;
        var platformFeePercent = balanceCheck.PlatformFeePercent;
        var totalAmount = request.SupportAmount + platformFee + balanceCheck.GasFeeUsdc;

        // Get platform wallet for fee transfer
        var platformWalletPubkey = await _gasSponsorshipService.GetSponsorWalletPubkeyAsync();

        // Build the Solana transaction (transfers to both bird owner and platform)
        string? serializedTx = null;
        try
        {
            var recipientAtaExists = await _solanaService.CheckAtaExistsAsync(recipientWallet!.PublicKey);
            var sponsorPubkey = balanceCheck.GasSponsored ? platformWalletPubkey : null;

            // Build transaction with both transfers:
            // 1. Support amount -> bird owner wallet
            // 2. Platform fee -> platform wallet (if fee > 0)
            serializedTx = await _solanaService.BuildUsdcTransferTransactionAsync(
                supporterWallet!.PublicKey,
                recipientWallet.PublicKey,
                request.SupportAmount, // USDC to bird owner
                sponsorPubkey,
                !recipientAtaExists,
                platformFee > 0 ? platformWalletPubkey : null, // Platform wallet for fee
                platformFee); // Platform fee amount
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build transaction for support intent");
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "Failed to build transaction. Please try again."
            });
        }

        // Create the intent
        var intent = new SupportIntent
        {
            Id = Guid.NewGuid(),
            SupporterUserId = supporterUserId,
            BirdId = request.BirdId,
            RecipientUserId = recipientUserId,
            SupportAmount = request.SupportAmount,
            PlatformFee = platformFee,
            PlatformFeePercent = platformFeePercent,
            TotalAmount = totalAmount,
            Currency = request.Currency,
            Status = SupportIntentStatus.AwaitingPayment,
            PaymentMethod = SupportPaymentMethod.Wallet,
            SenderWalletPubkey = supporterWallet!.PublicKey,
            RecipientWalletPubkey = recipientWallet!.PublicKey,
            SerializedTransaction = serializedTx,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.PaymentExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Record gas sponsorship if applicable
        if (balanceCheck.GasSponsored && !string.IsNullOrEmpty(platformWalletPubkey))
        {
            await _gasSponsorshipService.RecordSponsorshipAsync(
                intent.Id,
                _config.GasSponsorship.MinSolThreshold,
                platformWalletPubkey,
                false,
                null);
        }

        // Insert intent
        await conn.ExecuteAsync(
            @"INSERT INTO support_intents
              (id, supporter_user_id, bird_id, recipient_user_id, support_amount, platform_support_amount,
               platform_fee_percent, total_amount, currency, status, payment_method, sender_wallet_pubkey,
               recipient_wallet_pubkey, serialized_transaction, expires_at, created_at, updated_at)
              VALUES (@Id, @SupporterUserId, @BirdId, @RecipientUserId, @SupportAmount, @PlatformFee,
               @PlatformFeePercent, @TotalAmount, @Currency, @Status, @PaymentMethod, @SenderWalletPubkey,
               @RecipientWalletPubkey, @SerializedTransaction, @ExpiresAt, @CreatedAt, @UpdatedAt)",
            intent);

        _logger.LogInformation(
            "Support intent created: {IntentId} for bird {BirdId}, Amount: {Amount} USDC, Fee: {Fee} USDC ({Percent}%), GasSponsored: {GasSponsored}",
            intent.Id, request.BirdId, request.SupportAmount, platformFee, platformFeePercent, balanceCheck.GasSponsored);

        return (new SupportIntentResponse
        {
            IntentId = intent.Id,
            BirdId = request.BirdId,
            BirdName = birdName,
            RecipientUserId = recipientUserId,
            RecipientName = recipient?.Name ?? "Unknown",
            RecipientWalletAddress = recipientWallet!.PublicKey,
            SupportAmount = request.SupportAmount,
            PlatformFee = platformFee,
            PlatformFeePercent = platformFeePercent,
            TotalAmount = totalAmount,
            Currency = request.Currency,
            Status = intent.Status,
            SerializedTransaction = serializedTx,
            GasSponsored = balanceCheck.GasSponsored,
            GasFeeUsdc = balanceCheck.GasFeeUsdc,
            ExpiresAt = intent.ExpiresAt,
            CreatedAt = intent.CreatedAt
        }, null);
    }

    /// <inheritdoc />
    public async Task<(SubmitTransactionResponse? Response, ValidationErrorResponse? Error)> SubmitSignedTransactionAsync(
        Guid userId,
        Guid intentId,
        string signedTransaction)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var intent = await conn.QueryFirstOrDefaultAsync<SupportIntent>(
            "SELECT * FROM support_intents WHERE id = @IntentId AND supporter_user_id = @UserId",
            new { IntentId = intentId, UserId = userId });

        if (intent == null)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.IntentNotFound,
                Message = "Support intent not found"
            });
        }

        if (intent.IsExpired)
        {
            await conn.ExecuteAsync(
                "UPDATE support_intents SET status = @Status, updated_at = @UpdatedAt WHERE id = @IntentId",
                new { Status = SupportIntentStatus.Expired, UpdatedAt = DateTime.UtcNow, IntentId = intentId });

            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.IntentExpired,
                Message = "Support intent has expired. Please create a new one."
            });
        }

        if (intent.Status != SupportIntentStatus.AwaitingPayment)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.IntentAlreadyProcessed,
                Message = $"Support intent is in invalid state: {intent.Status}"
            });
        }

        try
        {
            // Submit to Solana
            var signature = await _solanaService.SubmitTransactionAsync(signedTransaction);

            // Update intent status
            await conn.ExecuteAsync(
                @"UPDATE support_intents
                  SET status = @Status, solana_signature = @Signature, paid_at = @PaidAt, updated_at = @UpdatedAt
                  WHERE id = @IntentId",
                new
                {
                    Status = SupportIntentStatus.Processing,
                    Signature = signature,
                    PaidAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IntentId = intentId
                });

            _logger.LogInformation(
                "Support intent {IntentId} submitted to Solana: {Signature}",
                intentId, signature);

            return (new SubmitTransactionResponse
            {
                PaymentId = intentId,
                SolanaSignature = signature,
                Status = SupportIntentStatus.Processing
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit transaction for support intent {IntentId}", intentId);

            await conn.ExecuteAsync(
                "UPDATE support_intents SET status = @Status, updated_at = @UpdatedAt WHERE id = @IntentId",
                new { Status = SupportIntentStatus.Failed, UpdatedAt = DateTime.UtcNow, IntentId = intentId });

            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.TransactionFailed,
                Message = "Transaction failed: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Called by background job when transaction is confirmed on-chain.
    /// Records ledger entries and updates bird support count.
    /// </summary>
    public async Task<bool> ConfirmTransactionAsync(Guid intentId, int confirmations)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var intent = await conn.QueryFirstOrDefaultAsync<SupportIntent>(
            "SELECT * FROM support_intents WHERE id = @IntentId",
            new { IntentId = intentId });

        if (intent == null || intent.Status != SupportIntentStatus.Processing)
        {
            return false;
        }

        // Update confirmations
        await conn.ExecuteAsync(
            "UPDATE support_intents SET confirmations = @Confirmations, updated_at = @UpdatedAt WHERE id = @IntentId",
            new { Confirmations = confirmations, UpdatedAt = DateTime.UtcNow, IntentId = intentId });

        // Check if we have enough confirmations
        if (confirmations >= _config.RequiredConfirmations)
        {
            // Record ledger entries
            await RecordSupportLedgerEntriesAsync(conn, intent);

            // Update bird support count
            await conn.ExecuteAsync(
                "UPDATE birds SET supported_count = supported_count + 1 WHERE bird_id = @BirdId",
                new { intent.BirdId });

            // Mark as completed
            await conn.ExecuteAsync(
                @"UPDATE support_intents
                  SET status = @Status, completed_at = @CompletedAt, updated_at = @UpdatedAt
                  WHERE id = @IntentId",
                new
                {
                    Status = SupportIntentStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IntentId = intentId
                });

            _logger.LogInformation(
                "Support intent {IntentId} completed: {Amount} USDC confirmed on-chain",
                intentId, intent.TotalAmount);

            return true;
        }

        return false;
    }

    private async Task RecordSupportLedgerEntriesAsync(System.Data.IDbConnection conn, SupportIntent intent)
    {
        var now = DateTime.UtcNow;

        // Get current balances
        var supporterBalance = await conn.ExecuteScalarAsync<decimal?>(
            "SELECT balance_after FROM ledger_entries WHERE user_id = @UserId ORDER BY created_at DESC LIMIT 1",
            new { UserId = intent.SupporterUserId }) ?? 0m;

        var recipientBalance = await conn.ExecuteScalarAsync<decimal?>(
            "SELECT balance_after FROM ledger_entries WHERE user_id = @UserId ORDER BY created_at DESC LIMIT 1",
            new { UserId = intent.RecipientUserId }) ?? 0m;

        // Debit supporter
        await conn.ExecuteAsync(
            @"INSERT INTO ledger_entries (id, user_id, amount_usdc, entry_type, reference_type, reference_id, balance_after, description, created_at)
              VALUES (@Id, @UserId, @Amount, @EntryType, @RefType, @RefId, @BalanceAfter, @Description, @CreatedAt)",
            new
            {
                Id = Guid.NewGuid(),
                UserId = intent.SupporterUserId,
                Amount = -intent.TotalAmount,
                EntryType = "SupportPayment",
                RefType = "SupportIntent",
                RefId = intent.Id,
                BalanceAfter = supporterBalance - intent.TotalAmount,
                Description = "Support payment (on-chain)",
                CreatedAt = now
            });

        // Credit recipient (support amount only)
        await conn.ExecuteAsync(
            @"INSERT INTO ledger_entries (id, user_id, amount_usdc, entry_type, reference_type, reference_id, balance_after, description, created_at)
              VALUES (@Id, @UserId, @Amount, @EntryType, @RefType, @RefId, @BalanceAfter, @Description, @CreatedAt)",
            new
            {
                Id = Guid.NewGuid(),
                UserId = intent.RecipientUserId,
                Amount = intent.SupportAmount,
                EntryType = "SupportReceived",
                RefType = "SupportIntent",
                RefId = intent.Id,
                BalanceAfter = recipientBalance + intent.SupportAmount,
                Description = "Support received (on-chain)",
                CreatedAt = now
            });

        // Log platform fee
        if (intent.PlatformFee > 0)
        {
            _logger.LogInformation(
                "Platform fee: {Amount} USDC ({Percent}%) from support intent {IntentId}",
                intent.PlatformFee, intent.PlatformFeePercent, intent.Id);
        }
    }

    /// <inheritdoc />
    public async Task<(SupportIntentResponse? Response, ValidationErrorResponse? Error)> ConfirmSupportIntentAsync(
        Guid intentId,
        string? externalReference = null)
    {
        // This is now handled by ConfirmTransactionAsync for on-chain confirmations
        // This method can be used for manual confirmation by admin if needed
        return (null, new ValidationErrorResponse
        {
            ErrorCode = SupportIntentErrorCodes.InternalError,
            Message = "Use SubmitSignedTransaction to complete payment. Confirmation happens automatically on-chain."
        });
    }

    /// <inheritdoc />
    public async Task<SupportIntent?> GetSupportIntentAsync(Guid intentId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.QueryFirstOrDefaultAsync<SupportIntent>(
            @"SELECT * FROM support_intents
              WHERE id = @IntentId AND (supporter_user_id = @UserId OR recipient_user_id = @UserId)",
            new { IntentId = intentId, UserId = userId });
    }

    /// <inheritdoc />
    public async Task<SupportIntentResponse?> GetSupportIntentResponseAsync(Guid intentId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var intent = await GetSupportIntentAsync(intentId, userId);
        if (intent == null) return null;

        var bird = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT name FROM birds WHERE bird_id = @BirdId",
            new { intent.BirdId });

        var recipient = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE user_id = @UserId",
            new { UserId = intent.RecipientUserId });

        // Get gas sponsorship info
        var sponsorship = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT fee_usdc_charged FROM gas_sponsorships WHERE payment_id = @IntentId",
            new { IntentId = intentId });

        return new SupportIntentResponse
        {
            IntentId = intent.Id,
            BirdId = intent.BirdId,
            BirdName = bird?.name ?? "Unknown",
            RecipientUserId = intent.RecipientUserId,
            RecipientName = recipient?.Name ?? "Unknown",
            RecipientWalletAddress = intent.RecipientWalletPubkey,
            SupportAmount = intent.SupportAmount,
            PlatformFee = intent.PlatformFee,
            PlatformFeePercent = intent.PlatformFeePercent,
            TotalAmount = intent.TotalAmount,
            Currency = intent.Currency,
            Status = intent.Status,
            SerializedTransaction = intent.SerializedTransaction,
            GasSponsored = sponsorship != null,
            GasFeeUsdc = sponsorship?.fee_usdc_charged ?? 0,
            ExpiresAt = intent.ExpiresAt,
            CreatedAt = intent.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<bool> CancelSupportIntentAsync(Guid intentId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var rowsAffected = await conn.ExecuteAsync(
            @"UPDATE support_intents
              SET status = @Status, updated_at = @UpdatedAt
              WHERE id = @IntentId AND supporter_user_id = @UserId
              AND status IN (@Pending, @AwaitingPayment)",
            new
            {
                Status = SupportIntentStatus.Cancelled,
                UpdatedAt = DateTime.UtcNow,
                IntentId = intentId,
                UserId = userId,
                Pending = SupportIntentStatus.Pending,
                AwaitingPayment = SupportIntentStatus.AwaitingPayment
            });

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SupportIntentResponse>> GetUserSupportHistoryAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var offset = (page - 1) * pageSize;

        var intents = await conn.QueryAsync<SupportIntent>(
            @"SELECT * FROM support_intents
              WHERE supporter_user_id = @UserId
              ORDER BY created_at DESC
              LIMIT @Limit OFFSET @Offset",
            new { UserId = userId, Limit = pageSize, Offset = offset });

        var totalCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM support_intents WHERE supporter_user_id = @UserId",
            new { UserId = userId });

        var responses = new List<SupportIntentResponse>();
        foreach (var intent in intents)
        {
            var response = await GetSupportIntentResponseAsync(intent.Id, userId);
            if (response != null)
            {
                responses.Add(response);
            }
        }

        return new PagedResult<SupportIntentResponse>
        {
            Items = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
