using Microsoft.EntityFrameworkCore;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Entities;
using Wihngo.Models;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

public class CryptoPaymentService : ICryptoPaymentService
{
    private readonly AppDbContext _context;
    private readonly IBlockchainService _blockchainService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CryptoPaymentService> _logger;

    public CryptoPaymentService(
        AppDbContext context,
        IBlockchainService blockchainService,
        IConfiguration configuration,
        ILogger<CryptoPaymentService> logger)
    {
        _context = context;
        _blockchainService = blockchainService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> CreatePaymentRequestAsync(Guid userId, CreatePaymentRequestDto dto)
    {
        // Validate minimum amount
        var minAmount = _configuration.GetValue<decimal>("PaymentSettings:MinPaymentAmountUsd", 5m);
        if (dto.AmountUsd < minAmount)
        {
            throw new InvalidOperationException($"Minimum payment amount is ${minAmount}");
        }

        // Get exchange rate
        var rate = await _context.CryptoExchangeRates
            .FirstOrDefaultAsync(r => r.Currency == dto.Currency.ToUpper());

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

        // Get required confirmations
        var requiredConfirmations = GetRequiredConfirmations(dto.Network);

        // Generate payment URI
        var paymentUri = GeneratePaymentUri(dto.Currency, dto.Network, wallet.Address, amountCrypto);
        var qrCodeData = paymentUri;

        // Calculate expiration - 30 minutes from now
        // Ensure DateTime is set to UTC with DateTimeKind.Utc
        var expiresAt = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(30), DateTimeKind.Utc);

        // Create payment request
        var payment = new CryptoPaymentRequest
        {
            UserId = userId,
            BirdId = dto.BirdId,
            AmountUsd = dto.AmountUsd,
            AmountCrypto = amountCrypto,
            Currency = dto.Currency.ToUpper(),
            Network = dto.Network.ToLower(),
            ExchangeRate = rate.UsdRate,
            WalletAddress = wallet.Address,
            QrCodeData = qrCodeData,
            PaymentUri = paymentUri,
            RequiredConfirmations = requiredConfirmations,
            Status = "pending",
            Purpose = dto.Purpose,
            Plan = dto.Plan,
            ExpiresAt = expiresAt
        };

        _context.CryptoPaymentRequests.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Payment request {payment.Id} created for user {userId}");

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetPaymentRequestAsync(Guid paymentId, Guid userId)
    {
        var payment = await _context.CryptoPaymentRequests
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

        if (payment == null)
        {
            return null;
        }

        // Check if expired
        if (payment.Status == "pending" && DateTime.UtcNow > payment.ExpiresAt)
        {
            payment.Status = "expired";
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto> VerifyPaymentAsync(Guid paymentId, Guid userId, VerifyPaymentDto dto)
    {
        var payment = await _context.CryptoPaymentRequests
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

        if (payment == null)
        {
            throw new InvalidOperationException("Payment not found");
        }

        if (payment.Status != "pending" && payment.Status != "confirming")
        {
            throw new InvalidOperationException($"Payment already {payment.Status}");
        }

        // Verify transaction on blockchain
        var txInfo = await _blockchainService.VerifyTransactionAsync(
            dto.TransactionHash,
            payment.Currency,
            payment.Network
        );

        if (txInfo == null)
        {
            throw new InvalidOperationException("Transaction not found on blockchain");
        }

        // Verify amount (allow 1% tolerance)
        var minAmount = payment.AmountCrypto * 0.99m;
        if (txInfo.Amount < minAmount)
        {
            throw new InvalidOperationException(
                $"Incorrect amount. Expected {payment.AmountCrypto}, received {txInfo.Amount}");
        }

        // Verify recipient address
        if (!txInfo.ToAddress.Equals(payment.WalletAddress, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Incorrect recipient address");
        }

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

        // Create transaction record
        var transaction = new CryptoTransaction
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
            Status = txInfo.Confirmations >= payment.RequiredConfirmations ? "confirmed" : "pending"
        };

        _context.CryptoTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Complete payment if confirmed
        if (payment.Status == "confirmed")
        {
            await CompletePaymentAsync(payment);
        }

        _logger.LogInformation($"Payment {payment.Id} verified with tx {dto.TransactionHash}");

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
        return await _context.PlatformWallets
            .Where(w => w.Currency == currency.ToUpper() &&
                       w.Network == network.ToLower() &&
                       w.IsActive)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task CompletePaymentAsync(CryptoPaymentRequest payment)
    {
        try
        {
            payment.Status = "completed";
            payment.CompletedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            if (payment.Purpose == "premium_subscription" && payment.BirdId != null)
            {
                await ActivatePremiumSubscriptionAsync(payment);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Payment {payment.Id} completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to complete payment {payment.Id}");
            payment.Status = "failed";
            await _context.SaveChangesAsync();
        }
    }

    private async Task ActivatePremiumSubscriptionAsync(CryptoPaymentRequest payment)
    {
        if (payment.BirdId == null) return;

        var existingSub = await _context.BirdPremiumSubscriptions
            .FirstOrDefaultAsync(s => s.BirdId == payment.BirdId && s.Status == "active");

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

            if (existingSub.CurrentPeriodEnd < DateTime.UtcNow)
            {
                existingSub.CurrentPeriodEnd = DateTime.UtcNow.AddDays(extensionDays);
            }
            else
            {
                existingSub.CurrentPeriodEnd = existingSub.CurrentPeriodEnd.AddDays(extensionDays);
            }

            existingSub.UpdatedAt = DateTime.UtcNow;
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

            var subscription = new BirdPremiumSubscription
            {
                BirdId = payment.BirdId.Value,
                OwnerId = payment.UserId,
                Status = "active",
                Plan = payment.Plan ?? "monthly",
                Provider = "crypto",
                ProviderSubscriptionId = payment.Id.ToString(),
                PriceCents = (long)(payment.AmountUsd * 100),
                DurationDays = durationDays,
                StartedAt = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(durationDays),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BirdPremiumSubscriptions.Add(subscription);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Premium activated for bird {payment.BirdId}");
    }

    private int GetRequiredConfirmations(string network)
    {
        return network.ToLower() switch
        {
            "tron" => 19,
            "ethereum" => 12,
            "bitcoin" => 2,
            "binance-smart-chain" => 15,
            "polygon" => 128,
            "solana" => 32,
            _ => 6
        };
    }

    private string GeneratePaymentUri(string currency, string network, string address, decimal amount)
    {
        return network.ToLower() switch
        {
            "tron" => address,
            "ethereum" or "polygon" or "binance-smart-chain" => $"ethereum:{address}",
            "bitcoin" => $"bitcoin:{address}?amount={amount}",
            "solana" => $"solana:{address}?amount={amount}",
            _ => address
        };
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
        var payment = await _context.CryptoPaymentRequests
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

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
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Payment {payment.Id} cancelled by user {userId}");

        return MapToDto(payment);
    }
}
