using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

[ApiController]
[Route("api/payments/crypto")]
[Authorize]
public class CryptoPaymentController : ControllerBase
{
    private readonly ICryptoPaymentService _paymentService;
    private readonly AppDbContext _context;
    private readonly ILogger<CryptoPaymentController> _logger;

    public CryptoPaymentController(
        ICryptoPaymentService paymentService,
        AppDbContext context,
        ILogger<CryptoPaymentController> logger)
    {
        _paymentService = paymentService;
        _context = context;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userGuid;
    }

    /// <summary>
    /// Create a new crypto payment request
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto dto)
    {
        try
        {
            var userId = GetUserId();
            var payment = await _paymentService.CreatePaymentRequestAsync(userId, dto);

            return Ok(new
            {
                paymentRequest = payment,
                message = "Payment request created successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new { error = "Failed to create payment request" });
        }
    }

    /// <summary>
    /// Get payment status by ID
    /// </summary>
    [HttpGet("{paymentId}")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid paymentId)
    {
        try
        {
            var userId = GetUserId();
            var payment = await _context.CryptoPaymentRequests
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

            if (payment == null)
            {
                return NotFound(new { error = "Payment not found" });
            }

            // Check if expired
            if (payment.Status == "pending" && DateTime.UtcNow > payment.ExpiresAt)
            {
                payment.Status = "expired";
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // If payment has a transaction hash and is pending/confirming, check blockchain immediately
            // This provides real-time updates without requiring a separate check-status endpoint call
            if (payment.TransactionHash != null && 
                (payment.Status == "pending" || payment.Status == "confirming" || payment.Status == "confirmed"))
            {
                try
                {
                    var previousStatus = payment.Status;
                    var blockchainService = HttpContext.RequestServices.GetRequiredService<IBlockchainService>();
                    var txInfo = await blockchainService.VerifyTransactionAsync(
                        payment.TransactionHash,
                        payment.Currency,
                        payment.Network
                    );

                    if (txInfo != null)
                    {
                        payment.Confirmations = txInfo.Confirmations;

                        if (txInfo.Confirmations >= payment.RequiredConfirmations)
                        {
                            if (payment.Status == "confirmed")
                            {
                                // Already confirmed but not completed - complete it now
                                _logger.LogInformation($"[GetPayment] Payment {payment.Id} is confirmed, completing now");
                                await _paymentService.CompletePaymentAsync(payment);
                                await _context.Entry(payment).ReloadAsync();
                                
                                Console.WriteLine($"? Payment {payment.Id} completed via GetPayment endpoint");
                                _logger.LogInformation($"[GetPayment] Payment {payment.Id} completed - Status: {previousStatus} -> {payment.Status}");
                            }
                            else if (payment.Status != "completed")
                            {
                                // First time reaching required confirmations
                                payment.Status = "confirmed";
                                payment.ConfirmedAt = DateTime.UtcNow;
                                payment.UpdatedAt = DateTime.UtcNow;

                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation($"[GetPayment] Payment {payment.Id} confirmed, completing now");
                                await _paymentService.CompletePaymentAsync(payment);
                                await _context.Entry(payment).ReloadAsync();

                                Console.WriteLine($"? Payment {payment.Id} completed via GetPayment endpoint");
                                _logger.LogInformation($"[GetPayment] Payment {payment.Id} completed - Status: {previousStatus} -> {payment.Status}");
                            }
                        }
                        else if (payment.Status != "confirming")
                        {
                            payment.Status = "confirming";
                            payment.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation($"[GetPayment] Payment {payment.Id} status changed to 'confirming' ({txInfo.Confirmations}/{payment.RequiredConfirmations} confirmations)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking blockchain status in GetPayment for {PaymentId}", paymentId);
                    // Don't fail the request, just return current status
                }
            }

            // Reload to ensure we have the latest data
            await _context.Entry(payment).ReloadAsync();

            return Ok(new PaymentResponseDto
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
                ExpiresAt = DateTime.SpecifyKind(payment.ExpiresAt, DateTimeKind.Utc),
                ConfirmedAt = payment.ConfirmedAt.HasValue ? DateTime.SpecifyKind(payment.ConfirmedAt.Value, DateTimeKind.Utc) : null,
                CompletedAt = payment.CompletedAt.HasValue ? DateTime.SpecifyKind(payment.CompletedAt.Value, DateTimeKind.Utc) : null,
                CreatedAt = DateTime.SpecifyKind(payment.CreatedAt, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(payment.UpdatedAt, DateTimeKind.Utc)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId}", paymentId);
            return StatusCode(500, new { error = "Failed to get payment" });
        }
    }

    /// <summary>
    /// Verify a payment transaction
    /// </summary>
    [HttpPost("{paymentId}/verify")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPayment(Guid paymentId, [FromBody] VerifyPaymentDto dto)
    {
        try
        {
            var userId = GetUserId();
            var payment = await _paymentService.VerifyPaymentAsync(paymentId, userId, dto);

            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment {PaymentId}", paymentId);
            return StatusCode(500, new { error = "Failed to verify payment" });
        }
    }

    /// <summary>
    /// Get payment history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserId();
            var payments = await _paymentService.GetPaymentHistoryAsync(userId, page, pageSize);

            return Ok(new
            {
                payments,
                page,
                pageSize,
                total = payments.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history");
            return StatusCode(500, new { error = "Failed to get payment history" });
        }
    }

    /// <summary>
    /// Get exchange rates
    /// </summary>
    [HttpGet("rates")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ExchangeRateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRates()
    {
        try
        {
            var rates = await _context.CryptoExchangeRates
                .Select(r => new ExchangeRateDto
                {
                    Currency = r.Currency,
                    UsdRate = r.UsdRate,
                    LastUpdated = r.LastUpdated,
                    Source = r.Source
                })
                .ToListAsync();

            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rates");
            return StatusCode(500, new { error = "Failed to get exchange rates" });
        }
    }

    /// <summary>
    /// Get exchange rate for specific currency
    /// </summary>
    [HttpGet("rates/{currency}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExchangeRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRate(string currency)
    {
        try
        {
            var rate = await _context.CryptoExchangeRates
                .Where(r => r.Currency == currency.ToUpper())
                .Select(r => new ExchangeRateDto
                {
                    Currency = r.Currency,
                    UsdRate = r.UsdRate,
                    LastUpdated = r.LastUpdated,
                    Source = r.Source
                })
                .FirstOrDefaultAsync();

            if (rate == null)
            {
                return NotFound(new { error = "Currency not found" });
            }

            return Ok(rate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate for {Currency}", currency);
            return StatusCode(500, new { error = "Failed to get exchange rate" });
        }
    }

    /// <summary>
    /// Get platform wallet for currency/network
    /// </summary>
    [HttpGet("wallet/{currency}/{network}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWallet(string currency, string network)
    {
        try
        {
            var wallet = await _paymentService.GetPlatformWalletAsync(currency, network);

            if (wallet == null)
            {
                return NotFound(new { error = $"No wallet configured for {currency} on {network}" });
            }

            return Ok(new
            {
                currency = wallet.Currency,
                network = wallet.Network,
                address = wallet.Address,
                qrCode = wallet.Address,
                isActive = wallet.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet");
            return StatusCode(500, new { error = "Failed to get wallet info" });
        }
    }

    /// <summary>
    /// Cancel a pending payment
    /// </summary>
    [HttpPost("{paymentId}/cancel")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPayment(Guid paymentId)
    {
        try
        {
            var userId = GetUserId();
            var payment = await _paymentService.CancelPaymentAsync(paymentId, userId);

            if (payment == null)
            {
                return NotFound(new { error = "Payment not found" });
            }

            return Ok(new
            {
                id = payment.Id,
                status = payment.Status,
                message = "Payment cancelled successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
            return StatusCode(500, new { error = "Failed to cancel payment" });
        }
    }

    /// <summary>
    /// Force check payment status - triggers immediate blockchain verification
    /// Use this for real-time status updates when polling for payment confirmation
    /// </summary>
    [HttpPost("{paymentId}/check-status")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckPaymentStatus(Guid paymentId)
    {
        try
        {
            var userId = GetUserId();
            var payment = await _context.CryptoPaymentRequests
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

            if (payment == null)
            {
                return NotFound(new { error = "Payment not found" });
            }

            // Check if expired
            if (payment.Status == "pending" && DateTime.UtcNow > payment.ExpiresAt)
            {
                payment.Status = "expired";
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // If payment has a transaction hash and is pending/confirming, check blockchain immediately
            if (payment.TransactionHash != null && 
                (payment.Status == "pending" || payment.Status == "confirming" || payment.Status == "confirmed"))
            {
                try
                {
                    var previousStatus = payment.Status;
                    var blockchainService = HttpContext.RequestServices.GetRequiredService<IBlockchainService>();
                    var txInfo = await blockchainService.VerifyTransactionAsync(
                        payment.TransactionHash,
                        payment.Currency,
                        payment.Network
                    );

                    if (txInfo != null)
                    {
                        payment.Confirmations = txInfo.Confirmations;

                        if (txInfo.Confirmations >= payment.RequiredConfirmations)
                        {
                            if (payment.Status == "confirmed")
                            {
                                // Already confirmed but not completed - complete it now
                                _logger.LogInformation($"[CheckStatus] Payment {payment.Id} is confirmed, completing now");
                                await _paymentService.CompletePaymentAsync(payment);
                                await _context.Entry(payment).ReloadAsync();
                                
                                Console.WriteLine($"? Payment {payment.Id} completed via CheckStatus endpoint");
                                _logger.LogInformation($"[CheckStatus] Payment {payment.Id} completed - Status: {previousStatus} -> {payment.Status}");
                            }
                            else if (payment.Status != "completed")
                            {
                                // First time reaching required confirmations
                                payment.Status = "confirmed";
                                payment.ConfirmedAt = DateTime.UtcNow;
                                payment.UpdatedAt = DateTime.UtcNow;

                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation($"[CheckStatus] Payment {payment.Id} confirmed, completing now");
                                await _paymentService.CompletePaymentAsync(payment);
                                await _context.Entry(payment).ReloadAsync();

                                Console.WriteLine($"? Payment {payment.Id} completed via CheckStatus endpoint");
                                _logger.LogInformation($"[CheckStatus] Payment {payment.Id} completed - Status: {previousStatus} -> {payment.Status}");
                            }
                        }
                        else if (payment.Status != "confirming")
                        {
                            payment.Status = "confirming";
                            payment.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation($"[CheckStatus] Payment {payment.Id} status changed to 'confirming' ({txInfo.Confirmations}/{payment.RequiredConfirmations} confirmations)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking blockchain status in CheckPaymentStatus for {PaymentId}", paymentId);
                    // Don't fail the request, just return current status
                }
            }

            // Reload to ensure we have the latest data
            await _context.Entry(payment).ReloadAsync();

            return Ok(new PaymentResponseDto
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
                ExpiresAt = DateTime.SpecifyKind(payment.ExpiresAt, DateTimeKind.Utc),
                ConfirmedAt = payment.ConfirmedAt.HasValue ? DateTime.SpecifyKind(payment.ConfirmedAt.Value, DateTimeKind.Utc) : null,
                CompletedAt = payment.CompletedAt.HasValue ? DateTime.SpecifyKind(payment.CompletedAt.Value, DateTimeKind.Utc) : null,
                CreatedAt = DateTime.SpecifyKind(payment.CreatedAt, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(payment.UpdatedAt, DateTimeKind.Utc)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status {PaymentId}", paymentId);
            return StatusCode(500, new { error = "Failed to check payment status" });
        }
    }
}
