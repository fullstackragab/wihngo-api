using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;

namespace Wihngo.Services;

public interface IInvoiceEmailService
{
    Task SendInvoiceEmailAsync(string toEmail, string invoiceNumber, byte[] pdfBytes, string? userName = null);
    Task SendPaymentConfirmationAsync(string toEmail, string invoiceNumber, decimal amount, string currency);
}

public class InvoiceEmailService : IInvoiceEmailService
{
    private readonly SmtpConfiguration _config;
    private readonly InvoiceConfiguration _invoiceConfig;
    private readonly ILogger<InvoiceEmailService> _logger;

    public InvoiceEmailService(
        IOptions<SmtpConfiguration> config,
        IOptions<InvoiceConfiguration> invoiceConfig,
        ILogger<InvoiceEmailService> logger)
    {
        _config = config.Value;
        _invoiceConfig = invoiceConfig.Value;
        _logger = logger;
    }

    public async Task SendInvoiceEmailAsync(string toEmail, string invoiceNumber, byte[] pdfBytes, string? userName = null)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.FromName, _config.FromEmail));
            message.To.Add(new MailboxAddress(userName ?? toEmail, toEmail));
            message.Subject = $"Your Wihngo Payment Receipt - {invoiceNumber}";

            var bodyBuilder = new BodyBuilder();
            
            // HTML body
            bodyBuilder.HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2563eb; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
        .footer {{ background-color: #f3f4f6; padding: 15px; text-align: center; font-size: 12px; color: #6b7280; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #2563eb; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .info-box {{ background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Payment Receipt</h1>
        </div>
        <div class='content'>
            <p>Hello {(string.IsNullOrEmpty(userName) ? "" : userName + ",")}</p>
            
            <p>Thank you for your payment! Your transaction has been successfully processed.</p>
            
            <p><strong>Receipt Number:</strong> {invoiceNumber}</p>
            <p><strong>Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy}</p>
            
            <p>Your payment receipt is attached to this email as a PDF document. Please keep this for your records.</p>
            
            <div class='info-box'>
                <strong>Important:</strong> This payment is to Wihngo, a for-profit company. Unless otherwise stated, this payment is not eligible for tax deduction.
            </div>
            
            <p>If you have any questions or concerns about your payment, please don't hesitate to contact our support team.</p>
            
            <a href='mailto:{_invoiceConfig.ContactEmail}' class='button'>Contact Support</a>
            
            <p>Thank you for using Wihngo!</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.UtcNow.Year} {_invoiceConfig.CompanyName}. All rights reserved.</p>
            <p>{_invoiceConfig.CompanyAddress}</p>
            <p>Email: {_invoiceConfig.ContactEmail}</p>
        </div>
    </div>
</body>
</html>";

            // Plain text alternative
            bodyBuilder.TextBody = $@"
Payment Receipt - {invoiceNumber}

Hello {(string.IsNullOrEmpty(userName) ? "" : userName + ",")},

Thank you for your payment! Your transaction has been successfully processed.

Receipt Number: {invoiceNumber}
Date: {DateTime.UtcNow:MMMM dd, yyyy}

Your payment receipt is attached to this email as a PDF document. Please keep this for your records.

IMPORTANT: This payment is to Wihngo, a for-profit company. Unless otherwise stated, this payment is not eligible for tax deduction.

If you have any questions or concerns about your payment, please contact us at {_invoiceConfig.ContactEmail}.

Thank you for using Wihngo!

---
{_invoiceConfig.CompanyName}
{_invoiceConfig.CompanyAddress}
Email: {_invoiceConfig.ContactEmail}
";

            // Attach PDF
            bodyBuilder.Attachments.Add($"{invoiceNumber}.pdf", pdfBytes, ContentType.Parse("application/pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_config.Host, _config.Port, _config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            
            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                await client.AuthenticateAsync(_config.Username, _config.Password);
            }
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Invoice email sent to {Email} for {InvoiceNumber}", toEmail, invoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice email to {Email} for {InvoiceNumber}", toEmail, invoiceNumber);
            throw;
        }
    }

    public async Task SendPaymentConfirmationAsync(string toEmail, string invoiceNumber, decimal amount, string currency)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.FromName, _config.FromEmail));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = $"Payment Confirmed - {invoiceNumber}";

            var bodyBuilder = new BodyBuilder();
            
            bodyBuilder.HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #10b981; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
        .amount {{ font-size: 32px; font-weight: bold; color: #10b981; text-align: center; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>? Payment Confirmed</h1>
        </div>
        <div class='content'>
            <p>Your payment has been successfully confirmed!</p>
            
            <div class='amount'>{amount:F2} {currency}</div>
            
            <p><strong>Receipt Number:</strong> {invoiceNumber}</p>
            
            <p>Your official receipt will be sent to you shortly in a separate email.</p>
            
            <p>Thank you for your payment!</p>
        </div>
    </div>
</body>
</html>";

            bodyBuilder.TextBody = $@"
Payment Confirmed

Your payment has been successfully confirmed!

Amount: {amount:F2} {currency}
Receipt Number: {invoiceNumber}

Your official receipt will be sent to you shortly in a separate email.

Thank you for your payment!
";

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_config.Host, _config.Port, _config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            
            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                await client.AuthenticateAsync(_config.Username, _config.Password);
            }
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Payment confirmation email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment confirmation email to {Email}", toEmail);
            // Don't throw - email is not critical for payment confirmation
        }
    }
}
