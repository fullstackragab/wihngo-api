using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var payment = await _paymentService.GetPaymentRequestAsync(paymentId, userId);

            if (payment == null)
            {
                return NotFound(new { error = "Payment not found" });
            }

            return Ok(payment);
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return Guid.Parse(userIdClaim);
    }
}
