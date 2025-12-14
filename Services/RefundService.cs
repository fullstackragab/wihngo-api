using System;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;

namespace Wihngo.Services;

public interface IRefundService
{
    Task<RefundRequest> CreateRefundRequestAsync(Guid invoiceId, decimal amount, string currency, string reason);
    Task<RefundRequest> ProcessPayPalRefundAsync(Guid refundRequestId);
    Task<RefundRequest> ProcessCryptoRefundAsync(Guid refundRequestId, string approvedByUserId);
    Task<List<RefundRequest>> GetPendingRefundsAsync();
    Task<RefundRequest?> GetRefundRequestAsync(Guid refundRequestId);
}

public class RefundService : IRefundService
{
    private readonly AppDbContext _context;
    private readonly IPayPalService _payPalService;
    private readonly IPaymentAuditService _auditService;
    private readonly IInvoiceEmailService _emailService;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        AppDbContext context,
        IPayPalService payPalService,
        IPaymentAuditService auditService,
        IInvoiceEmailService emailService,
        ILogger<RefundService> logger)
    {
        _context = context;
        _payPalService = payPalService;
        _auditService = auditService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RefundRequest> CreateRefundRequestAsync(Guid invoiceId, decimal amount, string currency, string reason)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                throw new InvalidOperationException($"Invoice {invoiceId} not found");

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId);

            if (payment == null)
                throw new InvalidOperationException($"No payment found for invoice {invoiceId}");

            // Check if refund already exists
            var existingRefund = await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.InvoiceId == invoiceId && r.State != "FAILED");

            if (existingRefund != null)
                throw new InvalidOperationException($"Refund request already exists for invoice {invoiceId}");

            var refundRequest = new RefundRequest
            {
                InvoiceId = invoiceId,
                PaymentId = payment.Id,
                Amount = amount,
                Currency = currency,
                Reason = reason,
                State = "REQUESTED",
                RefundMethod = payment.PaymentMethod,
                RequiresApproval = payment.PaymentMethod != "paypal", // Crypto refunds require approval
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RefundRequests.Add(refundRequest);
            await _context.SaveChangesAsync();

            // Log audit
            await _auditService.LogAuditAsync(
                "RefundRequest",
                refundRequest.Id,
                "REFUND_REQUESTED",
                null,
                new Dictionary<string, object>
                {
                    ["invoiceId"] = invoiceId,
                    ["amount"] = amount,
                    ["currency"] = currency,
                    ["reason"] = reason,
                    ["method"] = payment.PaymentMethod
                });

            await transaction.CommitAsync();

            _logger.LogInformation("Created refund request {RefundId} for invoice {InvoiceId}", 
                refundRequest.Id, invoiceId);

            // Auto-process PayPal refunds
            if (payment.PaymentMethod == "paypal")
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessPayPalRefundAsync(refundRequest.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-process PayPal refund {RefundId}", refundRequest.Id);
                    }
                });
            }

            return refundRequest;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create refund request for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<RefundRequest> ProcessPayPalRefundAsync(Guid refundRequestId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var refundRequest = await _context.RefundRequests
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == refundRequestId);

            if (refundRequest == null)
                throw new InvalidOperationException($"Refund request {refundRequestId} not found");

            if (refundRequest.Payment == null)
                throw new InvalidOperationException($"Payment not found for refund request {refundRequestId}");

            if (string.IsNullOrEmpty(refundRequest.Payment.ProviderTxId))
                throw new InvalidOperationException("PayPal transaction ID not found");

            // Update state to processing
            refundRequest.State = "PROCESSING";
            refundRequest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Call PayPal refund API
            var refundId = await _payPalService.RefundPayPalTransactionAsync(
                refundRequest.Payment.ProviderTxId,
                refundRequest.Amount,
                refundRequest.Currency,
                refundRequest.Reason);

            // Update refund request
            refundRequest.ProviderRefundId = refundId;
            refundRequest.State = "COMPLETED";
            refundRequest.CompletedAt = DateTime.UtcNow;
            refundRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditService.LogAuditAsync(
                "RefundRequest",
                refundRequest.Id,
                "REFUND_COMPLETED",
                null,
                new Dictionary<string, object>
                {
                    ["providerRefundId"] = refundId,
                    ["method"] = "paypal"
                });

            await transaction.CommitAsync();

            _logger.LogInformation("Processed PayPal refund {RefundId} with provider refund {ProviderRefundId}", 
                refundRequestId, refundId);

            // Send notification to user
            _ = Task.Run(async () =>
            {
                try
                {
                    var invoice = await _context.Invoices
                        .Include(i => i == _context.Invoices.FirstOrDefault(x => x.Id == refundRequest.InvoiceId))
                        .FirstOrDefaultAsync(i => i.Id == refundRequest.InvoiceId);

                    if (invoice != null)
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == invoice.UserId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            // TODO: Send refund confirmation email
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send refund notification for {RefundId}", refundRequestId);
                }
            });

            return refundRequest;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            
            // Update refund to failed state
            var refundRequest = await _context.RefundRequests.FirstOrDefaultAsync(r => r.Id == refundRequestId);
            if (refundRequest != null)
            {
                refundRequest.State = "FAILED";
                refundRequest.ErrorMessage = ex.Message;
                refundRequest.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogError(ex, "Failed to process PayPal refund {RefundId}", refundRequestId);
            throw;
        }
    }

    public async Task<RefundRequest> ProcessCryptoRefundAsync(Guid refundRequestId, string approvedByUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var refundRequest = await _context.RefundRequests
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == refundRequestId);

            if (refundRequest == null)
                throw new InvalidOperationException($"Refund request {refundRequestId} not found");

            if (!Guid.TryParse(approvedByUserId, out var approvedByGuid))
                throw new InvalidOperationException("Invalid approver user ID");

            // Mark as approved and ready for manual processing
            refundRequest.ApprovedBy = approvedByGuid;
            refundRequest.ApprovedAt = DateTime.UtcNow;
            refundRequest.State = "PROCESSING";
            refundRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditService.LogAuditAsync(
                "RefundRequest",
                refundRequest.Id,
                "REFUND_APPROVED",
                approvedByGuid,
                new Dictionary<string, object>
                {
                    ["approvedBy"] = approvedByUserId,
                    ["method"] = refundRequest.RefundMethod ?? "crypto"
                });

            await transaction.CommitAsync();

            _logger.LogInformation("Crypto refund {RefundId} approved by {ApprovedBy}, pending manual tx execution", 
                refundRequestId, approvedByUserId);

            // TODO: Queue job for crypto refund transaction
            // This would typically involve:
            // 1. Building a return transaction from merchant hot wallet
            // 2. Signing the transaction (potentially requires hardware wallet or multisig)
            // 3. Broadcasting the transaction
            // 4. Monitoring confirmation
            // 5. Updating refund request with tx hash and marking as COMPLETED

            return refundRequest;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to approve crypto refund {RefundId}", refundRequestId);
            throw;
        }
    }

    public async Task<List<RefundRequest>> GetPendingRefundsAsync()
    {
        return await _context.RefundRequests
            .AsNoTracking()
            .Include(r => r.Invoice)
            .Include(r => r.Payment)
            .Where(r => r.State == "REQUESTED" || r.State == "PROCESSING")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<RefundRequest?> GetRefundRequestAsync(Guid refundRequestId)
    {
        return await _context.RefundRequests
            .AsNoTracking()
            .Include(r => r.Invoice)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == refundRequestId);
    }
}
