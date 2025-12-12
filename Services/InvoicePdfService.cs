using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Models.Entities;
using System.Text.Json;

namespace Wihngo.Services;

public interface IInvoicePdfService
{
    Task<string> GenerateInvoicePdfAsync(Invoice invoice, Payment payment);
    Task<byte[]> GetInvoicePdfBytesAsync(string pdfUrl);
}

public class InvoicePdfService : IInvoicePdfService
{
    private readonly InvoiceConfiguration _config;
    private readonly ILogger<InvoicePdfService> _logger;

    public InvoicePdfService(
        IOptions<InvoiceConfiguration> config,
        ILogger<InvoicePdfService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        // Set QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerateInvoicePdfAsync(Invoice invoice, Payment payment)
    {
        try
        {
            var document = CreateInvoiceDocument(invoice, payment);
            
            // Ensure directory exists
            var directory = Path.Combine(Directory.GetCurrentDirectory(), _config.StoragePath);
            Directory.CreateDirectory(directory);
            
            var fileName = $"{invoice.InvoiceNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var filePath = Path.Combine(directory, fileName);
            
            // Generate PDF
            document.GeneratePdf(filePath);
            
            _logger.LogInformation("Generated invoice PDF: {FilePath}", filePath);
            
            // Return relative path or S3 URL
            return _config.UseS3Storage 
                ? await UploadToS3Async(filePath, fileName) 
                : $"/{_config.StoragePath}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice PDF for {InvoiceNumber}", invoice.InvoiceNumber);
            throw;
        }
    }

    public async Task<byte[]> GetInvoicePdfBytesAsync(string pdfUrl)
    {
        if (_config.UseS3Storage)
        {
            // TODO: Download from S3
            throw new NotImplementedException("S3 download not implemented");
        }
        
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), pdfUrl.TrimStart('/'));
        return await File.ReadAllBytesAsync(filePath);
    }

    private Document CreateInvoiceDocument(Invoice invoice, Payment payment)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, invoice, payment));
                page.Footer().Element(ComposeFooter);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_config.CompanyName)
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Medium);
                    
                column.Item().Text(_config.CompanyAddress)
                    .FontSize(9);
                    
                if (!string.IsNullOrEmpty(_config.ContactEmail))
                {
                    column.Item().Text($"Email: {_config.ContactEmail}")
                        .FontSize(9);
                }
                
                if (!string.IsNullOrEmpty(_config.TaxNumber))
                {
                    column.Item().Text($"Tax ID: {_config.TaxNumber}")
                        .FontSize(9);
                }
            });

            row.ConstantItem(150).Column(column =>
            {
                column.Item().Text("PAYMENT RECEIPT")
                    .FontSize(16)
                    .Bold()
                    .AlignRight();
                    
                column.Item().PaddingTop(5).Text($"Date: {DateTime.UtcNow:yyyy-MM-dd}")
                    .FontSize(9)
                    .AlignRight();
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice, Payment payment)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Invoice Number
            column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
            {
                row.RelativeItem().Text("Receipt Number:").Bold();
                row.RelativeItem().Text(invoice.InvoiceNumber ?? "N/A").AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Invoice Date:").Bold();
                row.RelativeItem().Text($"{invoice.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC").AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Payment Date:").Bold();
                row.RelativeItem().Text($"{payment.ConfirmedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"} UTC").AlignRight();
            });

            // Payment Details Section
            column.Item().PaddingTop(20).Text("PAYMENT DETAILS").Bold().FontSize(14);
            
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text("Payment Method:").Bold();
                row.RelativeItem().Text(payment.PaymentMethod.ToUpper()).AlignRight();
            });

            if (!string.IsNullOrEmpty(payment.Token))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Token:").Bold();
                    row.RelativeItem().Text(payment.Token.ToUpper()).AlignRight();
                });
            }

            if (!string.IsNullOrEmpty(payment.Chain))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Blockchain:").Bold();
                    row.RelativeItem().Text(payment.Chain.ToUpper()).AlignRight();
                });
            }

            if (!string.IsNullOrEmpty(payment.TxHash))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Transaction Hash:").Bold();
                    row.RelativeItem().Text(TruncateHash(payment.TxHash)).AlignRight();
                });
                
                // Add blockchain explorer link
                var explorerUrl = GetExplorerUrl(payment.Chain, payment.TxHash);
                if (!string.IsNullOrEmpty(explorerUrl))
                {
                    column.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().Text("");
                        row.RelativeItem().Text($"View on explorer: {explorerUrl}")
                            .FontSize(8)
                            .FontColor(Colors.Blue.Medium)
                            .AlignRight();
                    });
                }
            }

            if (!string.IsNullOrEmpty(payment.ProviderTxId))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("PayPal Transaction ID:").Bold();
                    row.RelativeItem().Text(payment.ProviderTxId).AlignRight();
                });
            }

            if (!string.IsNullOrEmpty(payment.PayerIdentifier))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Payer:").Bold();
                    row.RelativeItem().Text(TruncateText(payment.PayerIdentifier, 50)).AlignRight();
                });
            }

            // Amount Section
            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            if (payment.AmountCrypto.HasValue && !string.IsNullOrEmpty(payment.Token))
            {
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text("Amount Paid (Crypto):").Bold();
                    row.RelativeItem().Text($"{payment.AmountCrypto:F6} {payment.Token}").AlignRight();
                });
            }

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Amount (USD):").Bold().FontSize(13);
                row.RelativeItem().Text($"${invoice.AmountFiatAtSettlement ?? invoice.AmountFiat:F2}")
                    .Bold()
                    .FontSize(13)
                    .AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Currency:").Bold();
                row.RelativeItem().Text(invoice.SettlementCurrency ?? invoice.FiatCurrency).AlignRight();
            });

            // Tax Deductibility Notice
            column.Item().PaddingTop(30).Background(Colors.Yellow.Lighten3).Padding(15).Column(noticeColumn =>
            {
                noticeColumn.Item().Text("IMPORTANT TAX NOTICE").Bold().FontSize(12);
                
                var notice = invoice.IsTaxDeductible 
                    ? "This payment may be eligible for tax deduction. Please consult with your tax advisor."
                    : (invoice.ReceiptNotes ?? _config.DefaultReceiptNotes);
                    
                noticeColumn.Item().PaddingTop(5).Text(notice).FontSize(9);
            });

            // Additional Notes
            if (!string.IsNullOrEmpty(invoice.ReceiptNotes) && !invoice.IsTaxDeductible)
            {
                column.Item().PaddingTop(15).Text("Notes:").Bold();
                column.Item().PaddingTop(5).Text(invoice.ReceiptNotes).FontSize(9);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Generated on: ").FontSize(8);
            text.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(8).Bold();
        });
        
        container.AlignCenter().PaddingTop(5).Text($"For support, contact: {_config.ContactEmail}")
            .FontSize(8)
            .FontColor(Colors.Grey.Medium);
    }

    private string TruncateHash(string hash)
    {
        if (hash.Length <= 20) return hash;
        return $"{hash[..10]}...{hash[^10..]}";
    }

    private string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return $"{text[..(maxLength - 3)]}...";
    }

    private string GetExplorerUrl(string? chain, string? txHash)
    {
        if (string.IsNullOrEmpty(chain) || string.IsNullOrEmpty(txHash))
            return string.Empty;

        return chain.ToLower() switch
        {
            "solana" => $"https://solscan.io/tx/{txHash}",
            "base" => $"https://basescan.org/tx/{txHash}",
            "ethereum" => $"https://etherscan.io/tx/{txHash}",
            _ => string.Empty
        };
    }

    private async Task<string> UploadToS3Async(string filePath, string fileName)
    {
        // TODO: Implement S3 upload using AWS SDK
        _logger.LogWarning("S3 upload not implemented, returning local path");
        return $"/{_config.StoragePath}/{fileName}";
    }
}
