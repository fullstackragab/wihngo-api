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
public class SupportIntentService : ISupportIntentService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IWalletService _walletService;
    private readonly ISolanaTransactionService _solanaService;
    private readonly ILedgerService _ledgerService;
    private readonly ISupportConfirmationEmailService _supportConfirmationEmailService;
    private readonly P2PPaymentConfiguration _config;
    private readonly ILogger<SupportIntentService> _logger;

    // Minimum SOL required for transaction fees
    private const decimal MinSolForGas = 0.001m;

    public SupportIntentService(
        IDbConnectionFactory dbFactory,
        IWalletService walletService,
        ISolanaTransactionService solanaService,
        ILedgerService ledgerService,
        ISupportConfirmationEmailService supportConfirmationEmailService,
        IOptions<P2PPaymentConfiguration> config,
        ILogger<SupportIntentService> logger)
    {
        _dbFactory = dbFactory;
        _walletService = walletService;
        _solanaService = solanaService;
        _ledgerService = ledgerService;
        _supportConfirmationEmailService = supportConfirmationEmailService;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Preflight check before creating a support intent.
    /// </summary>
    public async Task<SupportPreflightResponse> PreflightAsync(
        Guid userId,
        SupportPreflightRequest request)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // 1. Validate Wihngo support minimum
        if (request.WihngoSupportAmount > 0 && request.WihngoSupportAmount < _config.MinWihngoSupportUsdc)
        {
            return new SupportPreflightResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.WihngoSupportTooLow,
                Message = $"Minimum Wihngo support is ${_config.MinWihngoSupportUsdc:F2} USDC. Set to $0 to skip."
            };
        }

        // 2. Validate bird
        var bird = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT bird_id, owner_id, name, image_url, support_enabled FROM birds WHERE bird_id = @BirdId",
            new { request.BirdId });

        if (bird == null)
        {
            return new SupportPreflightResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.BirdNotFound,
                Message = "Bird not found"
            };
        }

        // Check if bird is accepting support
        if (bird.support_enabled == false)
        {
            return new SupportPreflightResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.SupportNotEnabled,
                Message = "This bird is not accepting support at the moment",
                Bird = new BirdSupportInfo
                {
                    BirdId = request.BirdId,
                    Name = bird.name,
                    ImageUrl = bird.image_url
                }
            };
        }

        Guid recipientUserId = bird.owner_id;

        // Cannot support own bird
        if (recipientUserId == userId)
        {
            return new SupportPreflightResponse
            {
                CanSupport = false,
                ErrorCode = SupportIntentErrorCodes.CannotSupportOwnBird,
                Message = "You cannot support your own bird"
            };
        }

        // 3. Get recipient info
        var recipient = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE user_id = @UserId",
            new { UserId = recipientUserId });

        Console.WriteLine($"[Preflight] Looking up wallet for recipient: {recipientUserId}");
        var recipientWallet = await _walletService.GetPrimaryWalletAsync(recipientUserId);
        Console.WriteLine($"[Preflight] Recipient wallet found: {recipientWallet != null}");
        if (recipientWallet != null)
        {
            Console.WriteLine($"[Preflight] Recipient wallet pubkey: {recipientWallet.PublicKey}");
        }

        if (recipientWallet == null)
        {
            return new SupportPreflightResponse
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

        // 4. Check user's wallet
        var userWallet = await _walletService.GetPrimaryWalletAsync(userId);
        if (userWallet == null)
        {
            return new SupportPreflightResponse
            {
                CanSupport = false,
                HasWallet = false,
                ErrorCode = SupportIntentErrorCodes.WalletRequired,
                Message = "Connect your Phantom wallet to support this bird",
                Bird = new BirdSupportInfo
                {
                    BirdId = request.BirdId,
                    Name = bird.name
                },
                Recipient = new RecipientInfo
                {
                    UserId = recipientUserId,
                    Name = recipient?.Name ?? "Unknown",
                    WalletAddress = recipientWallet.PublicKey
                },
                UsdcMintAddress = _config.UsdcMintAddress,
                WihngoWalletAddress = _config.WihngoTreasuryWallet
            };
        }

        // 5. Get balances from Solana
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

        // 6. Calculate requirements (bird-first model: NO platform fee deduction)
        var totalUsdc = request.BirdAmount + request.WihngoSupportAmount;
        var solRequired = MinSolForGas;

        // 7. Check balances
        var hasEnoughUsdc = usdcBalance >= totalUsdc;
        var hasEnoughSol = solBalance >= solRequired;

        string? errorCode = null;
        string? message = null;

        if (!hasEnoughUsdc)
        {
            errorCode = SupportIntentErrorCodes.InsufficientUsdc;
            message = $"Insufficient USDC. You have {usdcBalance:F2} USDC but need {totalUsdc:F2} USDC";
        }
        else if (!hasEnoughSol)
        {
            errorCode = SupportIntentErrorCodes.InsufficientSol;
            message = $"Insufficient SOL for gas. You have {solBalance:F6} SOL but need {solRequired:F6} SOL";
        }

        Console.WriteLine($"[Preflight] Building SUCCESS response with WalletAddress: {recipientWallet.PublicKey}");

        return new SupportPreflightResponse
        {
            CanSupport = hasEnoughUsdc && hasEnoughSol,
            HasWallet = true,
            UsdcBalance = usdcBalance,
            SolBalance = solBalance,
            BirdAmount = request.BirdAmount,
            WihngoSupportAmount = request.WihngoSupportAmount,
            TotalUsdcRequired = totalUsdc,
            SolRequired = solRequired,
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
            },
            UsdcMintAddress = _config.UsdcMintAddress,
            WihngoWalletAddress = _config.WihngoTreasuryWallet
        };
    }

    /// <summary>
    /// Alias for PreflightAsync - checks if user can support a bird.
    /// </summary>
    public async Task<CheckSupportBalanceResponse> CheckSupportBalanceAsync(
        Guid userId,
        CheckSupportBalanceRequest request)
    {
        var result = await PreflightAsync(userId, request);
        // CheckSupportBalanceResponse inherits from SupportPreflightResponse
        return new CheckSupportBalanceResponse
        {
            CanSupport = result.CanSupport,
            HasWallet = result.HasWallet,
            UsdcBalance = result.UsdcBalance,
            SolBalance = result.SolBalance,
            BirdAmount = result.BirdAmount,
            WihngoSupportAmount = result.WihngoSupportAmount,
            TotalUsdcRequired = result.TotalUsdcRequired,
            SolRequired = result.SolRequired,
            ErrorCode = result.ErrorCode,
            Message = result.Message,
            Bird = result.Bird,
            Recipient = result.Recipient,
            UsdcMintAddress = result.UsdcMintAddress,
            WihngoWalletAddress = result.WihngoWalletAddress
        };
    }

    /// <inheritdoc />
    public async Task<(SupportIntentResponse? Response, ValidationErrorResponse? Error)> CreateSupportIntentAsync(
        Guid supporterUserId,
        CreateSupportIntentRequest request)
    {
        // Validate Wihngo support minimum
        if (request.WihngoSupportAmount > 0 && request.WihngoSupportAmount < _config.MinWihngoSupportUsdc)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.WihngoSupportTooLow,
                Message = $"Minimum Wihngo support is ${_config.MinWihngoSupportUsdc:F2} USDC. Set to $0 to skip.",
                FieldErrors = new Dictionary<string, string[]>
                {
                    ["wihngoSupportAmount"] = new[] { $"Minimum is ${_config.MinWihngoSupportUsdc:F2} or $0" }
                }
            });
        }

        // First do a preflight check
        var preflight = await PreflightAsync(supporterUserId, new SupportPreflightRequest
        {
            BirdId = request.BirdId,
            BirdAmount = request.BirdAmount,
            WihngoSupportAmount = request.WihngoSupportAmount
        });

        if (!preflight.CanSupport)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = preflight.ErrorCode ?? SupportIntentErrorCodes.InternalError,
                Message = preflight.Message ?? "Cannot create support intent",
                FieldErrors = preflight.ErrorCode == SupportIntentErrorCodes.WalletRequired
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

        // Calculate total (bird-first model: NO deductions)
        var totalAmount = request.BirdAmount + request.WihngoSupportAmount;

        // Build the Solana transaction with two transfers:
        // 1. Bird amount -> bird owner wallet (100%)
        // 2. Wihngo support -> Wihngo treasury wallet (if > 0)
        string? serializedTx = null;
        try
        {
            serializedTx = await _solanaService.BuildUsdcTransferTransactionAsync(
                supporterWallet!.PublicKey,
                recipientWallet!.PublicKey,
                request.BirdAmount,
                null, // No gas sponsor
                false, // Assume ATA exists
                request.WihngoSupportAmount > 0 ? _config.WihngoTreasuryWallet : null,
                request.WihngoSupportAmount);
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
            BirdAmount = request.BirdAmount,
            WihngoSupportAmount = request.WihngoSupportAmount,
            TotalAmount = totalAmount,
            Currency = request.Currency,
            Status = SupportIntentStatus.AwaitingPayment,
            PaymentMethod = SupportPaymentMethod.Wallet,
            SenderWalletPubkey = supporterWallet!.PublicKey,
            RecipientWalletPubkey = recipientWallet!.PublicKey,
            WihngoWalletPubkey = request.WihngoSupportAmount > 0 ? _config.WihngoTreasuryWallet : null,
            SerializedTransaction = serializedTx,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.PaymentExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Insert intent (support_amount = bird_amount for compatibility with legacy column)
        await conn.ExecuteAsync(
            @"INSERT INTO support_intents
              (id, supporter_user_id, bird_id, recipient_user_id, support_amount, bird_amount, wihngo_support_amount,
               total_amount, currency, status, payment_method, sender_wallet_pubkey,
               recipient_wallet_pubkey, wihngo_wallet_pubkey, serialized_transaction, expires_at, created_at, updated_at)
              VALUES (@Id, @SupporterUserId, @BirdId, @RecipientUserId, @BirdAmount, @BirdAmount, @WihngoSupportAmount,
               @TotalAmount, @Currency, @Status, @PaymentMethod, @SenderWalletPubkey,
               @RecipientWalletPubkey, @WihngoWalletPubkey, @SerializedTransaction, @ExpiresAt, @CreatedAt, @UpdatedAt)",
            intent);

        _logger.LogInformation(
            "Support intent created: {IntentId} for bird {BirdId}, BirdAmount: {BirdAmount} USDC, WihngoSupport: {WihngoSupport} USDC (bird money is sacred)",
            intent.Id, request.BirdId, request.BirdAmount, request.WihngoSupportAmount);

        return (new SupportIntentResponse
        {
            IntentId = intent.Id,
            BirdId = request.BirdId,
            BirdName = birdName,
            RecipientUserId = recipientUserId,
            RecipientName = recipient?.Name ?? "Unknown",
            BirdWalletAddress = recipientWallet!.PublicKey,
            WihngoWalletAddress = request.WihngoSupportAmount > 0 ? _config.WihngoTreasuryWallet : null,
            BirdAmount = request.BirdAmount,
            WihngoSupportAmount = request.WihngoSupportAmount,
            TotalAmount = totalAmount,
            Currency = request.Currency,
            UsdcMintAddress = _config.UsdcMintAddress,
            Status = intent.Status,
            SerializedTransaction = serializedTx,
            ExpiresAt = intent.ExpiresAt,
            CreatedAt = intent.CreatedAt
        }, null);
    }

    /// <inheritdoc />
    public async Task<(SupportWihngoResponse? Response, ValidationErrorResponse? Error)> CreateWihngoSupportIntentAsync(
        Guid userId,
        SupportWihngoRequest request)
    {
        if (request.Amount < _config.MinWihngoSupportUsdc)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.WihngoSupportTooLow,
                Message = $"Minimum Wihngo support is ${_config.MinWihngoSupportUsdc:F2} USDC"
            });
        }

        var userWallet = await _walletService.GetPrimaryWalletAsync(userId);
        if (userWallet == null)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.WalletRequired,
                Message = "Connect your Phantom wallet to support Wihngo"
            });
        }

        // Check balance
        decimal usdcBalance = await _solanaService.GetUsdcBalanceAsync(userWallet.PublicKey);
        if (usdcBalance < request.Amount)
        {
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InsufficientUsdc,
                Message = $"Insufficient USDC. You have {usdcBalance:F2} USDC but need {request.Amount:F2} USDC"
            });
        }

        // Build transaction
        string? serializedTx = null;
        try
        {
            serializedTx = await _solanaService.BuildUsdcTransferTransactionAsync(
                userWallet.PublicKey,
                _config.WihngoTreasuryWallet,
                request.Amount,
                null,
                false,
                null,
                0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build Wihngo support transaction");
            return (null, new ValidationErrorResponse
            {
                ErrorCode = SupportIntentErrorCodes.InternalError,
                Message = "Failed to build transaction. Please try again."
            });
        }

        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        // Create a special support intent (no bird)
        var intent = new SupportIntent
        {
            Id = Guid.NewGuid(),
            SupporterUserId = userId,
            BirdId = Guid.Empty, // No bird
            RecipientUserId = Guid.Empty, // Wihngo
            BirdAmount = 0,
            WihngoSupportAmount = request.Amount,
            TotalAmount = request.Amount,
            Currency = "USDC",
            Status = SupportIntentStatus.AwaitingPayment,
            PaymentMethod = SupportPaymentMethod.Wallet,
            SenderWalletPubkey = userWallet.PublicKey,
            RecipientWalletPubkey = null,
            WihngoWalletPubkey = _config.WihngoTreasuryWallet,
            SerializedTransaction = serializedTx,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_config.PaymentExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await conn.ExecuteAsync(
            @"INSERT INTO support_intents
              (id, supporter_user_id, bird_id, recipient_user_id, support_amount, bird_amount, wihngo_support_amount,
               total_amount, currency, status, payment_method, sender_wallet_pubkey,
               recipient_wallet_pubkey, wihngo_wallet_pubkey, serialized_transaction, expires_at, created_at, updated_at)
              VALUES (@Id, @SupporterUserId, @BirdId, @RecipientUserId, @BirdAmount, @BirdAmount, @WihngoSupportAmount,
               @TotalAmount, @Currency, @Status, @PaymentMethod, @SenderWalletPubkey,
               @RecipientWalletPubkey, @WihngoWalletPubkey, @SerializedTransaction, @ExpiresAt, @CreatedAt, @UpdatedAt)",
            intent);

        _logger.LogInformation(
            "Wihngo support intent created: {IntentId}, Amount: {Amount} USDC",
            intent.Id, request.Amount);

        return (new SupportWihngoResponse
        {
            IntentId = intent.Id,
            Amount = request.Amount,
            WihngoWalletAddress = _config.WihngoTreasuryWallet,
            UsdcMintAddress = _config.UsdcMintAddress,
            SerializedTransaction = serializedTx,
            Status = intent.Status,
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

            // Update bird support count (if this is a bird support, not just Wihngo support)
            if (intent.BirdId != Guid.Empty)
            {
                await conn.ExecuteAsync(
                    "UPDATE birds SET supported_count = supported_count + 1 WHERE bird_id = @BirdId",
                    new { intent.BirdId });
            }

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
                "Support intent {IntentId} completed: BirdAmount={BirdAmount} USDC (100% to owner), WihngoSupport={WihngoSupport} USDC",
                intentId, intent.BirdAmount, intent.WihngoSupportAmount);

            // Send support confirmation email to supporter
            _ = Task.Run(async () =>
            {
                try
                {
                    using var emailConn = await _dbFactory.CreateOpenConnectionAsync();

                    var supporter = await emailConn.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM users WHERE user_id = @UserId",
                        new { UserId = intent.SupporterUserId });

                    var bird = await emailConn.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT name, image_url FROM birds WHERE bird_id = @BirdId",
                        new { intent.BirdId });

                    if (supporter?.Email != null && bird != null)
                    {
                        await _supportConfirmationEmailService.SendSupportConfirmationAsync(new SupportConfirmationDto
                        {
                            SupporterEmail = supporter.Email,
                            SupporterName = supporter.Name ?? "Supporter",
                            BirdName = bird.name ?? "Bird",
                            BirdImageUrl = bird.image_url,
                            BirdAmount = intent.BirdAmount,
                            WihngoAmount = intent.WihngoSupportAmount > 0 ? intent.WihngoSupportAmount : null,
                            TotalAmount = intent.TotalAmount,
                            TransactionDateTime = DateTime.UtcNow,
                            TransactionHash = intent.SolanaSignature ?? intentId.ToString()
                        });

                        _logger.LogInformation("Support confirmation email sent to {Email} for intent {IntentId}", supporter.Email, intentId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send support confirmation email for intent {IntentId}", intentId);
                }
            });

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

        // Debit supporter (total amount)
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

        // Credit recipient (bird amount - 100% goes to bird owner)
        if (intent.BirdId != Guid.Empty && intent.BirdAmount > 0)
        {
            var recipientBalance = await conn.ExecuteScalarAsync<decimal?>(
                "SELECT balance_after FROM ledger_entries WHERE user_id = @UserId ORDER BY created_at DESC LIMIT 1",
                new { UserId = intent.RecipientUserId }) ?? 0m;

            await conn.ExecuteAsync(
                @"INSERT INTO ledger_entries (id, user_id, amount_usdc, entry_type, reference_type, reference_id, balance_after, description, created_at)
                  VALUES (@Id, @UserId, @Amount, @EntryType, @RefType, @RefId, @BalanceAfter, @Description, @CreatedAt)",
                new
                {
                    Id = Guid.NewGuid(),
                    UserId = intent.RecipientUserId,
                    Amount = intent.BirdAmount,
                    EntryType = "SupportReceived",
                    RefType = "SupportIntent",
                    RefId = intent.Id,
                    BalanceAfter = recipientBalance + intent.BirdAmount,
                    Description = "Bird support received (100% - on-chain)",
                    CreatedAt = now
                });
        }

        // Log Wihngo support (if any)
        if (intent.WihngoSupportAmount > 0)
        {
            _logger.LogInformation(
                "Wihngo support received: {Amount} USDC from support intent {IntentId} (optional, additive)",
                intent.WihngoSupportAmount, intent.Id);
        }
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

        string birdName = "Unknown";
        if (intent.BirdId != Guid.Empty)
        {
            var bird = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT name FROM birds WHERE bird_id = @BirdId",
                new { intent.BirdId });
            birdName = bird?.name ?? "Unknown";
        }

        var recipient = intent.RecipientUserId != Guid.Empty
            ? await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE user_id = @UserId",
                new { UserId = intent.RecipientUserId })
            : null;

        return new SupportIntentResponse
        {
            IntentId = intent.Id,
            BirdId = intent.BirdId,
            BirdName = birdName,
            RecipientUserId = intent.RecipientUserId,
            RecipientName = recipient?.Name ?? "Wihngo",
            BirdWalletAddress = intent.RecipientWalletPubkey,
            WihngoWalletAddress = intent.WihngoWalletPubkey,
            BirdAmount = intent.BirdAmount,
            WihngoSupportAmount = intent.WihngoSupportAmount,
            TotalAmount = intent.TotalAmount,
            Currency = intent.Currency,
            UsdcMintAddress = _config.UsdcMintAddress,
            Status = intent.Status,
            SerializedTransaction = intent.SerializedTransaction,
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
