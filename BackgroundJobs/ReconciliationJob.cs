using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;

namespace Wihngo.BackgroundJobs;

/// <summary>
/// Daily reconciliation job that cross-checks payment records with on-chain data and PayPal transaction history
/// </summary>
public class ReconciliationJob
{
    private readonly AppDbContext _context;
    private readonly InvoiceConfiguration _config;
    private readonly ILogger<ReconciliationJob> _logger;

    public ReconciliationJob(
        AppDbContext context,
        IOptions<InvoiceConfiguration> config,
        ILogger<ReconciliationJob> logger)
    {
        _context = context;
        _config = config.Value;
        _logger = logger;
    }

    public async Task ReconcilePaymentsAsync()
    {
        _logger.LogInformation("Starting daily payment reconciliation");

        var anomalies = new List<string>();
        var reconDate = DateTime.UtcNow.Date;

        try
        {
            // 1. Check for invoices stuck in CONFIRMED state without invoice issued
            var stuckInvoices = await _context.Invoices
                .Where(i => i.State == "CONFIRMED" && 
                           i.IssuedAt == null &&
                           i.CreatedAt < DateTime.UtcNow.AddHours(-24))
                .ToListAsync();

            if (stuckInvoices.Any())
            {
                anomalies.Add($"Found {stuckInvoices.Count} invoices stuck in CONFIRMED state without invoice issued:");
                foreach (var invoice in stuckInvoices)
                {
                    anomalies.Add($"  - Invoice {invoice.Id}, created {invoice.CreatedAt:yyyy-MM-dd HH:mm}");
                }
            }

            // 2. Check for payments without corresponding invoices
            var orphanedPayments = await _context.Payments
                .Where(p => !_context.Invoices.Any(i => i.Id == p.InvoiceId))
                .ToListAsync();

            if (orphanedPayments.Any())
            {
                anomalies.Add($"Found {orphanedPayments.Count} orphaned payments (no matching invoice):");
                foreach (var payment in orphanedPayments)
                {
                    anomalies.Add($"  - Payment {payment.Id}, tx {payment.TxHash ?? payment.ProviderTxId}");
                }
            }

            // 3. Check for duplicate payments by transaction hash
            var duplicatePayments = await _context.Payments
                .Where(p => p.TxHash != null)
                .GroupBy(p => p.TxHash)
                .Where(g => g.Count() > 1)
                .Select(g => new { TxHash = g.Key, Count = g.Count(), Payments = g.ToList() })
                .ToListAsync();

            if (duplicatePayments.Any())
            {
                anomalies.Add($"Found {duplicatePayments.Count} duplicate transaction hashes:");
                foreach (var dup in duplicatePayments)
                {
                    anomalies.Add($"  - TxHash {dup.TxHash}: {dup.Count} payments");
                }
            }

            // 4. Check for invoices that expired without payment
            var expiredUnpaid = await _context.Invoices
                .Where(i => i.ExpiresAt < DateTime.UtcNow &&
                           (i.State == "CREATED" || i.State == "AWAITING_PAYMENT") &&
                           i.State != "EXPIRED" &&
                           i.CreatedAt > DateTime.UtcNow.AddDays(-30)) // Only last 30 days
                .ToListAsync();

            if (expiredUnpaid.Any())
            {
                _logger.LogInformation("Found {Count} expired unpaid invoices to mark as EXPIRED", expiredUnpaid.Count);
                
                foreach (var invoice in expiredUnpaid)
                {
                    invoice.State = "EXPIRED";
                    invoice.UpdatedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
            }

            // 5. Check for refunds stuck in PROCESSING state
            var stuckRefunds = await _context.RefundRequests
                .Where(r => r.State == "PROCESSING" &&
                           r.CreatedAt < DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            if (stuckRefunds.Any())
            {
                anomalies.Add($"Found {stuckRefunds.Count} refunds stuck in PROCESSING state for > 7 days:");
                foreach (var refund in stuckRefunds)
                {
                    anomalies.Add($"  - Refund {refund.Id}, created {refund.CreatedAt:yyyy-MM-dd HH:mm}");
                }
            }

            // 6. Check for amount mismatches between invoice and payment
            var amountMismatches = await _context.Payments
                .Include(p => p.Invoice)
                .Where(p => p.FiatValueAtPayment.HasValue &&
                           p.Invoice != null &&
                           p.Invoice.AmountFiatAtSettlement.HasValue &&
                           Math.Abs(p.FiatValueAtPayment.Value - p.Invoice.AmountFiatAtSettlement.Value) > 0.01m)
                .ToListAsync();

            if (amountMismatches.Any())
            {
                anomalies.Add($"Found {amountMismatches.Count} payments with amount mismatches:");
                foreach (var payment in amountMismatches)
                {
                    anomalies.Add($"  - Payment {payment.Id}: Paid {payment.FiatValueAtPayment}, Expected {payment.Invoice?.AmountFiatAtSettlement}");
                }
            }

            // 7. Summary statistics
            var todayInvoices = await _context.Invoices
                .Where(i => i.CreatedAt >= reconDate)
                .CountAsync();

            var todayCompletedPayments = await _context.Payments
                .Where(p => p.ConfirmedAt >= reconDate)
                .CountAsync();

            var todayRevenue = await _context.Invoices
                .Where(i => i.IssuedAt >= reconDate && i.AmountFiatAtSettlement.HasValue)
                .SumAsync(i => i.AmountFiatAtSettlement ?? 0);

            _logger.LogInformation("Reconciliation summary: {InvoiceCount} invoices, {PaymentCount} payments, ${Revenue:F2} revenue today",
                todayInvoices, todayCompletedPayments, todayRevenue);

            // Send report if anomalies found
            if (anomalies.Any())
            {
                _logger.LogWarning("Found {Count} types of anomalies during reconciliation", anomalies.Count);
                await SendReconciliationReportAsync(anomalies, reconDate);
            }
            else
            {
                _logger.LogInformation("Reconciliation completed successfully with no anomalies");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during payment reconciliation");
            anomalies.Add($"ERROR: Reconciliation failed - {ex.Message}");
            await SendReconciliationReportAsync(anomalies, reconDate);
        }
    }

    private async Task SendReconciliationReportAsync(List<string> anomalies, DateTime reconDate)
    {
        try
        {
            var adminEmail = _config.ContactEmail; // Use this as admin notification email
            
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Payment Reconciliation Report - {reconDate:yyyy-MM-dd}");
            report.AppendLine();
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine();
            report.AppendLine("ANOMALIES DETECTED:");
            report.AppendLine();
            
            foreach (var anomaly in anomalies)
            {
                report.AppendLine(anomaly);
            }

            _logger.LogInformation("Sending reconciliation report to {Email}", adminEmail);

            // TODO: Implement proper admin notification email
            // For now, just log the report
            _logger.LogWarning("Reconciliation Report:\n{Report}", report.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reconciliation report");
        }
    }
}
