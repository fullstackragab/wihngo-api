using Microsoft.Extensions.Options;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Wihngo.Services;

public interface IPayPalService
{
    Task<string> CreatePayPalOrderAsync(Guid invoiceId, decimal amount, string currency);
    Task<bool> CapturePayPalOrderAsync(string orderId);
    Task<bool> VerifyWebhookSignature(string webhookId, string transmissionId, string transmissionTime, 
        string certUrl, string authAlgo, string transmissionSig, string webhookEvent);
    Task<string> RefundPayPalTransactionAsync(string transactionId, decimal amount, string currency, string? note = null);
}

public class PayPalService : IPayPalService
{
    private readonly PayPalConfiguration _config;
    private readonly AppDbContext _context;
    private readonly ILogger<PayPalService> _logger;
    private readonly PayPalHttpClient _client;

    public PayPalService(
        IOptions<PayPalConfiguration> config,
        AppDbContext context,
        ILogger<PayPalService> logger)
    {
        _config = config.Value;
        _context = context;
        _logger = logger;

        // Initialize PayPal client
        var environment = _config.Mode.ToLower() == "live"
            ? new LiveEnvironment(_config.ClientId, _config.ClientSecret)
            : (PayPalEnvironment)new SandboxEnvironment(_config.ClientId, _config.ClientSecret);

        _client = new PayPalHttpClient(environment);
    }

    public async Task<string> CreatePayPalOrderAsync(Guid invoiceId, decimal amount, string currency)
    {
        try
        {
            var order = new OrderRequest
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        ReferenceId = invoiceId.ToString(),
                        AmountWithBreakdown = new AmountWithBreakdown
                        {
                            CurrencyCode = currency,
                            Value = amount.ToString("F2")
                        },
                        Description = $"Wihngo Payment - Invoice {invoiceId}"
                    }
                },
                ApplicationContext = new ApplicationContext
                {
                    ReturnUrl = _config.ReturnUrl,
                    CancelUrl = _config.CancelUrl,
                    BrandName = "Wihngo",
                    LandingPage = "BILLING",
                    UserAction = "PAY_NOW"
                }
            };

            var request = new OrdersCreateRequest();
            request.Prefer("return=representation");
            request.RequestBody(order);

            var response = await _client.Execute(request);
            var result = response.Result<Order>();

            // Update invoice with PayPal order ID
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice != null)
            {
                invoice.PayPalOrderId = result.Id;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Created PayPal order {OrderId} for invoice {InvoiceId}", 
                result.Id, invoiceId);

            return result.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayPal order for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<bool> CapturePayPalOrderAsync(string orderId)
    {
        try
        {
            var request = new OrdersCaptureRequest(orderId);
            request.Prefer("return=representation");
            request.RequestBody(new OrderActionRequest());

            var response = await _client.Execute(request);
            var result = response.Result<Order>();

            if (result.Status == "COMPLETED")
            {
                _logger.LogInformation("Captured PayPal order {OrderId}", orderId);
                return true;
            }

            _logger.LogWarning("PayPal order {OrderId} capture failed with status {Status}", 
                orderId, result.Status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture PayPal order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<bool> VerifyWebhookSignature(
        string webhookId, 
        string transmissionId, 
        string transmissionTime,
        string certUrl, 
        string authAlgo, 
        string transmissionSig, 
        string webhookEvent)
    {
        try
        {
            // PayPal webhook verification using SDK
            var request = new PayPalHttp.HttpRequest("/v1/notifications/verify-webhook-signature", HttpMethod.Post)
            {
                ContentType = "application/json"
            };

            request.Body = new
            {
                auth_algo = authAlgo,
                cert_url = certUrl,
                transmission_id = transmissionId,
                transmission_sig = transmissionSig,
                transmission_time = transmissionTime,
                webhook_id = webhookId,
                webhook_event = webhookEvent
            };

            var response = await _client.Execute(request);
            var result = response.Result<dynamic>();

            var verificationStatus = result.verification_status?.ToString() ?? "";
            bool isVerified = verificationStatus == "SUCCESS";

            _logger.LogInformation("PayPal webhook verification result: {Status}", (string)verificationStatus);

            return await Task.FromResult(isVerified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify PayPal webhook signature");
            return false;
        }
    }

    public async Task<string> RefundPayPalTransactionAsync(string transactionId, decimal amount, string currency, string? note = null)
    {
        try
        {
            var request = new PayPalHttp.HttpRequest($"/v2/payments/captures/{transactionId}/refund", HttpMethod.Post)
            {
                ContentType = "application/json"
            };

            request.Body = new
            {
                amount = new
                {
                    value = amount.ToString("F2"),
                    currency_code = currency
                },
                note_to_payer = note ?? "Refund processed by Wihngo"
            };

            var response = await _client.Execute(request);
            var result = response.Result<dynamic>();

            var refundId = result.id?.ToString() ?? "";
            _logger.LogInformation("Created PayPal refund {RefundId} for transaction {TransactionId}", 
                (string)refundId, transactionId);

            return refundId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund PayPal transaction {TransactionId}", transactionId);
            throw;
        }
    }
}
