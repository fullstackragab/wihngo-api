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
/// Core service for P2P USDC payments on Solana
/// </summary>
public class P2PPaymentService : IP2PPaymentService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ISolanaTransactionService _solanaService;
    private readonly IGasSponsorshipService _gasSponsorshipService;
    private readonly ILedgerService _ledgerService;
    private readonly IWalletService _walletService;
    private readonly P2PPaymentConfiguration _config;
    private readonly ILogger<P2PPaymentService> _logger;

    public P2PPaymentService(
        IDbConnectionFactory dbFactory,
        ISolanaTransactionService solanaService,
        IGasSponsorshipService gasSponsorshipService,
        ILedgerService ledgerService,
        IWalletService walletService,
        IOptions<P2PPaymentConfiguration> config,
        ILogger<P2PPaymentService> logger)
    {
        _dbFactory = dbFactory;
        _solanaService = solanaService;
        _gasSponsorshipService = gasSponsorshipService;
        _ledgerService = ledgerService;
        _walletService = walletService;
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PreflightPaymentResponse> PreflightPaymentAsync(Guid senderUserId, PreflightPaymentRequest request)
    {
        try
        {
            // 1. Find recipient
            using var conn = await _dbFactory.CreateOpenConnectionAsync();

            User? recipient = null;

            // Try to find by user ID first
            if (Guid.TryParse(request.RecipientId, out var recipientGuid))
            {
                recipient = await conn.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM users WHERE user_id = @UserId",
                    new { UserId = recipientGuid });
            }

            // Try by username if not found
            if (recipient == null)
            {
                recipient = await conn.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM users WHERE LOWER(name) = LOWER(@Name)",
                    new { Name = request.RecipientId });
            }

            if (recipient == null)
            {
                return new PreflightPaymentResponse
                {
                    Valid = false,
                    ErrorMessage = "Recipient not found",
                    ErrorCode = "RECIPIENT_NOT_FOUND"
                };
            }

            // 2. Check sender is not recipient
            if (recipient.UserId == senderUserId)
            {
                return new PreflightPaymentResponse
                {
                    Valid = false,
                    ErrorMessage = "Cannot send payment to yourself",
                    ErrorCode = "SELF_PAYMENT"
                };
            }

            // 3. Check amount is valid
            if (request.Amount < _config.MinPaymentUsdc)
            {
                return new PreflightPaymentResponse
                {
                    Valid = false,
                    ErrorMessage = $"Minimum payment is ${_config.MinPaymentUsdc}",
                    ErrorCode = "AMOUNT_TOO_LOW"
                };
            }

            if (request.Amount > _config.MaxPaymentUsdc)
            {
                return new PreflightPaymentResponse
                {
                    Valid = false,
                    ErrorMessage = $"Maximum payment is ${_config.MaxPaymentUsdc}",
                    ErrorCode = "AMOUNT_TOO_HIGH"
                };
            }

            // 4. Check sender has wallet linked
            var senderWallet = await _walletService.GetPrimaryWalletAsync(senderUserId);
            if (senderWallet == null)
            {
                return new PreflightPaymentResponse
                {
                    Valid = false,
                    ErrorMessage = "Please link a wallet first",
                    ErrorCode = "NO_WALLET"
                };
            }

            // 5. Check if gas sponsorship is needed
            var gasNeeded = await _gasSponsorshipService.ShouldSponsorGasAsync(senderWallet.PublicKey);
            var fee = gasNeeded ? _gasSponsorshipService.GetSponsorshipFeeUsdc() : 0m;

            // 6. Check sender has sufficient on-chain USDC
            var senderUsdcBalance = await _solanaService.GetUsdcBalanceAsync(senderWallet.PublicKey);
            var totalRequired = request.Amount + fee;

            if (senderUsdcBalance < totalRequired)
            {
                return new PreflightPaymentResponse
                {
                    Valid = false,
                    ErrorMessage = $"Insufficient USDC balance. You have ${senderUsdcBalance:F2}, need ${totalRequired:F2}",
                    ErrorCode = "INSUFFICIENT_BALANCE"
                };
            }

            return new PreflightPaymentResponse
            {
                Valid = true,
                RecipientName = recipient.Name,
                RecipientUsername = recipient.Name,
                RecipientUserId = recipient.UserId,
                GasNeeded = gasNeeded,
                EstimatedFee = fee
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in preflight payment check");
            return new PreflightPaymentResponse
            {
                Valid = false,
                ErrorMessage = "An error occurred validating the payment",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(Guid senderUserId, CreatePaymentIntentRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Validate sender has wallet
        var senderWallet = await _walletService.GetPrimaryWalletAsync(senderUserId);
        if (senderWallet == null)
        {
            throw new InvalidOperationException("Sender must have a linked wallet");
        }

        // 2. Get recipient info
        var recipient = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE user_id = @UserId",
            new { UserId = request.RecipientUserId });

        if (recipient == null)
        {
            throw new InvalidOperationException("Recipient not found");
        }

        // 3. Get recipient wallet (optional - they can have an ATA created)
        var recipientWallet = await _walletService.GetPrimaryWalletAsync(request.RecipientUserId);
        var recipientPubkey = recipientWallet?.PublicKey;

        if (recipientPubkey == null)
        {
            throw new InvalidOperationException("Recipient must have a linked wallet to receive payments");
        }

        // 4. Check if gas sponsorship is needed
        var gasSponsored = await _gasSponsorshipService.ShouldSponsorGasAsync(senderWallet.PublicKey);
        var fee = gasSponsored ? _gasSponsorshipService.GetSponsorshipFeeUsdc() : 0m;

        // 5. Check if recipient ATA needs to be created
        var recipientAtaExists = await _solanaService.CheckAtaExistsAsync(recipientPubkey);

        // 6. Get sponsor wallet pubkey for fee payer
        var sponsorWalletPubkey = gasSponsored
            ? await _gasSponsorshipService.GetSponsorWalletPubkeyAsync()
            : null;

        // 7. Build unsigned transaction
        var serializedTx = await _solanaService.BuildUsdcTransferTransactionAsync(
            senderWallet.PublicKey,
            recipientPubkey,
            request.AmountUsdc,
            sponsorWalletPubkey,
            !recipientAtaExists);

        // 8. Create payment record
        var payment = new P2PPayment
        {
            Id = Guid.NewGuid(),
            SenderUserId = senderUserId,
            RecipientUserId = request.RecipientUserId,
            SenderWalletPubkey = senderWallet.PublicKey,
            RecipientWalletPubkey = recipientPubkey,
            AmountUsdc = request.AmountUsdc,
            FeeUsdc = fee,
            Status = P2PPaymentStatus.Pending,
            GasSponsored = gasSponsored,
            SerializedTransaction = serializedTx,
            Memo = request.Memo,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.PaymentExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await conn.ExecuteAsync(
            @"INSERT INTO p2p_payments
              (id, sender_user_id, recipient_user_id, sender_wallet_pubkey, recipient_wallet_pubkey,
               amount_usdc, fee_usdc, status, gas_sponsored, serialized_transaction, memo, expires_at, created_at, updated_at)
              VALUES (@Id, @SenderUserId, @RecipientUserId, @SenderWalletPubkey, @RecipientWalletPubkey,
               @AmountUsdc, @FeeUsdc, @Status, @GasSponsored, @SerializedTransaction, @Memo, @ExpiresAt, @CreatedAt, @UpdatedAt)",
            payment);

        _logger.LogInformation(
            "Created payment intent {PaymentId}: {Amount} USDC from {Sender} to {Recipient}, Fee: {Fee}, GasSponsored: {Sponsored}",
            payment.Id, payment.AmountUsdc, senderUserId, request.RecipientUserId, fee, gasSponsored);

        return new PaymentIntentResponse
        {
            PaymentId = payment.Id,
            SerializedTransaction = serializedTx,
            GasSponsored = gasSponsored,
            FeeUsdc = fee,
            TotalUsdc = payment.AmountUsdc + fee,
            AmountUsdc = payment.AmountUsdc,
            ExpiresAt = payment.ExpiresAt,
            Status = payment.Status,
            Recipient = new RecipientInfo
            {
                UserId = recipient.UserId,
                Name = recipient.Name,
                Username = recipient.Name,
                ProfileImage = recipient.ProfileImage
            }
        };
    }

    /// <inheritdoc />
    public async Task<PaymentStatusResponse?> GetPaymentIntentAsync(Guid paymentId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var payment = await conn.QueryFirstOrDefaultAsync<P2PPayment>(
            "SELECT * FROM p2p_payments WHERE id = @PaymentId AND (sender_user_id = @UserId OR recipient_user_id = @UserId)",
            new { PaymentId = paymentId, UserId = userId });

        if (payment == null) return null;

        // Get sender and recipient info
        var sender = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE user_id = @UserId",
            new { UserId = payment.SenderUserId });

        var recipient = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE user_id = @UserId",
            new { UserId = payment.RecipientUserId });

        return new PaymentStatusResponse
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            SolanaSignature = payment.SolanaSignature,
            Confirmations = payment.Confirmations,
            RequiredConfirmations = _config.RequiredConfirmations,
            AmountUsdc = payment.AmountUsdc,
            FeeUsdc = payment.FeeUsdc,
            TotalUsdc = payment.TotalUsdc,
            GasSponsored = payment.GasSponsored,
            Memo = payment.Memo,
            Sender = sender != null ? new SenderInfo
            {
                UserId = sender.UserId,
                Name = sender.Name,
                Username = sender.Name,
                ProfileImage = sender.ProfileImage
            } : null,
            Recipient = recipient != null ? new RecipientInfo
            {
                UserId = recipient.UserId,
                Name = recipient.Name,
                Username = recipient.Name,
                ProfileImage = recipient.ProfileImage
            } : null,
            CreatedAt = payment.CreatedAt,
            SubmittedAt = payment.SubmittedAt,
            ConfirmedAt = payment.ConfirmedAt,
            CompletedAt = payment.CompletedAt,
            ExpiresAt = payment.ExpiresAt
        };
    }

    /// <inheritdoc />
    public async Task<SubmitTransactionResponse> SubmitSignedTransactionAsync(Guid userId, SubmitTransactionRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Get payment
        var payment = await conn.QueryFirstOrDefaultAsync<P2PPayment>(
            "SELECT * FROM p2p_payments WHERE id = @PaymentId AND sender_user_id = @UserId",
            new { PaymentId = request.PaymentId, UserId = userId });

        if (payment == null)
        {
            return new SubmitTransactionResponse
            {
                PaymentId = request.PaymentId,
                Status = P2PPaymentStatus.Failed,
                ErrorMessage = "Payment not found"
            };
        }

        // 2. Check for idempotency - if same key was already used, return existing result
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existingWithSameKey = await conn.QueryFirstOrDefaultAsync<P2PPayment>(@"
                SELECT * FROM p2p_payments
                WHERE id = @PaymentId
                AND idempotency_key = @IdempotencyKey
                AND status NOT IN ('pending', 'expired', 'cancelled')",
                new { PaymentId = request.PaymentId, IdempotencyKey = request.IdempotencyKey });

            if (existingWithSameKey != null)
            {
                _logger.LogInformation(
                    "Idempotent request detected for payment {PaymentId} with key {Key}. Returning existing result.",
                    request.PaymentId, request.IdempotencyKey);

                return new SubmitTransactionResponse
                {
                    PaymentId = existingWithSameKey.Id,
                    SolanaSignature = existingWithSameKey.SolanaSignature,
                    Status = existingWithSameKey.Status,
                    WasAlreadySubmitted = true
                };
            }
        }

        // 3. Check payment is still pending
        if (payment.Status != P2PPaymentStatus.Pending)
        {
            return new SubmitTransactionResponse
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                SolanaSignature = payment.SolanaSignature,
                ErrorMessage = $"Payment is already {payment.Status}",
                WasAlreadySubmitted = payment.Status == P2PPaymentStatus.Submitted ||
                                      payment.Status == P2PPaymentStatus.Confirming ||
                                      payment.Status == P2PPaymentStatus.Confirmed ||
                                      payment.Status == P2PPaymentStatus.Completed
            };
        }

        // 4. Check not expired
        if (payment.IsExpired)
        {
            await conn.ExecuteAsync(
                "UPDATE p2p_payments SET status = @Status, updated_at = @UpdatedAt WHERE id = @PaymentId",
                new { Status = P2PPaymentStatus.Expired, UpdatedAt = DateTime.UtcNow, PaymentId = payment.Id });

            return new SubmitTransactionResponse
            {
                PaymentId = payment.Id,
                Status = P2PPaymentStatus.Expired,
                ErrorMessage = "Payment intent has expired"
            };
        }

        try
        {
            // 4. Prepare the transaction for submission
            var transactionToSubmit = request.SignedTransaction;

            // If gas is sponsored, the platform wallet must add its signature
            if (payment.GasSponsored)
            {
                _logger.LogInformation(
                    "Adding sponsor signature for gas-sponsored payment {PaymentId}",
                    payment.Id);

                transactionToSubmit = await _solanaService.AddSponsorSignatureAsync(request.SignedTransaction);
            }

            // 5. Submit the fully-signed transaction
            var signature = await _solanaService.SubmitTransactionAsync(transactionToSubmit);

            // 7. Record gas sponsorship if applicable
            if (payment.GasSponsored)
            {
                var sponsorPubkey = await _gasSponsorshipService.GetSponsorWalletPubkeyAsync();
                if (sponsorPubkey != null)
                {
                    // Estimate SOL used (approximately 0.000005 SOL per signature + compute)
                    await _gasSponsorshipService.RecordSponsorshipAsync(
                        payment.Id,
                        0.000005m, // Estimated SOL
                        sponsorPubkey);
                }
            }

            // 8. Update payment status with idempotency key
            await conn.ExecuteAsync(
                @"UPDATE p2p_payments
                  SET status = @Status,
                      solana_signature = @Signature,
                      idempotency_key = @IdempotencyKey,
                      submitted_at = @SubmittedAt,
                      updated_at = @UpdatedAt
                  WHERE id = @PaymentId",
                new
                {
                    Status = P2PPaymentStatus.Submitted,
                    Signature = signature,
                    IdempotencyKey = request.IdempotencyKey,
                    SubmittedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PaymentId = payment.Id
                });

            _logger.LogInformation(
                "Payment {PaymentId} submitted to Solana: {Signature}",
                payment.Id, signature);

            return new SubmitTransactionResponse
            {
                PaymentId = payment.Id,
                SolanaSignature = signature,
                Status = P2PPaymentStatus.Submitted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit payment {PaymentId} to Solana", payment.Id);

            await conn.ExecuteAsync(
                "UPDATE p2p_payments SET status = @Status, updated_at = @UpdatedAt WHERE id = @PaymentId",
                new { Status = P2PPaymentStatus.Failed, UpdatedAt = DateTime.UtcNow, PaymentId = payment.Id });

            return new SubmitTransactionResponse
            {
                PaymentId = payment.Id,
                Status = P2PPaymentStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelPaymentAsync(Guid paymentId, Guid userId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var rowsAffected = await conn.ExecuteAsync(
            @"UPDATE p2p_payments
              SET status = @Status, updated_at = @UpdatedAt
              WHERE id = @PaymentId AND sender_user_id = @UserId AND status = @CurrentStatus",
            new
            {
                Status = P2PPaymentStatus.Cancelled,
                UpdatedAt = DateTime.UtcNow,
                PaymentId = paymentId,
                UserId = userId,
                CurrentStatus = P2PPaymentStatus.Pending
            });

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Payment {PaymentId} cancelled by user {UserId}", paymentId, userId);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PaymentSummary>> GetUserPaymentsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var offset = (page - 1) * pageSize;

        var payments = await conn.QueryAsync<P2PPayment>(
            @"SELECT * FROM p2p_payments
              WHERE sender_user_id = @UserId OR recipient_user_id = @UserId
              ORDER BY created_at DESC
              LIMIT @Limit OFFSET @Offset",
            new { UserId = userId, Limit = pageSize, Offset = offset });

        var totalCount = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM p2p_payments
              WHERE sender_user_id = @UserId OR recipient_user_id = @UserId",
            new { UserId = userId });

        // Get other party info for each payment
        var summaries = new List<PaymentSummary>();
        foreach (var payment in payments)
        {
            var isSender = payment.SenderUserId == userId;
            var otherPartyId = isSender ? payment.RecipientUserId : payment.SenderUserId;

            var otherParty = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE user_id = @UserId",
                new { UserId = otherPartyId });

            summaries.Add(new PaymentSummary
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                AmountUsdc = payment.AmountUsdc,
                Memo = payment.Memo,
                CreatedAt = payment.CreatedAt,
                IsSender = isSender,
                OtherParty = otherParty != null ? new UserSummaryDto
                {
                    UserId = otherParty.UserId,
                    Name = otherParty.Name,
                    ProfileImage = otherParty.ProfileImage
                } : null
            });
        }

        return new PagedResult<PaymentSummary>
        {
            Items = summaries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<bool> CheckPaymentConfirmationAsync(Guid paymentId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var payment = await conn.QueryFirstOrDefaultAsync<P2PPayment>(
            "SELECT * FROM p2p_payments WHERE id = @PaymentId",
            new { PaymentId = paymentId });

        if (payment == null || string.IsNullOrEmpty(payment.SolanaSignature))
        {
            return false;
        }

        // Get transaction status
        var status = await _solanaService.GetTransactionStatusAsync(payment.SolanaSignature);

        if (!status.Found)
        {
            _logger.LogWarning("Transaction {Signature} not found for payment {PaymentId}",
                payment.SolanaSignature, paymentId);
            return false;
        }

        if (status.Error != null)
        {
            // Transaction failed on-chain
            await conn.ExecuteAsync(
                "UPDATE p2p_payments SET status = @Status, updated_at = @UpdatedAt WHERE id = @PaymentId",
                new { Status = P2PPaymentStatus.Failed, UpdatedAt = DateTime.UtcNow, PaymentId = paymentId });

            _logger.LogError("Payment {PaymentId} failed on-chain: {Error}", paymentId, status.Error);
            return false;
        }

        // Update confirmations
        await conn.ExecuteAsync(
            @"UPDATE p2p_payments
              SET confirmations = @Confirmations, block_slot = @Slot, updated_at = @UpdatedAt
              WHERE id = @PaymentId",
            new
            {
                Confirmations = status.Confirmations,
                Slot = status.Slot,
                UpdatedAt = DateTime.UtcNow,
                PaymentId = paymentId
            });

        // Check if confirmed (finalized)
        if (status.Finalized || status.Confirmations >= _config.RequiredConfirmations)
        {
            // Verify the transaction matches expected parameters
            var verification = await _solanaService.VerifyTransactionAsync(
                payment.SolanaSignature,
                payment.SenderWalletPubkey!,
                payment.RecipientWalletPubkey!,
                payment.AmountUsdc);

            if (!verification.Success)
            {
                _logger.LogError(
                    "Payment {PaymentId} verification failed: Sender={Sender}, Recipient={Recipient}, Amount={Amount}",
                    paymentId, verification.SenderMatches, verification.RecipientMatches, verification.AmountMatches);

                await conn.ExecuteAsync(
                    "UPDATE p2p_payments SET status = @Status, updated_at = @UpdatedAt WHERE id = @PaymentId",
                    new { Status = P2PPaymentStatus.Failed, UpdatedAt = DateTime.UtcNow, PaymentId = paymentId });

                return false;
            }

            // Mark as confirmed
            await conn.ExecuteAsync(
                @"UPDATE p2p_payments
                  SET status = @Status, confirmed_at = @ConfirmedAt, updated_at = @UpdatedAt
                  WHERE id = @PaymentId",
                new
                {
                    Status = P2PPaymentStatus.Confirmed,
                    ConfirmedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PaymentId = paymentId
                });

            // Reload payment for ledger recording
            payment = await conn.QueryFirstOrDefaultAsync<P2PPayment>(
                "SELECT * FROM p2p_payments WHERE id = @PaymentId",
                new { PaymentId = paymentId });

            // Record ledger entries
            payment!.Status = P2PPaymentStatus.Completed;
            await _ledgerService.RecordPaymentAsync(payment);

            // Mark as completed
            await conn.ExecuteAsync(
                @"UPDATE p2p_payments
                  SET status = @Status, completed_at = @CompletedAt, updated_at = @UpdatedAt
                  WHERE id = @PaymentId",
                new
                {
                    Status = P2PPaymentStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PaymentId = paymentId
                });

            _logger.LogInformation(
                "Payment {PaymentId} completed: {Amount} USDC from {Sender} to {Recipient}",
                paymentId, payment.AmountUsdc, payment.SenderUserId, payment.RecipientUserId);

            return true;
        }

        // Still confirming
        if (payment.Status != P2PPaymentStatus.Confirming)
        {
            await conn.ExecuteAsync(
                "UPDATE p2p_payments SET status = @Status, updated_at = @UpdatedAt WHERE id = @PaymentId",
                new { Status = P2PPaymentStatus.Confirming, UpdatedAt = DateTime.UtcNow, PaymentId = paymentId });
        }

        return false;
    }

    /// <inheritdoc />
    public Task<List<P2PPayment>> GetPendingConfirmationsAsync()
    {
        // p2p_payments table deprecated - new payment system uses payments table
        // Return empty list to prevent background job errors
        return Task.FromResult(new List<P2PPayment>());
    }

    /// <inheritdoc />
    public async Task HandlePaymentTimeoutAsync(Guid paymentId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var payment = await conn.QueryFirstOrDefaultAsync<P2PPayment>(
            "SELECT * FROM p2p_payments WHERE id = @PaymentId",
            new { PaymentId = paymentId });

        if (payment == null || string.IsNullOrEmpty(payment.SolanaSignature))
        {
            _logger.LogWarning("Cannot handle timeout for payment {PaymentId}: not found or no signature", paymentId);
            return;
        }

        // Check if transaction exists on-chain (may have appeared after timeout check)
        var status = await _solanaService.GetTransactionStatusAsync(payment.SolanaSignature);

        if (status.Found)
        {
            // Transaction exists on-chain - let normal confirmation flow handle it
            _logger.LogInformation(
                "Payment {PaymentId} transaction found on-chain after timeout check. Confirmations: {Confirmations}",
                paymentId, status.Confirmations);

            // Update status to confirming if it was still submitted
            if (payment.Status == P2PPaymentStatus.Submitted)
            {
                await conn.ExecuteAsync(
                    @"UPDATE p2p_payments
                      SET status = @Status, confirmations = @Confirmations, updated_at = @UpdatedAt
                      WHERE id = @PaymentId",
                    new
                    {
                        Status = P2PPaymentStatus.Confirming,
                        Confirmations = status.Confirmations,
                        UpdatedAt = DateTime.UtcNow,
                        PaymentId = paymentId
                    });
            }
            return;
        }

        // Transaction not found on-chain after timeout - mark as timeout
        _logger.LogWarning(
            "Payment {PaymentId} timed out: transaction {Signature} not found on-chain. Marking as timeout.",
            paymentId, payment.SolanaSignature);

        await conn.ExecuteAsync(
            @"UPDATE p2p_payments
              SET status = @Status,
                  error_message = @ErrorMessage,
                  updated_at = @UpdatedAt
              WHERE id = @PaymentId",
            new
            {
                Status = P2PPaymentStatus.Timeout,
                ErrorMessage = "Transaction was submitted but never appeared on-chain. The transaction may have been dropped from the mempool. You can retry the payment.",
                UpdatedAt = DateTime.UtcNow,
                PaymentId = paymentId
            });

        // TODO: Consider sending a notification to the user about the timeout
    }
}
