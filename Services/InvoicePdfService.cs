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
    Task<byte[]> GenerateCryptoPaymentReceiptAsync(CryptoPaymentRequest payment, string invoiceNumber, string? userName = null, string? userEmail = null);
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

    public Task<byte[]> GenerateCryptoPaymentReceiptAsync(CryptoPaymentRequest payment, string invoiceNumber, string? userName = null, string? userEmail = null)
    {
        try
        {
            var document = CreateCryptoPaymentDocument(payment, invoiceNumber, userName, userEmail);

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);

            _logger.LogInformation("Generated crypto payment receipt PDF for {InvoiceNumber}", invoiceNumber);

            return Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate crypto payment receipt PDF for {InvoiceNumber}", invoiceNumber);
            throw;
        }
    }

    private Document CreateCryptoPaymentDocument(CryptoPaymentRequest payment, string invoiceNumber, string? userName, string? userEmail)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeCryptoContent(content, payment, invoiceNumber, userName, userEmail));
                page.Footer().Element(ComposeFooter);
            });
        });
    }

    private void ComposeCryptoContent(IContainer container, CryptoPaymentRequest payment, string invoiceNumber, string? userName, string? userEmail)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Invoice Number
            column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
            {
                row.RelativeItem().Text("Receipt Number:").Bold();
                row.RelativeItem().Text(invoiceNumber).AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Invoice Date:").Bold();
                row.RelativeItem().Text($"{payment.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC").AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Payment Date:").Bold();
                row.RelativeItem().Text($"{payment.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? payment.ConfirmedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"} UTC").AlignRight();
            });

            // Customer Details
            if (!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(userEmail))
            {
                column.Item().PaddingTop(15).Text("CUSTOMER").Bold().FontSize(12);
                column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                if (!string.IsNullOrEmpty(userName))
                {
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Name:").Bold();
                        row.RelativeItem().Text(userName).AlignRight();
                    });
                }

                if (!string.IsNullOrEmpty(userEmail))
                {
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Email:").Bold();
                        row.RelativeItem().Text(userEmail).AlignRight();
                    });
                }
            }

            // Payment Details Section
            column.Item().PaddingTop(20).Text("PAYMENT DETAILS").Bold().FontSize(14);
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text("Payment Method:").Bold();
                row.RelativeItem().Text("CRYPTOCURRENCY").AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Currency:").Bold();
                row.RelativeItem().Text(payment.Currency.ToUpper()).AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Network:").Bold();
                row.RelativeItem().Text(payment.Network.ToUpper()).AlignRight();
            });

            if (!string.IsNullOrEmpty(payment.TransactionHash))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Transaction Hash:").Bold();
                    row.RelativeItem().Text(TruncateHash(payment.TransactionHash)).AlignRight();
                });

                // Add blockchain explorer link
                var explorerUrl = GetExplorerUrl(payment.Network, payment.TransactionHash);
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

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Wallet Address:").Bold();
                row.RelativeItem().Text(TruncateHash(payment.WalletAddress)).AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Confirmations:").Bold();
                row.RelativeItem().Text($"{payment.Confirmations}/{payment.RequiredConfirmations}").AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Purpose:").Bold();
                row.RelativeItem().Text(FormatPurpose(payment.Purpose)).AlignRight();
            });

            if (!string.IsNullOrEmpty(payment.Plan))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Plan:").Bold();
                    row.RelativeItem().Text(payment.Plan.ToUpper()).AlignRight();
                });
            }

            // Amount Section
            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text("Amount Paid (Crypto):").Bold();
                row.RelativeItem().Text($"{payment.AmountCrypto:F6} {payment.Currency}").AlignRight();
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Exchange Rate:").Bold();
                row.RelativeItem().Text($"1 {payment.Currency} = ${payment.ExchangeRate:F4} USD").AlignRight();
            });

            column.Item().PaddingTop(10).Background(Colors.Green.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Text("TOTAL AMOUNT (USD):").Bold().FontSize(14);
                row.RelativeItem().Text($"${payment.AmountUsd:F2}")
                    .Bold()
                    .FontSize(14)
                    .AlignRight();
            });

            // Tax Deductibility Notice
            column.Item().PaddingTop(30).Background(Colors.Yellow.Lighten3).Padding(15).Column(noticeColumn =>
            {
                noticeColumn.Item().Text("IMPORTANT TAX NOTICE").Bold().FontSize(12);
                noticeColumn.Item().PaddingTop(5).Text(_config.DefaultReceiptNotes).FontSize(9);
            });

            // Status
            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Text("Payment Status:").Bold();
                row.RelativeItem().Text(payment.Status.ToUpper())
                    .FontColor(payment.Status == "completed" ? Colors.Green.Medium : Colors.Orange.Medium)
                    .Bold()
                    .AlignRight();
            });
        });
    }

    private string FormatPurpose(string? purpose)
    {
        if (string.IsNullOrEmpty(purpose)) return "General Payment";

        return purpose switch
        {
            "donation" => "Donation / Support",
            "premium_subscription" => "Premium Subscription",
            "purchase" => "Purchase",
            _ => purpose.Replace("_", " ").ToUpper()
        };
    }
}
