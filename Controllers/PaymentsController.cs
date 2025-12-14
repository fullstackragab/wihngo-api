using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using Wihngo.Services;

namespace Wihngo.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentAuditService _auditService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        AppDbContext context,
        IInvoiceService invoiceService,
        IPaymentAuditService auditService,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _invoiceService = invoiceService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Submit payment transaction info from mobile client
    /// POST /api/v1/payments/submit
    /// Used by mobile to report txHash and payer info for manual verification
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitPayment([FromBody] SubmitPaymentRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            // Validate request
            if (string.IsNullOrEmpty(request.TxHash))
            {
                return BadRequest(new { message = "Transaction hash is required" });
            }

            // Get invoice
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId);

            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Verify ownership
            if (invoice.UserId != userId)
            {
                return Forbid();
            }

            // Check if payment already exists
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TxHash == request.TxHash);

            if (existingPayment != null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Payment already submitted",
                    payment = new
                    {
                        id = existingPayment.Id,
                        status = "already_exists"
                    }
                });
            }

            // Create payment record (pending blockchain confirmation)
            var payment = new Payment
            {
                InvoiceId = request.InvoiceId,
                PaymentMethod = request.PaymentMethod ?? "unknown",
                PayerIdentifier = request.PayerWalletAddress,
                TxHash = request.TxHash,
                Token = request.Token,
                Chain = request.Chain,
                AmountCrypto = request.AmountCrypto,
                Confirmations = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            // Update invoice state to awaiting confirmation
            if (invoice.State == nameof(InvoicePaymentState.CREATED))
            {
                invoice.State = nameof(InvoicePaymentState.AWAITING_PAYMENT);
                invoice.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Log event
            await _auditService.LogPaymentEventAsync(
                invoice.Id,
                payment.Id,
                "PAYMENT_SUBMITTED",
                invoice.State,
                nameof(InvoicePaymentState.ONCHAIN_CONFIRMING),
                userId.ToString(),
                "Payment submitted by user, awaiting blockchain confirmation",
                new Dictionary<string, object>
                {
                    ["txHash"] = request.TxHash,
                    ["chain"] = request.Chain ?? "unknown",
                    ["payerAddress"] = request.PayerWalletAddress ?? "unknown"
                });

            _logger.LogInformation("Payment submitted for invoice {InvoiceId} with tx hash {TxHash}",
                request.InvoiceId, request.TxHash);

            return Ok(new
            {
                success = true,
                message = "Payment submitted successfully. It will be confirmed once the transaction is verified on-chain.",
                payment = new
                {
                    id = payment.Id,
                    invoiceId = payment.InvoiceId,
                    txHash = payment.TxHash,
                    status = "pending_confirmation",
                    confirmations = payment.Confirmations
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit payment");
            return StatusCode(500, new { message = "Failed to submit payment", error = ex.Message });
        }
    }

    /// <summary>
    /// Get payment status for an invoice
    /// GET /api/v1/payments/{invoiceId}/status
    /// </summary>
    [HttpGet("{invoiceId}/status")]
    public async Task<IActionResult> GetPaymentStatus(Guid invoiceId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            // Verify ownership
            if (invoice.UserId != userId)
            {
                return Forbid();
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId);

            return Ok(new
            {
                success = true,
                invoice = new
                {
                    id = invoice.Id,
                    state = invoice.State,
                    invoiceNumber = invoice.InvoiceNumber
                },
                payment = payment != null ? new
                {
                    id = payment.Id,
                    method = payment.PaymentMethod,
                    txHash = payment.TxHash,
                    confirmations = payment.Confirmations,
                    confirmedAt = payment.ConfirmedAt,
                    chain = payment.Chain,
                    token = payment.Token,
                    amount = payment.AmountCrypto
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment status for invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Failed to get payment status", error = ex.Message });
        }
    }
}

public class SubmitPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? PayerWalletAddress { get; set; }
    public string? Token { get; set; }
    public string? Chain { get; set; }
    public decimal? AmountCrypto { get; set; }
}
