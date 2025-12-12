namespace Wihngo.Configuration;

public class InvoiceConfiguration
{
    public string CompanyName { get; set; } = "Wihngo Inc.";
    public string CompanyAddress { get; set; } = "123 Main Street, Suite 100, San Francisco, CA 94105, USA";
    public string TaxNumber { get; set; } = string.Empty; // Optional tax ID
    public string ContactEmail { get; set; } = "support@wihngo.com";
    public string SupportPhone { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = "https://wihngo.com";
    
    public string InvoicePrefix { get; set; } = "WIH";
    public string DefaultReceiptNotes { get; set; } = 
        "This payment is not a charitable donation. Wihngo is a for-profit company and this payment is not eligible for tax deduction unless otherwise stated.";
    
    public string StoragePath { get; set; } = "invoices";
    public bool UseS3Storage { get; set; } = false;
    public string? S3BucketName { get; set; }
    public string? S3Region { get; set; }
    
    public int InvoiceExpiryMinutes { get; set; } = 30;
}

public class SolanaConfiguration
{
    public string RpcUrl { get; set; } = "https://api.mainnet-beta.solana.com";
    public string MerchantWalletAddress { get; set; } = string.Empty;
    public int ConfirmationBlocks { get; set; } = 32; // finalized
    public int PollingIntervalSeconds { get; set; } = 10;
}

public class BaseConfiguration
{
    public string RpcUrl { get; set; } = "https://mainnet.base.org";
    public string MerchantWalletAddress { get; set; } = string.Empty;
    public string? PaymentReceiverContract { get; set; }
    public int ConfirmationBlocks { get; set; } = 12;
    public int PollingIntervalSeconds { get; set; } = 15;
}

public class PayPalConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Mode { get; set; } = "sandbox"; // sandbox or live
    public string WebhookId { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class SmtpConfiguration
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@wihngo.com";
    public string FromName { get; set; } = "Wihngo";
}
