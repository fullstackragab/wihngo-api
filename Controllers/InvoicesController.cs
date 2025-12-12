using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Wihngo.Services;

namespace Wihngo.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoicePdfService _pdfService;
    private readonly IRefundService _refundService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IInvoiceService invoiceService,
        IInvoicePdfService pdfService,
        IRefundService refundService,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new invoice
    /// POST /api/v1/invoices
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            if (request.AmountFiat <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than zero" });
            }

            if (request.PreferredPaymentMethods == null || !request.PreferredPaymentMethods.Any())
            {
                return BadRequest(new { message = "At least one payment method must be specified" });
            }

            var invoice = await _invoiceService.CreateInvoiceAsync(
                userId,
                request.BirdId,
                request.AmountFiat,
                request.FiatCurrency ?? "USD",
                request.PreferredPaymentMethods,
                request.Metadata);

            // Build checkout instructions
            var checkoutInstructions = new
            {
                invoiceId = invoice.Id,
                amount = invoice.AmountFiat,
                currency = invoice.FiatCurrency,
                expiresAt = invoice.ExpiresAt,
                paymentMethods = new
                {
                    solana = !string.IsNullOrEmpty(invoice.SolanaReference) ? new
                    {
                        reference = invoice.SolanaReference,
                        // Solana Pay URI format: solana:<recipient>?amount=<amount>&reference=<reference>&label=<label>&message=<message>
                        solanaPayUri = $"solana:MERCHANT_ADDRESS?amount={invoice.AmountFiat}&reference={invoice.SolanaReference}&label=Wihngo&message=Payment%20for%20invoice%20{invoice.Id}",
                        instructions = "Scan QR code or use Solana Pay compatible wallet"
                    } : null,
                    
                    @base = !string.IsNullOrEmpty(invoice.BasePaymentData) ? new
                    {
                        data = JsonSerializer.Deserialize<Dictionary<string, object>>(invoice.BasePaymentData),
                        // EIP-681 format: ethereum:<address>@<chainId>/<function>?<parameters>
                        eip681Uri = $"ethereum:MERCHANT_ADDRESS@8453?value={invoice.AmountFiat}",
                        instructions = "Send USDC/EURC to merchant address on Base network"
                    } : null,
                    
                    paypal = !string.IsNullOrEmpty(invoice.PayPalOrderId) ? new
                    {
                        orderId = invoice.PayPalOrderId,
                        approvalUrl = $"https://www.paypal.com/checkoutnow?token={invoice.PayPalOrderId}",
                        instructions = "Click the approval URL to complete payment via PayPal"
                    } : null
                }
            };

            return Ok(new
            {
                success = true,
                invoice = new
                {
                    id = invoice.Id,
                    amount = invoice.AmountFiat,
                    currency = invoice.FiatCurrency,
                    state = invoice.State,
                    createdAt = invoice.CreatedAt,
                    expiresAt = invoice.ExpiresAt
                },
                checkout = checkoutInstructions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice");
            return StatusCode(500, new { message = "Failed to create invoice", error = ex.Message });
        }
    }

    /// <summary>
    /// Get invoice metadata
    /// GET /api/v1/invoices/{invoiceId}
    /// </summary>
    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> GetInvoice(Guid invoiceId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);
            
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Verify ownership
            if (invoice.UserId != userId)
            {
                return Forbid();
            }

            return Ok(new
            {
                success = true,
                invoice = new
                {
                    id = invoice.Id,
                    invoiceNumber = invoice.InvoiceNumber,
                    userId = invoice.UserId,
                    birdId = invoice.BirdId,
                    amount = invoice.AmountFiat,
                    currency = invoice.FiatCurrency,
                    settlementAmount = invoice.AmountFiatAtSettlement,
                    settlementCurrency = invoice.SettlementCurrency,
                    state = invoice.State,
                    pdfUrl = invoice.IssuedPdfUrl,
                    issuedAt = invoice.IssuedAt,
                    isTaxDeductible = invoice.IsTaxDeductible,
                    createdAt = invoice.CreatedAt,
                    expiresAt = invoice.ExpiresAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Failed to retrieve invoice", error = ex.Message });
        }
    }

    /// <summary>
    /// Download invoice PDF
    /// GET /api/v1/invoices/{invoiceId}/download
    /// </summary>
    [HttpGet("{invoiceId}/download")]
    public async Task<IActionResult> DownloadInvoice(Guid invoiceId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);
            
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Verify ownership
            if (invoice.UserId != userId)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(invoice.IssuedPdfUrl))
            {
                return BadRequest(new { message = "Invoice PDF not yet generated" });
            }

            var pdfBytes = await _pdfService.GetInvoicePdfBytesAsync(invoice.IssuedPdfUrl);
            
            return File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Failed to download invoice", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user's invoices
    /// GET /api/v1/invoices
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var invoices = await _invoiceService.GetUserInvoicesAsync(userId, page, pageSize);

            return Ok(new
            {
                success = true,
                page,
                pageSize,
                invoices = invoices.Select(i => new
                {
                    id = i.Id,
                    invoiceNumber = i.InvoiceNumber,
                    amount = i.AmountFiat,
                    currency = i.FiatCurrency,
                    state = i.State,
                    issuedAt = i.IssuedAt,
                    createdAt = i.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user invoices");
            return StatusCode(500, new { message = "Failed to retrieve invoices", error = ex.Message });
        }
    }

    /// <summary>
    /// Create refund request for an invoice
    /// POST /api/v1/invoices/{invoiceId}/refund
    /// </summary>
    [HttpPost("{invoiceId}/refund")]
    public async Task<IActionResult> CreateRefund(Guid invoiceId, [FromBody] CreateRefundRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);
            
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Verify ownership
            if (invoice.UserId != userId)
            {
                return Forbid();
            }

            // Validate refund amount
            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Refund amount must be greater than zero" });
            }

            var maxRefundAmount = invoice.AmountFiatAtSettlement ?? invoice.AmountFiat;
            if (request.Amount > maxRefundAmount)
            {
                return BadRequest(new { message = $"Refund amount cannot exceed {maxRefundAmount} {invoice.FiatCurrency}" });
            }

            if (string.IsNullOrEmpty(request.Reason))
            {
                return BadRequest(new { message = "Refund reason is required" });
            }

            var refundRequest = await _refundService.CreateRefundRequestAsync(
                invoiceId,
                request.Amount,
                request.Currency ?? invoice.FiatCurrency,
                request.Reason);

            return Ok(new
            {
                success = true,
                message = "Refund request created successfully",
                refund = new
                {
                    id = refundRequest.Id,
                    invoiceId = refundRequest.InvoiceId,
                    amount = refundRequest.Amount,
                    currency = refundRequest.Currency,
                    reason = refundRequest.Reason,
                    state = refundRequest.State,
                    requiresApproval = refundRequest.RequiresApproval,
                    createdAt = refundRequest.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create refund request for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Failed to create refund request", error = ex.Message });
        }
    }
}

public class CreateInvoiceRequest
{
    public Guid? BirdId { get; set; }
    public decimal AmountFiat { get; set; }
    public string? FiatCurrency { get; set; } = "USD";
    public List<string> PreferredPaymentMethods { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
}

public class CreateRefundRequest
{
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string Reason { get; set; } = string.Empty;
}
