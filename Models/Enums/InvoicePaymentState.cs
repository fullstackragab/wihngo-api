namespace Wihngo.Models.Enums;

/// <summary>
/// Payment lifecycle states for Wihngo invoice/receipt system
/// </summary>
public enum InvoicePaymentState
{
    CREATED,
    AWAITING_PAYMENT,
    ONCHAIN_CONFIRMING,
    CONFIRMED,
    INVOICE_ISSUED,
    COMPLETED,
    FAILED,
    CANCELED,
    REFUNDED,
    EXPIRED
}
