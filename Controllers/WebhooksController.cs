using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using Wihngo.Services;

namespace Wihngo.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPayPalService _payPalService;
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentAuditService _auditService;
    private readonly PayPalConfiguration _payPalConfig;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        AppDbContext context,
        IPayPalService payPalService,
        IInvoiceService invoiceService,
        IPaymentAuditService auditService,
        IOptions<PayPalConfiguration> payPalConfig,
        ILogger<WebhooksController> logger)
    {
        _context = context;
        _payPalService = payPalService;
        _invoiceService = invoiceService;
        _auditService = auditService;
        _payPalConfig = payPalConfig.Value;
        _logger = logger;
    }

    /// <summary>
    /// PayPal webhook endpoint
    /// POST /api/v1/webhooks/paypal
    /// </summary>
    [HttpPost("paypal")]
    public async Task<IActionResult> PayPalWebhook()
    {
        try
        {
            // Read webhook body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // Get PayPal webhook headers
            var transmissionId = Request.Headers["PAYPAL-TRANSMISSION-ID"].ToString();
            var transmissionTime = Request.Headers["PAYPAL-TRANSMISSION-TIME"].ToString();
            var certUrl = Request.Headers["PAYPAL-CERT-URL"].ToString();
            var authAlgo = Request.Headers["PAYPAL-AUTH-ALGO"].ToString();
            var transmissionSig = Request.Headers["PAYPAL-TRANSMISSION-SIG"].ToString();

            // Parse webhook event
            var webhookEvent = JsonSerializer.Deserialize<JsonElement>(body);
            var eventType = webhookEvent.GetProperty("event_type").GetString() ?? "";
            var eventId = webhookEvent.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Received PayPal webhook: {EventType} - {EventId}", eventType, eventId);

            // Check for duplicate webhook
            var existingWebhook = await _context.WebhooksReceived
                .FirstOrDefaultAsync(w => w.Provider == "paypal" && w.ProviderEventId == eventId);

            if (existingWebhook != null)
            {
                _logger.LogInformation("Duplicate PayPal webhook {EventId}, skipping", eventId);
                return Ok(new { message = "Webhook already processed" });
            }

            // Verify webhook signature
            var isValid = await _payPalService.VerifyWebhookSignature(
                _payPalConfig.WebhookId,
                transmissionId,
                transmissionTime,
                certUrl,
                authAlgo,
                transmissionSig,
                body);

            if (!isValid)
            {
                _logger.LogWarning("Invalid PayPal webhook signature for event {EventId}", eventId);
                return Unauthorized(new { message = "Invalid webhook signature" });
            }

            // Store webhook
            var webhookReceived = new WebhookReceived
            {
                Provider = "paypal",
                ProviderEventId = eventId,
                EventType = eventType,
                Payload = body,
                Processed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.WebhooksReceived.Add(webhookReceived);
            await _context.SaveChangesAsync();

            // Process webhook based on event type
            try
            {
                switch (eventType)
                {
                    case "PAYMENT.CAPTURE.COMPLETED":
                        await ProcessPaymentCaptureCompletedAsync(webhookEvent, webhookReceived.Id);
                        break;

                    case "PAYMENT.CAPTURE.DENIED":
                    case "PAYMENT.CAPTURE.DECLINED":
                        await ProcessPaymentCaptureFailedAsync(webhookEvent, webhookReceived.Id);
                        break;

                    case "PAYMENT.CAPTURE.REFUNDED":
                        await ProcessPaymentRefundedAsync(webhookEvent, webhookReceived.Id);
                        break;

                    default:
                        _logger.LogInformation("Unhandled PayPal webhook event type: {EventType}", eventType);
                        break;
                }

                // Mark as processed
                webhookReceived.Processed = true;
                webhookReceived.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal webhook {EventId}", eventId);
                webhookReceived.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PayPal webhook");
            return StatusCode(500, new { message = "Failed to process webhook", error = ex.Message });
        }
    }

    private async Task ProcessPaymentCaptureCompletedAsync(JsonElement webhookEvent, Guid webhookId)
    {
        try
        {
            var resource = webhookEvent.GetProperty("resource");
            var captureId = resource.GetProperty("id").GetString();
            var orderId = resource.GetProperty("supplementary_data")
                .GetProperty("related_ids")
                .GetProperty("order_id").GetString();

            _logger.LogInformation("Processing PayPal capture completed: Order {OrderId}, Capture {CaptureId}",
                orderId, captureId);

            // Find invoice by PayPal order ID
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.PayPalOrderId == orderId);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for PayPal order {OrderId}", orderId);
                return;
            }

            // Check if payment already exists
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ProviderTxId == captureId);

            if (existingPayment != null)
            {
                _logger.LogInformation("Payment already recorded for capture {CaptureId}", captureId);
                return;
            }

            // Get payer email
            var payerEmail = resource.GetProperty("payer")
                .GetProperty("email_address").GetString();

            // Get amount
            var amount = decimal.Parse(resource.GetProperty("amount")
                .GetProperty("value").GetString() ?? "0");

            var currency = resource.GetProperty("amount")
                .GetProperty("currency_code").GetString();

            // Create payment record
            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                PaymentMethod = "paypal",
                PayerIdentifier = payerEmail,
                ProviderTxId = captureId,
                FiatValueAtPayment = amount,
                Confirmations = 1, // PayPal is instant
                ConfirmedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Record payment confirmation
            await _invoiceService.RecordPaymentConfirmedAsync(invoice.Id, payment, amount);

            _logger.LogInformation("Processed PayPal payment for invoice {InvoiceId}", invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PayPal capture completed webhook");
            throw;
        }
    }

    private async Task ProcessPaymentCaptureFailedAsync(JsonElement webhookEvent, Guid webhookId)
    {
        try
        {
            var resource = webhookEvent.GetProperty("resource");
            var orderId = resource.GetProperty("supplementary_data")
                .GetProperty("related_ids")
                .GetProperty("order_id").GetString();

            _logger.LogInformation("Processing PayPal capture failed: Order {OrderId}", orderId);

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.PayPalOrderId == orderId);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for PayPal order {OrderId}", orderId);
                return;
            }

            // Update invoice state to failed
            await _invoiceService.UpdateInvoiceStateAsync(
                invoice.Id,
                InvoicePaymentState.FAILED,
                "PayPal payment capture failed");

            _logger.LogInformation("Marked invoice {InvoiceId} as failed", invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PayPal capture failed webhook");
            throw;
        }
    }

    private async Task ProcessPaymentRefundedAsync(JsonElement webhookEvent, Guid webhookId)
    {
        try
        {
            var resource = webhookEvent.GetProperty("resource");
            var refundId = resource.GetProperty("id").GetString();

            _logger.LogInformation("Processing PayPal refund: Refund {RefundId}", refundId);

            // Find refund request by provider refund ID
            var refundRequest = await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.ProviderRefundId == refundId);

            if (refundRequest == null)
            {
                _logger.LogWarning("Refund request not found for PayPal refund {RefundId}", refundId);
                return;
            }

            // Update refund request state if needed
            if (refundRequest.State != "COMPLETED")
            {
                refundRequest.State = "COMPLETED";
                refundRequest.CompletedAt = DateTime.UtcNow;
                refundRequest.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated refund request {RefundRequestId} to completed", refundRequest.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PayPal refund webhook");
            throw;
        }
    }

    /// <summary>
    /// Placeholder for EVM webhook (if using a service like Alchemy, QuickNode, etc.)
    /// POST /api/v1/webhooks/evm
    /// </summary>
    [HttpPost("evm")]
    public async Task<IActionResult> EvmWebhook()
    {
        _logger.LogInformation("Received EVM webhook (not implemented)");
        return Ok(new { message = "EVM webhook received but not implemented" });
    }

    /// <summary>
    /// Placeholder for Solana webhook
    /// POST /api/v1/webhooks/solana
    /// </summary>
    [HttpPost("solana")]
    public async Task<IActionResult> SolanaWebhook()
    {
        _logger.LogInformation("Received Solana webhook (not implemented)");
        return Ok(new { message = "Solana webhook received but not implemented" });
    }
}
