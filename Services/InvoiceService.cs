using System;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

public interface IInvoiceService
{
    Task<Invoice> CreateInvoiceAsync(Guid userId, Guid? birdId, decimal amountFiat, string fiatCurrency, 
        List<string> preferredPaymentMethods, Dictionary<string, object>? metadata = null);
    Task<Invoice?> GetInvoiceAsync(Guid invoiceId);
    Task<List<Invoice>> GetUserInvoicesAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<Invoice> UpdateInvoiceStateAsync(Guid invoiceId, InvoicePaymentState newState, string? reason = null);
    Task<string> GenerateAndIssueInvoiceAsync(Guid invoiceId);
    Task<Invoice> RecordPaymentConfirmedAsync(Guid invoiceId, Payment payment, decimal? fiatValueAtPayment = null);
}

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly InvoiceConfiguration _config;
    private readonly IInvoicePdfService _pdfService;
    private readonly IInvoiceEmailService _emailService;
    private readonly IPushNotificationService _pushService;
    private readonly IPaymentAuditService _auditService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        AppDbContext context,
        IDbConnectionFactory dbFactory,
        IOptions<InvoiceConfiguration> config,
        IInvoicePdfService pdfService,
        IInvoiceEmailService emailService,
        IPushNotificationService pushService,
        IPaymentAuditService auditService,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _dbFactory = dbFactory;
        _config = config.Value;
        _pdfService = pdfService;
        _emailService = emailService;
        _pushService = pushService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Invoice> CreateInvoiceAsync(
        Guid userId, 
        Guid? birdId, 
        decimal amountFiat, 
        string fiatCurrency,
        List<string> preferredPaymentMethods,
        Dictionary<string, object>? metadata = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = new Invoice
            {
                UserId = userId,
                BirdId = birdId,
                AmountFiat = amountFiat,
                FiatCurrency = fiatCurrency,
                State = nameof(InvoicePaymentState.CREATED),
                PreferredPaymentMethods = JsonSerializer.Serialize(preferredPaymentMethods),
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_config.InvoiceExpiryMinutes),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Generate payment method specific data
            if (preferredPaymentMethods.Contains("solana"))
            {
                invoice.SolanaReference = Guid.NewGuid().ToString("N"); // Used as reference pubkey
            }

            if (preferredPaymentMethods.Contains("base"))
            {
                var baseData = new
                {
                    invoiceId = invoice.Id.ToString(),
                    // EIP-681 format or contract calldata can be generated here
                };
                invoice.BasePaymentData = JsonSerializer.Serialize(baseData);
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Log event
            await _auditService.LogPaymentEventAsync(
                invoice.Id,
                null,
                "INVOICE_CREATED",
                null,
                nameof(InvoicePaymentState.CREATED),
                "system",
                "Invoice created",
                metadata);

            await transaction.CommitAsync();

            _logger.LogInformation("Created invoice {InvoiceId} for user {UserId} amount {Amount} {Currency}",
                invoice.Id, userId, amountFiat, fiatCurrency);

            return invoice;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create invoice for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Invoice?> GetInvoiceAsync(Guid invoiceId)
    {
        return await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<List<Invoice>> GetUserInvoicesAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Invoice> UpdateInvoiceStateAsync(Guid invoiceId, InvoicePaymentState newState, string? reason = null)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice == null)
            throw new InvalidOperationException($"Invoice {invoiceId} not found");

        var previousState = invoice.State;
        invoice.State = newState.ToString();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogPaymentEventAsync(
            invoiceId,
            null,
            "STATE_TRANSITION",
            previousState,
            invoice.State,
            "system",
            reason ?? $"State changed from {previousState} to {newState}");

        _logger.LogInformation("Updated invoice {InvoiceId} state from {PreviousState} to {NewState}",
            invoiceId, previousState, newState);

        return invoice;
    }

    public async Task<string> GenerateAndIssueInvoiceAsync(Guid invoiceId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i == _context.Invoices.FirstOrDefault(x => x.Id == invoiceId))
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
                
            if (invoice == null)
                throw new InvalidOperationException($"Invoice {invoiceId} not found");

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId);
                
            if (payment == null)
                throw new InvalidOperationException($"No payment found for invoice {invoiceId}");

            // Generate sequential invoice number using database sequence
            var invoiceNumber = await GenerateInvoiceNumberAsync();
            invoice.InvoiceNumber = invoiceNumber;
            invoice.IssuedAt = DateTime.UtcNow;
            invoice.State = nameof(InvoicePaymentState.INVOICE_ISSUED);
            invoice.UpdatedAt = DateTime.UtcNow;

            // Generate PDF
            var pdfUrl = await _pdfService.GenerateInvoicePdfAsync(invoice, payment);
            invoice.IssuedPdfUrl = pdfUrl;

            await _context.SaveChangesAsync();

            // Get user email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == invoice.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                // Send email with PDF attachment
                var pdfBytes = await _pdfService.GetInvoicePdfBytesAsync(pdfUrl);
                await _emailService.SendInvoiceEmailAsync(
                    user.Email,
                    invoiceNumber,
                    pdfBytes,
                    user.Name);

                // Send push notification
                await _pushService.SendInvoiceIssuedNotificationAsync(user.UserId, invoiceNumber);
            }

            // Log audit event
            await _auditService.LogPaymentEventAsync(
                invoiceId,
                payment.Id,
                "INVOICE_ISSUED",
                null,
                nameof(InvoicePaymentState.INVOICE_ISSUED),
                "system",
                $"Invoice {invoiceNumber} issued and sent to user");

            await transaction.CommitAsync();

            _logger.LogInformation("Generated and issued invoice {InvoiceNumber} for invoice {InvoiceId}",
                invoiceNumber, invoiceId);

            return invoiceNumber;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to generate and issue invoice for {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<Invoice> RecordPaymentConfirmedAsync(Guid invoiceId, Payment payment, decimal? fiatValueAtPayment = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null)
                throw new InvalidOperationException($"Invoice {invoiceId} not found");

            // Update invoice with settlement information
            invoice.AmountFiatAtSettlement = fiatValueAtPayment ?? invoice.AmountFiat;
            invoice.SettlementCurrency = invoice.FiatCurrency;
            invoice.State = nameof(InvoicePaymentState.CONFIRMED);
            invoice.UpdatedAt = DateTime.UtcNow;

            // Save payment
            payment.InvoiceId = invoiceId;
            payment.ConfirmedAt = DateTime.UtcNow;
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            // Log event
            await _auditService.LogPaymentEventAsync(
                invoiceId,
                payment.Id,
                "PAYMENT_CONFIRMED",
                nameof(InvoicePaymentState.AWAITING_PAYMENT),
                nameof(InvoicePaymentState.CONFIRMED),
                "system",
                $"Payment confirmed via {payment.PaymentMethod}");

            await transaction.CommitAsync();

            _logger.LogInformation("Recorded payment confirmation for invoice {InvoiceId}", invoiceId);

            // Trigger invoice generation asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await GenerateAndIssueInvoiceAsync(invoiceId);
                    
                    // Mark as completed
                    await UpdateInvoiceStateAsync(invoiceId, InvoicePaymentState.COMPLETED, "Invoice issued and sent");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate invoice after payment confirmation for {InvoiceId}", invoiceId);
                }
            });

            return invoice;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to record payment confirmation for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        // Use database sequence for atomic invoice number generation
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
        
        using var connection = await _dbFactory.CreateOpenConnectionAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT nextval('wihngo_invoice_seq')";
        var nextVal = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        return $"{_config.InvoicePrefix}-{yearMonth}-{nextVal:D6}";
    }
}
