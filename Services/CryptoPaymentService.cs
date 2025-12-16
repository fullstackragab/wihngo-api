using System;
using Dapper;
using Npgsql;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Entities;
using Wihngo.Models;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

public class CryptoPaymentService : ICryptoPaymentService
{
    private readonly AppDbContext _context;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IBlockchainService _blockchainService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CryptoPaymentService> _logger;
    private readonly IHdWalletService? _hdWalletService;
    private readonly IInvoiceEmailService? _invoiceEmailService;
    private readonly IInvoicePdfService? _invoicePdfService;

    public CryptoPaymentService(
        AppDbContext context,
        IDbConnectionFactory dbFactory,
        IBlockchainService blockchainService,
        IConfiguration configuration,
        ILogger<CryptoPaymentService> logger,
        IHdWalletService? hdWalletService = null,
        IInvoiceEmailService? invoiceEmailService = null,
        IInvoicePdfService? invoicePdfService = null)
    {
        _context = context;
        _dbFactory = dbFactory;
        _blockchainService = blockchainService;
        _configuration = configuration;
        _logger = logger;
        _hdWalletService = hdWalletService;
        _invoiceEmailService = invoiceEmailService;
        _invoicePdfService = invoicePdfService;
    }

    // New helper: allocate a unique index from Postgres sequence (only for Solana)
    private async Task<long> AllocateHdIndexAsync(string network)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();
        
        // Only Solana is supported
        if (!network.Equals("solana", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Network '{network}' is not supported. Only Solana network is accepted.");
        }

        string sequenceName = "hd_address_index_seq_solana";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT nextval('{sequenceName}')";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<PaymentResponseDto> CreatePaymentRequestAsync(Guid userId, CreatePaymentRequestDto dto)
    {
        // Validate that only Solana network is used
        if (!dto.Network.Equals("solana", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only Solana network is supported for crypto payments.");
        }

        // Validate that only USDC or EURC is used
        var supportedCurrencies = new[] { "USDC", "EURC" };
        if (!supportedCurrencies.Contains(dto.Currency.ToUpper()))
        {
            throw new InvalidOperationException("Only USDC and EURC are supported currencies on Solana network.");
        }

        // Validate minimum amount
        var minAmount = _configuration.GetValue<decimal>("PaymentSettings:MinPaymentAmountUsd", 5m);
        if (dto.AmountUsd < minAmount)
        {
            throw new InvalidOperationException($"Minimum payment amount is ${minAmount}");
        }

        // Get exchange rate using raw SQL
        CryptoExchangeRate? rate;
        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            rate = await conn.QueryFirstOrDefaultAsync<CryptoExchangeRate>(
                "SELECT * FROM crypto_exchange_rates WHERE currency = @Currency",
                new { Currency = dto.Currency.ToUpper() });
        }

        if (rate == null)
        {
            throw new InvalidOperationException($"Exchange rate not available for {dto.Currency}");
        }

        // Calculate crypto amount
        var amountCrypto = dto.AmountUsd / rate.UsdRate;

        // Get platform wallet
        var wallet = await GetPlatformWalletAsync(dto.Currency, dto.Network);
        if (wallet == null)
        {
            throw new InvalidOperationException($"No wallet configured for {dto.Currency} on {dto.Network}");
        }

        int? addressIndex = null;
        
        // If HD mnemonic configured and service available, derive a unique address per payment
        var hdMnemonic = _configuration["PlatformWallets:HdMnemonic"];
        if (!string.IsNullOrEmpty(hdMnemonic) && _hdWalletService != null)
        {
            try
            {
                // Allocate index atomically from Postgres sequence to avoid collisions
                var index = (int)await AllocateHdIndexAsync(dto.Network);
                addressIndex = index;

                var derived = _hdWalletService.DeriveAddress(hdMnemonic, dto.Network, index);
                if (!string.IsNullOrEmpty(derived))
                {
                    _logger.LogInformation(
                        "[HD Wallet] Allocated index {Index} for {Network}, derived address: {Address}, path: m/44'/60'/0'/0/{Index}",
                        index, dto.Network, derived, index);
                    
                    // Use derived address as unique wallet for this payment
                    wallet = new PlatformWallet
                    {
                        Id = Guid.NewGuid(),
                        Currency = wallet.Currency,
                        Network = wallet.Network,
                        Address = derived,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        DerivationPath = $"m/44'/60'/0'/0/{index}"
                    };
                }
                else
                {
                    _logger.LogWarning("[HD Wallet] Failed to derive address for index {Index}, falling back to static wallet", index);
                    addressIndex = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HD Wallet] Failed to derive per-payment address; falling back to platform wallet");
                addressIndex = null;
            }
        }
        else
        {
            _logger.LogInformation("[HD Wallet] HD mnemonic not configured, using static platform wallet");
        }

        // Get required confirmations
        var requiredConfirmations = GetRequiredConfirmations(dto.Network);

        // Generate payment URI
        var paymentUri = GeneratePaymentUri(dto.Currency, dto.Network, wallet.Address, amountCrypto);
        var qrCodeData = paymentUri;

        // Calculate expiration - 30 minutes from now
        // Ensure DateTime is set to UTC with DateTimeKind.Utc
        var expiresAt = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(30), DateTimeKind.Utc);

        // Create payment request using Dapper
        var paymentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        CryptoPaymentRequest payment;
        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            var sql = @"
                INSERT INTO crypto_payment_requests (
                    id, user_id, bird_id, amount_usd, amount_crypto, currency, network,
                    exchange_rate, wallet_address, address_index, qr_code_data, payment_uri,
                    confirmations, required_confirmations, status, purpose, plan, expires_at, created_at, updated_at
                ) VALUES (
                    @Id, @UserId, @BirdId, @AmountUsd, @AmountCrypto, @Currency, @Network,
                    @ExchangeRate, @WalletAddress, @AddressIndex, @QrCodeData, @PaymentUri,
                    @Confirmations, @RequiredConfirmations, @Status, @Purpose, @Plan, @ExpiresAt, @CreatedAt, @UpdatedAt
                ) RETURNING *";

            payment = await conn.QuerySingleAsync<CryptoPaymentRequest>(sql, new
            {
                Id = paymentId,
                UserId = userId,
                BirdId = dto.BirdId,
                AmountUsd = dto.AmountUsd,
                AmountCrypto = amountCrypto,
                Currency = dto.Currency.ToUpper(),
                Network = dto.Network.ToLower(),
                ExchangeRate = rate.UsdRate,
                WalletAddress = wallet.Address,
                AddressIndex = addressIndex,
                QrCodeData = qrCodeData,
                PaymentUri = paymentUri,
                Confirmations = 0,
                RequiredConfirmations = requiredConfirmations,
                Status = "pending",
                Purpose = dto.Purpose,
                Plan = dto.Plan,
                ExpiresAt = expiresAt,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        _logger.LogInformation(
            "[Payment] Created payment request {PaymentId} for user {UserId} - Amount: {Amount} {Currency}, Wallet: {Wallet}, HD Index: {Index}",
            payment.Id, userId, amountCrypto, dto.Currency, wallet.Address, addressIndex?.ToString() ?? "N/A");

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetPaymentRequestAsync(Guid paymentId, Guid userId)
    {
        CryptoPaymentRequest? payment;
        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            payment = await conn.QueryFirstOrDefaultAsync<CryptoPaymentRequest>(
                "SELECT * FROM crypto_payment_requests WHERE id = @PaymentId AND user_id = @UserId",
                new { PaymentId = paymentId, UserId = userId });
        }

        if (payment == null)
        {
            return null;
        }

        // Check if expired
        if (payment.Status == "pending" && DateTime.UtcNow > payment.ExpiresAt)
        {
            payment.Status = "expired";
            payment.UpdatedAt = DateTime.UtcNow;
            using (var conn = await _dbFactory.CreateOpenConnectionAsync())
            {
                await conn.ExecuteAsync(
                    "UPDATE crypto_payment_requests SET status = @Status, updated_at = @UpdatedAt WHERE id = @Id",
                    new { Status = payment.Status, UpdatedAt = payment.UpdatedAt, Id = payment.Id });
            }
        }

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto> VerifyPaymentAsync(Guid paymentId, Guid userId, VerifyPaymentDto dto)
    {
        CryptoPaymentRequest? payment;
        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            payment = await conn.QueryFirstOrDefaultAsync<CryptoPaymentRequest>(
                "SELECT * FROM crypto_payment_requests WHERE id = @PaymentId AND user_id = @UserId",
                new { PaymentId = paymentId, UserId = userId });
        }

        if (payment == null)
        {
            throw new InvalidOperationException("Payment not found");
        }

        if (payment.Status != "pending" && payment.Status != "confirming")
        {
            throw new InvalidOperationException($"Payment already {payment.Status}");
        }

        _logger.LogInformation($"[VerifyPayment] Verifying payment {paymentId} with tx hash {dto.TransactionHash}");

        // Verify transaction on blockchain
        var txInfo = await _blockchainService.VerifyTransactionAsync(
            dto.TransactionHash,
            payment.Currency,
            payment.Network
        );

        if (txInfo == null)
        {
            _logger.LogWarning($"[VerifyPayment] Transaction {dto.TransactionHash} not found on blockchain");
            throw new InvalidOperationException("Transaction not found on blockchain");
        }

        // Verify amount (allow 1% tolerance)
        var minAmount = payment.AmountCrypto * 0.99m;
        if (txInfo.Amount < minAmount && txInfo.Amount > 0)
        {
            _logger.LogWarning($"[VerifyPayment] Amount mismatch. Expected: {payment.AmountCrypto}, Received: {txInfo.Amount}");
            throw new InvalidOperationException(
                $"Incorrect amount. Expected {payment.AmountCrypto}, received {txInfo.Amount}");
        }

        // Verify recipient address
        if (!txInfo.ToAddress.Equals(payment.WalletAddress, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning($"[VerifyPayment] Address mismatch. Expected: {payment.WalletAddress}, Received: {txInfo.ToAddress}");
            throw new InvalidOperationException("Incorrect recipient address");
        }

        var previousStatus = payment.Status;

        // Update payment
        payment.TransactionHash = dto.TransactionHash;
        payment.UserWalletAddress = dto.UserWalletAddress ?? txInfo.FromAddress;
        payment.Confirmations = txInfo.Confirmations;
        payment.Status = txInfo.Confirmations >= payment.RequiredConfirmations ? "confirmed" : "confirming";
        payment.UpdatedAt = DateTime.UtcNow;

        if (payment.Status == "confirmed" && payment.ConfirmedAt == null)
        {
            payment.ConfirmedAt = DateTime.UtcNow;
        }

        // Update payment and create transaction record using Dapper
        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            // Update payment request
            await conn.ExecuteAsync(@"
                UPDATE crypto_payment_requests SET
                    transaction_hash = @TransactionHash,
                    user_wallet_address = @UserWalletAddress,
                    confirmations = @Confirmations,
                    status = @Status,
                    confirmed_at = @ConfirmedAt,
                    updated_at = @UpdatedAt
                WHERE id = @Id",
                new
                {
                    TransactionHash = payment.TransactionHash,
                    UserWalletAddress = payment.UserWalletAddress,
                    Confirmations = payment.Confirmations,
                    Status = payment.Status,
                    ConfirmedAt = payment.ConfirmedAt,
                    UpdatedAt = payment.UpdatedAt,
                    Id = payment.Id
                });

            // Create transaction record
            await conn.ExecuteAsync(@"
                INSERT INTO crypto_transactions (
                    payment_request_id, transaction_hash, from_address, to_address, amount,
                    currency, network, confirmations, block_number, block_hash, fee, status,
                    detected_at
                ) VALUES (
                    @PaymentRequestId, @TransactionHash, @FromAddress, @ToAddress, @Amount,
                    @Currency, @Network, @Confirmations, @BlockNumber, @BlockHash, @Fee, @Status,
                    @DetectedAt
                )",
                new
                {
                    PaymentRequestId = payment.Id,
                    TransactionHash = dto.TransactionHash,
                    FromAddress = txInfo.FromAddress,
                    ToAddress = txInfo.ToAddress,
                    Amount = txInfo.Amount,
                    Currency = payment.Currency,
                    Network = payment.Network,
                    Confirmations = txInfo.Confirmations,
                    BlockNumber = txInfo.BlockNumber,
                    BlockHash = txInfo.BlockHash,
                    Fee = txInfo.Fee,
                    Status = txInfo.Confirmations >= payment.RequiredConfirmations ? "confirmed" : "pending",
                    DetectedAt = DateTime.UtcNow
                });
        }

        _logger.LogInformation($"[VerifyPayment] Payment {payment.Id} verified - Status: {previousStatus} -> {payment.Status}, Confirmations: {txInfo.Confirmations}/{payment.RequiredConfirmations}");

        // Complete payment if confirmed
        if (payment.Status == "confirmed")
        {
            _logger.LogInformation($"[VerifyPayment] Payment {payment.Id} has required confirmations, completing now");
            await CompletePaymentAsync(payment);

            // Reload payment to get updated status
            using (var conn = await _dbFactory.CreateOpenConnectionAsync())
            {
                payment = await conn.QueryFirstOrDefaultAsync<CryptoPaymentRequest>(
                    "SELECT * FROM crypto_payment_requests WHERE id = @Id",
                    new { Id = payment.Id }) ?? payment;
            }

            Console.WriteLine($"? Payment {payment.Id} completed via VerifyPayment endpoint");
        }

        return MapToDto(payment);
    }

    public async Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(Guid userId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var payments = await _context.CryptoPaymentRequests
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return payments.Select(MapToDto).ToList();
    }

    public async Task<PlatformWallet?> GetPlatformWalletAsync(string currency, string network)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();
        return await conn.QueryFirstOrDefaultAsync<PlatformWallet>(
            @"SELECT * FROM platform_wallets
              WHERE currency = @Currency
              AND network = @Network
              AND is_active = TRUE
              ORDER BY created_at DESC
              LIMIT 1",
            new { Currency = currency.ToUpper(), Network = network.ToLower() });
    }

    public async Task CompletePaymentAsync(CryptoPaymentRequest payment)
    {
        try
        {
            var previousStatus = payment.Status;

            _logger.LogInformation($"[CompletePayment] Starting completion for payment {payment.Id} (Current status: {previousStatus})");

            payment.Status = "completed";
            payment.CompletedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            if (payment.Purpose == "premium_subscription" && payment.BirdId != null)
            {
                _logger.LogInformation($"[CompletePayment] Activating premium subscription for bird {payment.BirdId}");
                await ActivatePremiumSubscriptionAsync(payment);
            }

            // Update payment status using Dapper
            using (var conn = await _dbFactory.CreateOpenConnectionAsync())
            {
                await conn.ExecuteAsync(@"
                    UPDATE crypto_payment_requests SET
                        status = @Status,
                        completed_at = @CompletedAt,
                        updated_at = @UpdatedAt
                    WHERE id = @Id",
                    new
                    {
                        Status = payment.Status,
                        CompletedAt = payment.CompletedAt,
                        UpdatedAt = payment.UpdatedAt,
                        Id = payment.Id
                    });
            }

            Console.WriteLine($"??? PAYMENT COMPLETED SUCCESSFULLY ???");
            Console.WriteLine($"Payment ID: {payment.Id}");
            Console.WriteLine($"User ID: {payment.UserId}");
            Console.WriteLine($"Amount: {payment.AmountCrypto} {payment.Currency} (${payment.AmountUsd})");
            Console.WriteLine($"Purpose: {payment.Purpose}");
            Console.WriteLine($"Plan: {payment.Plan}");
            Console.WriteLine($"Status: {previousStatus} -> {payment.Status}");
            Console.WriteLine($"Completed At: {payment.CompletedAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Transaction Hash: {payment.TransactionHash}");
            Console.WriteLine($"===========================================");

            _logger.LogInformation($"[CompletePayment] ? Payment {payment.Id} completed successfully - Status changed from '{previousStatus}' to '{payment.Status}'");

            // Send payment confirmation email
            await SendPaymentConfirmationEmailAsync(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CompletePayment] ? Failed to complete payment {payment.Id}");
            Console.WriteLine($"? ERROR: Failed to complete payment {payment.Id}: {ex.Message}");

            payment.Status = "failed";
            using (var conn = await _dbFactory.CreateOpenConnectionAsync())
            {
                await conn.ExecuteAsync(
                    "UPDATE crypto_payment_requests SET status = @Status, updated_at = @UpdatedAt WHERE id = @Id",
                    new { Status = payment.Status, UpdatedAt = DateTime.UtcNow, Id = payment.Id });
            }
        }
    }

    private async Task ActivatePremiumSubscriptionAsync(CryptoPaymentRequest payment)
    {
        if (payment.BirdId == null) return;

        BirdPremiumSubscription? existingSub;
        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            existingSub = await conn.QueryFirstOrDefaultAsync<BirdPremiumSubscription>(
                "SELECT * FROM bird_premium_subscriptions WHERE bird_id = @BirdId AND status = 'active'",
                new { BirdId = payment.BirdId });

            if (existingSub != null)
            {
                // Extend existing subscription
                var extensionDays = payment.Plan?.ToLower() switch
                {
                    "monthly" => 30,
                    "yearly" => 365,
                    "lifetime" => 36500, // 100 years
                    _ => 30
                };

                DateTime newEndDate;
                if (existingSub.CurrentPeriodEnd < DateTime.UtcNow)
                {
                    newEndDate = DateTime.UtcNow.AddDays(extensionDays);
                }
                else
                {
                    newEndDate = existingSub.CurrentPeriodEnd.AddDays(extensionDays);
                }

                await conn.ExecuteAsync(@"
                    UPDATE bird_premium_subscriptions SET
                        current_period_end = @CurrentPeriodEnd,
                        updated_at = @UpdatedAt
                    WHERE id = @Id",
                    new
                    {
                        CurrentPeriodEnd = newEndDate,
                        UpdatedAt = DateTime.UtcNow,
                        Id = existingSub.Id
                    });
            }
            else
            {
                // Create new subscription
                var durationDays = payment.Plan?.ToLower() switch
                {
                    "monthly" => 30,
                    "yearly" => 365,
                    "lifetime" => 36500,
                    _ => 30
                };

                var now = DateTime.UtcNow;
                await conn.ExecuteAsync(@"
                    INSERT INTO bird_premium_subscriptions (
                        bird_id, owner_id, status, plan, provider, provider_subscription_id,
                        price_cents, duration_days, started_at, current_period_end, created_at, updated_at
                    ) VALUES (
                        @BirdId, @OwnerId, @Status, @Plan, @Provider, @ProviderSubscriptionId,
                        @PriceCents, @DurationDays, @StartedAt, @CurrentPeriodEnd, @CreatedAt, @UpdatedAt
                    )",
                    new
                    {
                        BirdId = payment.BirdId.Value,
                        OwnerId = payment.UserId,
                        Status = "active",
                        Plan = payment.Plan ?? "monthly",
                        Provider = "crypto",
                        ProviderSubscriptionId = payment.Id.ToString(),
                        PriceCents = (long)(payment.AmountUsd * 100),
                        DurationDays = durationDays,
                        StartedAt = now,
                        CurrentPeriodEnd = now.AddDays(durationDays),
                        CreatedAt = now,
                        UpdatedAt = now
                    });
            }
        }

        _logger.LogInformation($"Premium activated for bird {payment.BirdId}");
    }

    private int GetRequiredConfirmations(string network)
    {
        // Only Solana is supported
        if (network.Equals("solana", StringComparison.OrdinalIgnoreCase))
        {
            return 32;
        }
        
        throw new InvalidOperationException($"Network '{network}' is not supported. Only Solana network is accepted.");
    }

    private string GeneratePaymentUri(string currency, string network, string address, decimal amount)
    {
        // Only Solana is supported
        if (!network.Equals("solana", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Network '{network}' is not supported. Only Solana network is accepted.");
        }
        
        return $"solana:{address}";
    }

    private PaymentResponseDto MapToDto(CryptoPaymentRequest payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            BirdId = payment.BirdId,
            AmountUsd = payment.AmountUsd,
            AmountCrypto = payment.AmountCrypto,
            Currency = payment.Currency,
            Network = payment.Network,
            ExchangeRate = payment.ExchangeRate,
            WalletAddress = payment.WalletAddress,
            AddressIndex = payment.AddressIndex,
            UserWalletAddress = payment.UserWalletAddress,
            QrCodeData = payment.QrCodeData,
            PaymentUri = payment.PaymentUri,
            TransactionHash = payment.TransactionHash,
            Confirmations = payment.Confirmations,
            RequiredConfirmations = payment.RequiredConfirmations,
            Status = payment.Status,
            Purpose = payment.Purpose,
            Plan = payment.Plan,
            // Ensure DateTime is treated as UTC when serialized
            ExpiresAt = DateTime.SpecifyKind(payment.ExpiresAt, DateTimeKind.Utc),
            ConfirmedAt = payment.ConfirmedAt.HasValue ? DateTime.SpecifyKind(payment.ConfirmedAt.Value, DateTimeKind.Utc) : null,
            CompletedAt = payment.CompletedAt.HasValue ? DateTime.SpecifyKind(payment.CompletedAt.Value, DateTimeKind.Utc) : null,
            CreatedAt = DateTime.SpecifyKind(payment.CreatedAt, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(payment.UpdatedAt, DateTimeKind.Utc)
        };
    }

    public async Task<PaymentResponseDto?> CancelPaymentAsync(Guid paymentId, Guid userId)
    {
        CryptoPaymentRequest? payment;
        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            payment = await conn.QueryFirstOrDefaultAsync<CryptoPaymentRequest>(
                "SELECT * FROM crypto_payment_requests WHERE id = @PaymentId AND user_id = @UserId",
                new { PaymentId = paymentId, UserId = userId });
        }

        if (payment == null)
        {
            return null;
        }

        // Only allow cancellation of pending payments
        if (payment.Status != "pending")
        {
            throw new InvalidOperationException($"Cannot cancel payment with status '{payment.Status}'");
        }

        payment.Status = "cancelled";
        payment.UpdatedAt = DateTime.UtcNow;

        using (var conn = await _dbFactory.CreateOpenConnectionAsync())
        {
            await conn.ExecuteAsync(
                "UPDATE crypto_payment_requests SET status = @Status, updated_at = @UpdatedAt WHERE id = @Id",
                new { Status = payment.Status, UpdatedAt = payment.UpdatedAt, Id = payment.Id });
        }

        _logger.LogInformation($"Payment {payment.Id} cancelled by user {userId}");

        return MapToDto(payment);
    }

    private async Task SendPaymentConfirmationEmailAsync(CryptoPaymentRequest payment)
    {
        if (_invoiceEmailService == null)
        {
            _logger.LogWarning("[Email] Invoice email service not available, skipping email for payment {PaymentId}", payment.Id);
            return;
        }

        try
        {
            // Get user email from database
            string? userEmail = null;
            string? userName = null;

            using (var conn = await _dbFactory.CreateOpenConnectionAsync())
            {
                var user = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT email, name FROM users WHERE user_id = @UserId",
                    new { UserId = payment.UserId });

                if (user != null)
                {
                    userEmail = user.email;
                    userName = user.name;
                }
            }

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("[Email] User {UserId} has no email address, skipping payment confirmation email", payment.UserId);
                return;
            }

            // Generate invoice number
            var invoiceNumber = $"WIH-{DateTime.UtcNow:yyyyMM}-{payment.Id.ToString()[..8].ToUpper()}";

            // Generate PDF invoice if service is available
            byte[]? pdfBytes = null;
            if (_invoicePdfService != null)
            {
                try
                {
                    pdfBytes = await _invoicePdfService.GenerateCryptoPaymentReceiptAsync(
                        payment,
                        invoiceNumber,
                        userName,
                        userEmail);
                    _logger.LogInformation("[Invoice] Generated PDF invoice {InvoiceNumber} for payment {PaymentId}", invoiceNumber, payment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Invoice] Failed to generate PDF invoice for payment {PaymentId}, sending email without attachment", payment.Id);
                }
            }

            // Send invoice email with PDF attachment
            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                await _invoiceEmailService.SendInvoiceEmailAsync(
                    userEmail,
                    invoiceNumber,
                    pdfBytes,
                    userName);
                _logger.LogInformation("[Email] Invoice email with PDF sent to {Email} for payment {PaymentId}", userEmail, payment.Id);
            }
            else
            {
                // Fallback to simple confirmation email without PDF
                await _invoiceEmailService.SendPaymentConfirmationAsync(
                    userEmail,
                    invoiceNumber,
                    payment.AmountUsd,
                    payment.Currency);
                _logger.LogInformation("[Email] Payment confirmation email (without PDF) sent to {Email} for payment {PaymentId}", userEmail, payment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send payment confirmation email for payment {PaymentId}", payment.Id);
            // Don't throw - email failure shouldn't break the payment completion
        }
    }
}
