using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Wihngo.Data;
using Wihngo.Models.Entities;

namespace Wihngo.Services;

public interface IPaymentAuditService
{
    Task LogPaymentEventAsync(Guid invoiceId, Guid? paymentId, string eventType, 
        string? previousState, string? newState, string? actor, string? reason, 
        Dictionary<string, object>? rawPayload = null);
    Task LogAuditAsync(string entityType, Guid entityId, string action, Guid? userId, 
        Dictionary<string, object>? details, string? ipAddress = null);
    Task<List<PaymentEvent>> GetInvoiceEventsAsync(Guid invoiceId);
    Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId);
}

public class PaymentAuditService : IPaymentAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentAuditService> _logger;

    public PaymentAuditService(AppDbContext context, ILogger<PaymentAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogPaymentEventAsync(
        Guid invoiceId, 
        Guid? paymentId, 
        string eventType,
        string? previousState, 
        string? newState, 
        string? actor, 
        string? reason,
        Dictionary<string, object>? rawPayload = null)
    {
        try
        {
            var paymentEvent = new PaymentEvent
            {
                InvoiceId = invoiceId,
                PaymentId = paymentId,
                EventType = eventType,
                PreviousState = previousState,
                NewState = newState,
                Actor = actor,
                Reason = reason,
                RawPayload = rawPayload != null ? JsonSerializer.Serialize(rawPayload) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentEvents.Add(paymentEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Logged payment event: {EventType} for invoice {InvoiceId}", 
                eventType, invoiceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log payment event for invoice {InvoiceId}", invoiceId);
            // Don't throw - audit logging should not break the flow
        }
    }

    public async Task LogAuditAsync(
        string entityType, 
        Guid entityId, 
        string action, 
        Guid? userId,
        Dictionary<string, object>? details, 
        string? ipAddress = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Logged audit: {Action} on {EntityType} {EntityId}", 
                action, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit for {EntityType} {EntityId}", entityType, entityId);
            // Don't throw - audit logging should not break the flow
        }
    }

    public async Task<List<PaymentEvent>> GetInvoiceEventsAsync(Guid invoiceId)
    {
        return await _context.PaymentEvents
            .AsNoTracking()
            .Where(pe => pe.InvoiceId == invoiceId)
            .OrderBy(pe => pe.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderBy(al => al.CreatedAt)
            .ToListAsync();
    }
}
