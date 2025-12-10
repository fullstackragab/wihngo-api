# Backend Crypto Payment Implementation Guide (.NET)

**For**: GitHub Copilot / Claude Sonnet 4.5  
**Project**: Wihngo - Bird Premium Subscriptions  
**Backend**: ASP.NET Core Web API (.NET 8.0)  
**Date**: December 10, 2025

---

## 🎯 Implementation Overview

Implement a complete cryptocurrency payment backend for Wihngo using ASP.NET Core that:

1. Accepts USDT payments on TRON network (primary)
2. Supports Bitcoin, Ethereum, and other cryptocurrencies
3. Verifies transactions on blockchain
4. Activates premium subscriptions upon payment confirmation
5. Manages exchange rates and wallet addresses

**Primary Wallet**:

- **Network**: TRON Mainnet
- **Currency**: USDT (TRC-20)
- **Address**: `TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA`

---

## 📋 Project Setup

### 1. Create New Web API Project

```bash
dotnet new webapi -n Wihngo.Api
cd Wihngo.Api
```

### 2. Install NuGet Packages

```bash
# Entity Framework Core & PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools

# HTTP Client & JSON
dotnet add package Newtonsoft.Json
dotnet add package System.Net.Http.Json

# Background Jobs (Hangfire)
dotnet add package Hangfire.Core
dotnet add package Hangfire.PostgreSql
dotnet add package Hangfire.AspNetCore

# Blockchain Libraries
dotnet add package Nethereum.Web3
dotnet add package NBitcoin

# Authentication & Security
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt

# Utilities
dotnet add package BCrypt.Net-Next
```

### 3. Project Structure

```
Wihngo.Api/
├── Controllers/
│   └── CryptoPaymentController.cs
├── Services/
│   ├── Interfaces/
│   │   ├── ICryptoPaymentService.cs
│   │   ├── IBlockchainService.cs
│   │   └── IExchangeRateService.cs
│   ├── CryptoPaymentService.cs
│   ├── TronBlockchainService.cs
│   ├── EthereumBlockchainService.cs
│   └── ExchangeRateService.cs
├── Models/
│   ├── Entities/
│   │   ├── PlatformWallet.cs
│   │   ├── CryptoPaymentRequest.cs
│   │   ├── CryptoTransaction.cs
│   │   ├── CryptoExchangeRate.cs
│   │   └── CryptoPaymentMethod.cs
│   ├── DTOs/
│   │   ├── CreatePaymentRequestDto.cs
│   │   ├── PaymentResponseDto.cs
│   │   ├── VerifyPaymentDto.cs
│   │   └── ExchangeRateDto.cs
│   └── Enums/
│       ├── CryptoCurrency.cs
│       ├── CryptoNetwork.cs
│       └── PaymentStatus.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── BackgroundJobs/
│   ├── ExchangeRateUpdateJob.cs
│   └── PaymentMonitorJob.cs
├── Helpers/
│   └── WalletValidator.cs
├── appsettings.json
└── Program.cs
```

---

## 🗄️ Database Models (Entity Framework)

### 1. Enums

**Models/Enums/CryptoCurrency.cs**

```csharp
namespace Wihngo.Api.Models.Enums;

public enum CryptoCurrency
{
    BTC,
    ETH,
    USDT,
    USDC,
    BNB,
    SOL,
    DOGE
}
```

**Models/Enums/CryptoNetwork.cs**

```csharp
namespace Wihngo.Api.Models.Enums;

public enum CryptoNetwork
{
    Bitcoin,
    Ethereum,
    Tron,
    BinanceSmartChain,
    Polygon,
    Solana
}
```

**Models/Enums/PaymentStatus.cs**

```csharp
namespace Wihngo.Api.Models.Enums;

public enum PaymentStatus
{
    Pending,
    Confirming,
    Confirmed,
    Completed,
    Expired,
    Failed,
    Refunded
}
```

### 2. Entity Models

**Models/Entities/PlatformWallet.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wihngo.Api.Models.Enums;

namespace Wihngo.Api.Models.Entities;

[Table("platform_wallets")]
public class PlatformWallet
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Required]
    [Column("address")]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [Column("private_key_encrypted")]
    public string? PrivateKeyEncrypted { get; set; }

    [Column("derivation_path")]
    [MaxLength(100)]
    public string? DerivationPath { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Models/Entities/CryptoPaymentRequest.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wihngo.Api.Models.Enums;

namespace Wihngo.Api.Models.Entities;

[Table("crypto_payment_requests")]
public class CryptoPaymentRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("bird_id")]
    public Guid? BirdId { get; set; }

    [Required]
    [Column("amount_usd")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountUsd { get; set; }

    [Required]
    [Column("amount_crypto")]
    [Column(TypeName = "decimal(20,10)")]
    public decimal AmountCrypto { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Required]
    [Column("exchange_rate")]
    [Column(TypeName = "decimal(20,2)")]
    public decimal ExchangeRate { get; set; }

    [Required]
    [Column("wallet_address")]
    [MaxLength(255)]
    public string WalletAddress { get; set; } = string.Empty;

    [Column("user_wallet_address")]
    [MaxLength(255)]
    public string? UserWalletAddress { get; set; }

    [Required]
    [Column("qr_code_data")]
    public string QrCodeData { get; set; } = string.Empty;

    [Required]
    [Column("payment_uri")]
    public string PaymentUri { get; set; } = string.Empty;

    [Column("transaction_hash")]
    [MaxLength(255)]
    public string? TransactionHash { get; set; }

    [Column("confirmations")]
    public int Confirmations { get; set; } = 0;

    [Required]
    [Column("required_confirmations")]
    public int RequiredConfirmations { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Required]
    [Column("purpose")]
    [MaxLength(50)]
    public string Purpose { get; set; } = string.Empty;

    [Column("plan")]
    [MaxLength(20)]
    public string? Plan { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Models/Entities/CryptoTransaction.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Api.Models.Entities;

[Table("crypto_transactions")]
public class CryptoTransaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("payment_request_id")]
    public Guid PaymentRequestId { get; set; }

    [Required]
    [Column("transaction_hash")]
    [MaxLength(255)]
    public string TransactionHash { get; set; } = string.Empty;

    [Required]
    [Column("from_address")]
    [MaxLength(255)]
    public string FromAddress { get; set; } = string.Empty;

    [Required]
    [Column("to_address")]
    [MaxLength(255)]
    public string ToAddress { get; set; } = string.Empty;

    [Required]
    [Column("amount")]
    [Column(TypeName = "decimal(20,10)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Column("confirmations")]
    public int Confirmations { get; set; } = 0;

    [Column("block_number")]
    public long? BlockNumber { get; set; }

    [Column("block_hash")]
    [MaxLength(255)]
    public string? BlockHash { get; set; }

    [Column("fee")]
    [Column(TypeName = "decimal(20,10)")]
    public decimal? Fee { get; set; }

    [Column("gas_used")]
    public long? GasUsed { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Column("raw_transaction", TypeName = "jsonb")]
    public string? RawTransaction { get; set; }

    [Column("detected_at")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [ForeignKey("PaymentRequestId")]
    public CryptoPaymentRequest? PaymentRequest { get; set; }
}
```

**Models/Entities/CryptoExchangeRate.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Api.Models.Entities;

[Table("crypto_exchange_rates")]
public class CryptoExchangeRate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("usd_rate")]
    [Column(TypeName = "decimal(20,2)")]
    public decimal UsdRate { get; set; }

    [Required]
    [Column("source")]
    [MaxLength(50)]
    public string Source { get; set; } = "coingecko";

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
```

**Models/Entities/CryptoPaymentMethod.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wihngo.Api.Models.Entities;

[Table("crypto_payment_methods")]
public class CryptoPaymentMethod
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("wallet_address")]
    [MaxLength(255)]
    public string WalletAddress { get; set; } = string.Empty;

    [Required]
    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    [Column("label")]
    [MaxLength(100)]
    public string? Label { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("verified")]
    public bool Verified { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 3. DTOs

**Models/DTOs/CreatePaymentRequestDto.cs**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Wihngo.Api.Models.DTOs;

public class CreatePaymentRequestDto
{
    public Guid? BirdId { get; set; }

    [Required]
    [Range(5, 100000)]
    public decimal AmountUsd { get; set; }

    [Required]
    [RegularExpression("^(BTC|ETH|USDT|USDC|BNB|SOL|DOGE)$")]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(bitcoin|ethereum|tron|binance-smart-chain|polygon|solana)$")]
    public string Network { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(premium_subscription|donation|purchase)$")]
    public string Purpose { get; set; } = string.Empty;

    [RegularExpression("^(monthly|yearly|lifetime)$")]
    public string? Plan { get; set; }
}
```

**Models/DTOs/PaymentResponseDto.cs**

```csharp
namespace Wihngo.Api.Models.DTOs;

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? BirdId { get; set; }
    public decimal AmountUsd { get; set; }
    public decimal AmountCrypto { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public string? UserWalletAddress { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
    public string PaymentUri { get; set; } = string.Empty;
    public string? TransactionHash { get; set; }
    public int Confirmations { get; set; }
    public int RequiredConfirmations { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Plan { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Models/DTOs/VerifyPaymentDto.cs**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Wihngo.Api.Models.DTOs;

public class VerifyPaymentDto
{
    [Required]
    [MinLength(32)]
    public string TransactionHash { get; set; } = string.Empty;

    public string? UserWalletAddress { get; set; }
}
```

**Models/DTOs/ExchangeRateDto.cs**

```csharp
namespace Wihngo.Api.Models.DTOs;

public class ExchangeRateDto
{
    public string Currency { get; set; } = string.Empty;
    public decimal UsdRate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Source { get; set; } = string.Empty;
}
```

---

## 🗄️ Database Context

**Data/ApplicationDbContext.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Wihngo.Api.Models.Entities;

namespace Wihngo.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformWallet> PlatformWallets { get; set; }
    public DbSet<CryptoPaymentRequest> CryptoPaymentRequests { get; set; }
    public DbSet<CryptoTransaction> CryptoTransactions { get; set; }
    public DbSet<CryptoExchangeRate> CryptoExchangeRates { get; set; }
    public DbSet<CryptoPaymentMethod> CryptoPaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique constraints
        modelBuilder.Entity<PlatformWallet>()
            .HasIndex(w => new { w.Currency, w.Network, w.Address })
            .IsUnique();

        modelBuilder.Entity<CryptoExchangeRate>()
            .HasIndex(r => r.Currency)
            .IsUnique();

        modelBuilder.Entity<CryptoPaymentMethod>()
            .HasIndex(m => new { m.UserId, m.WalletAddress, m.Currency, m.Network })
            .IsUnique();

        modelBuilder.Entity<CryptoTransaction>()
            .HasIndex(t => t.TransactionHash)
            .IsUnique();

        // Indexes
        modelBuilder.Entity<CryptoPaymentRequest>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<CryptoPaymentRequest>()
            .HasIndex(p => p.TransactionHash);

        modelBuilder.Entity<CryptoPaymentRequest>()
            .HasIndex(p => p.ExpiresAt);

        // Seed initial data
        modelBuilder.Entity<PlatformWallet>().HasData(
            new PlatformWallet
            {
                Id = Guid.NewGuid(),
                Currency = "USDT",
                Network = "tron",
                Address = "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        modelBuilder.Entity<CryptoExchangeRate>().HasData(
            new CryptoExchangeRate { Id = Guid.NewGuid(), Currency = "BTC", UsdRate = 50000, Source = "coingecko" },
            new CryptoExchangeRate { Id = Guid.NewGuid(), Currency = "ETH", UsdRate = 3000, Source = "coingecko" },
            new CryptoExchangeRate { Id = Guid.NewGuid(), Currency = "USDT", UsdRate = 1, Source = "coingecko" },
            new CryptoExchangeRate { Id = Guid.NewGuid(), Currency = "USDC", UsdRate = 1, Source = "coingecko" },
            new CryptoExchangeRate { Id = Guid.NewGuid(), Currency = "BNB", UsdRate = 500, Source = "coingecko" },
            new CryptoExchangeRate { Id = Guid.NewGuid(), Currency = "SOL", UsdRate = 100, Source = "coingecko" },
            new CryptoExchangeRate { Id = Guid.NewGuid(), Currency = "DOGE", UsdRate = 0.1m, Source = "coingecko" }
        );
    }
}
```

---

## ⚙️ Configuration

**appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=wihngo;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-jwt-key-minimum-32-characters",
    "Issuer": "Wihngo.Api",
    "Audience": "Wihngo.Client",
    "ExpirationDays": 7
  },
  "BlockchainSettings": {
    "TronGrid": {
      "ApiUrl": "https://api.trongrid.io",
      "ApiKey": "your-trongrid-api-key"
    },
    "Infura": {
      "ProjectId": "your-infura-project-id",
      "ProjectSecret": "your-infura-secret"
    },
    "Alchemy": {
      "ApiKey": "your-alchemy-api-key"
    }
  },
  "ExchangeRateSettings": {
    "CoinGeckoApiKey": "your-coingecko-api-key",
    "UpdateIntervalMinutes": 5
  },
  "PaymentSettings": {
    "ExpirationMinutes": 30,
    "MinPaymentAmountUsd": 5.0
  },
  "Hangfire": {
    "DashboardPath": "/hangfire",
    "DashboardUsername": "admin",
    "DashboardPassword": "your-secure-password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=wihngo_dev;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

---

## 🔧 Services Implementation

### 1. Crypto Payment Service Interface

**Services/Interfaces/ICryptoPaymentService.cs**

```csharp
using Wihngo.Api.Models.DTOs;
using Wihngo.Api.Models.Entities;

namespace Wihngo.Api.Services.Interfaces;

public interface ICryptoPaymentService
{
    Task<PaymentResponseDto> CreatePaymentRequestAsync(Guid userId, CreatePaymentRequestDto dto);
    Task<PaymentResponseDto?> GetPaymentRequestAsync(Guid paymentId, Guid userId);
    Task<PaymentResponseDto> VerifyPaymentAsync(Guid paymentId, Guid userId, VerifyPaymentDto dto);
    Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(Guid userId, int page, int pageSize);
    Task<PlatformWallet?> GetPlatformWalletAsync(string currency, string network);
    Task CompletePaymentAsync(CryptoPaymentRequest payment);
}
```

### 2. Crypto Payment Service

**Services/CryptoPaymentService.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Wihngo.Api.Data;
using Wihngo.Api.Models.DTOs;
using Wihngo.Api.Models.Entities;
using Wihngo.Api.Services.Interfaces;

namespace Wihngo.Api.Services;

public class CryptoPaymentService : ICryptoPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IBlockchainService _blockchainService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CryptoPaymentService> _logger;

    public CryptoPaymentService(
        ApplicationDbContext context,
        IBlockchainService blockchainService,
        IConfiguration configuration,
        ILogger<CryptoPaymentService> logger)
    {
        _context = context;
        _blockchainService = blockchainService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> CreatePaymentRequestAsync(Guid userId, CreatePaymentRequestDto dto)
    {
        // Validate minimum amount
        var minAmount = _configuration.GetValue<decimal>("PaymentSettings:MinPaymentAmountUsd", 5m);
        if (dto.AmountUsd < minAmount)
        {
            throw new InvalidOperationException($"Minimum payment amount is ${minAmount}");
        }

        // Get exchange rate
        var rate = await _context.CryptoExchangeRates
            .FirstOrDefaultAsync(r => r.Currency == dto.Currency.ToUpper());

        if (rate == null)
        {
            throw new InvalidOperationException($"Exchange rate not available for {dto.Currency}");
        }

        // Calculate crypto amount
        var amountCrypto = dto.AmountUsd / rate.UsdRate;

        // Get platform wallet
        var wallet = await GetPlatformWalletAsync(dto.Currency, dto.Network);
        if (wallet == null)
        {
            throw new InvalidOperationException($"No wallet configured for {dto.Currency} on {dto.Network}");
        }

        // Get required confirmations
        var requiredConfirmations = GetRequiredConfirmations(dto.Network);

        // Generate payment URI
        var paymentUri = GeneratePaymentUri(dto.Currency, dto.Network, wallet.Address, amountCrypto);
        var qrCodeData = paymentUri;

        // Calculate expiration
        var expirationMinutes = _configuration.GetValue<int>("PaymentSettings:ExpirationMinutes", 30);
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Create payment request
        var payment = new CryptoPaymentRequest
        {
            UserId = userId,
            BirdId = dto.BirdId,
            AmountUsd = dto.AmountUsd,
            AmountCrypto = amountCrypto,
            Currency = dto.Currency.ToUpper(),
            Network = dto.Network.ToLower(),
            ExchangeRate = rate.UsdRate,
            WalletAddress = wallet.Address,
            QrCodeData = qrCodeData,
            PaymentUri = paymentUri,
            RequiredConfirmations = requiredConfirmations,
            Status = "pending",
            Purpose = dto.Purpose,
            Plan = dto.Plan,
            ExpiresAt = expiresAt
        };

        _context.CryptoPaymentRequests.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Payment request {payment.Id} created for user {userId}");

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetPaymentRequestAsync(Guid paymentId, Guid userId)
    {
        var payment = await _context.CryptoPaymentRequests
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

        if (payment == null)
        {
            return null;
        }

        // Check if expired
        if (payment.Status == "pending" && DateTime.UtcNow > payment.ExpiresAt)
        {
            payment.Status = "expired";
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto> VerifyPaymentAsync(Guid paymentId, Guid userId, VerifyPaymentDto dto)
    {
        var payment = await _context.CryptoPaymentRequests
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

        if (payment == null)
        {
            throw new InvalidOperationException("Payment not found");
        }

        if (payment.Status != "pending" && payment.Status != "confirming")
        {
            throw new InvalidOperationException($"Payment already {payment.Status}");
        }

        // Verify transaction on blockchain
        var txInfo = await _blockchainService.VerifyTransactionAsync(
            dto.TransactionHash,
            payment.Currency,
            payment.Network
        );

        if (txInfo == null)
        {
            throw new InvalidOperationException("Transaction not found on blockchain");
        }

        // Verify amount (allow 1% tolerance)
        var minAmount = payment.AmountCrypto * 0.99m;
        if (txInfo.Amount < minAmount)
        {
            throw new InvalidOperationException(
                $"Incorrect amount. Expected {payment.AmountCrypto}, received {txInfo.Amount}");
        }

        // Verify recipient address
        if (!txInfo.ToAddress.Equals(payment.WalletAddress, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Incorrect recipient address");
        }

        // Update payment
        payment.TransactionHash = dto.TransactionHash;
        payment.UserWalletAddress = dto.UserWalletAddress ?? txInfo.FromAddress;
        payment.Confirmations = txInfo.Confirmations;
        payment.Status = txInfo.Confirmations >= payment.RequiredConfirmations ? "confirmed" : "confirming";
        payment.UpdatedAt = DateTime.UtcNow;

        if (payment.Status == "confirmed" && payment.ConfirmedAt == null)
        {
            payment.ConfirmedAt = DateTime.UtcNow;
        }

        // Create transaction record
        var transaction = new CryptoTransaction
        {
            PaymentRequestId = payment.Id,
            TransactionHash = dto.TransactionHash,
            FromAddress = txInfo.FromAddress,
            ToAddress = txInfo.ToAddress,
            Amount = txInfo.Amount,
            Currency = payment.Currency,
            Network = payment.Network,
            Confirmations = txInfo.Confirmations,
            BlockNumber = txInfo.BlockNumber,
            BlockHash = txInfo.BlockHash,
            Fee = txInfo.Fee,
            Status = txInfo.Confirmations >= payment.RequiredConfirmations ? "confirmed" : "pending"
        };

        _context.CryptoTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Complete payment if confirmed
        if (payment.Status == "confirmed")
        {
            await CompletePaymentAsync(payment);
        }

        _logger.LogInformation($"Payment {payment.Id} verified with tx {dto.TransactionHash}");

        return MapToDto(payment);
    }

    public async Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(Guid userId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var payments = await _context.CryptoPaymentRequests
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return payments.Select(MapToDto).ToList();
    }

    public async Task<PlatformWallet?> GetPlatformWalletAsync(string currency, string network)
    {
        return await _context.PlatformWallets
            .Where(w => w.Currency == currency.ToUpper() &&
                       w.Network == network.ToLower() &&
                       w.IsActive)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task CompletePaymentAsync(CryptoPaymentRequest payment)
    {
        try
        {
            payment.Status = "completed";
            payment.CompletedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            if (payment.Purpose == "premium_subscription" && payment.BirdId != null)
            {
                await ActivatePremiumSubscriptionAsync(payment);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Payment {payment.Id} completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to complete payment {payment.Id}");
            payment.Status = "failed";
            await _context.SaveChangesAsync();
        }
    }

    private async Task ActivatePremiumSubscriptionAsync(CryptoPaymentRequest payment)
    {
        // TODO: Implement premium subscription activation
        // This should update the bird's premium status and create subscription record
        _logger.LogInformation($"Premium activated for bird {payment.BirdId}");
        await Task.CompletedTask;
    }

    private int GetRequiredConfirmations(string network)
    {
        return network.ToLower() switch
        {
            "tron" => 19,
            "ethereum" => 12,
            "bitcoin" => 2,
            "binance-smart-chain" => 15,
            "polygon" => 128,
            "solana" => 32,
            _ => 6
        };
    }

    private string GeneratePaymentUri(string currency, string network, string address, decimal amount)
    {
        return network.ToLower() switch
        {
            "tron" => address,
            "ethereum" or "polygon" or "binance-smart-chain" => $"ethereum:{address}",
            "bitcoin" => $"bitcoin:{address}?amount={amount}",
            "solana" => $"solana:{address}?amount={amount}",
            _ => address
        };
    }

    private PaymentResponseDto MapToDto(CryptoPaymentRequest payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            BirdId = payment.BirdId,
            AmountUsd = payment.AmountUsd,
            AmountCrypto = payment.AmountCrypto,
            Currency = payment.Currency,
            Network = payment.Network,
            ExchangeRate = payment.ExchangeRate,
            WalletAddress = payment.WalletAddress,
            UserWalletAddress = payment.UserWalletAddress,
            QrCodeData = payment.QrCodeData,
            PaymentUri = payment.PaymentUri,
            TransactionHash = payment.TransactionHash,
            Confirmations = payment.Confirmations,
            RequiredConfirmations = payment.RequiredConfirmations,
            Status = payment.Status,
            Purpose = payment.Purpose,
            Plan = payment.Plan,
            ExpiresAt = payment.ExpiresAt,
            ConfirmedAt = payment.ConfirmedAt,
            CompletedAt = payment.CompletedAt,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}
```

### 3. Blockchain Service Interface

**Services/Interfaces/IBlockchainService.cs**

```csharp
namespace Wihngo.Api.Services.Interfaces;

public class TransactionInfo
{
    public decimal Amount { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public int Confirmations { get; set; }
    public long? BlockNumber { get; set; }
    public string? BlockHash { get; set; }
    public decimal? Fee { get; set; }
}

public interface IBlockchainService
{
    Task<TransactionInfo?> VerifyTransactionAsync(string txHash, string currency, string network);
}
```

### 4. Blockchain Verification Service

**Services/BlockchainVerificationService.cs**

```csharp
using System.Numerics;
using System.Text.Json;
using Nethereum.Web3;
using Wihngo.Api.Services.Interfaces;

namespace Wihngo.Api.Services;

public class BlockchainVerificationService : IBlockchainService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlockchainVerificationService> _logger;

    public BlockchainVerificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BlockchainVerificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TransactionInfo?> VerifyTransactionAsync(string txHash, string currency, string network)
    {
        try
        {
            return network.ToLower() switch
            {
                "tron" => await VerifyTronTransactionAsync(txHash, currency),
                "ethereum" or "polygon" or "binance-smart-chain" =>
                    await VerifyEvmTransactionAsync(txHash, currency, network),
                "bitcoin" => await VerifyBitcoinTransactionAsync(txHash),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying transaction {txHash} on {network}");
            return null;
        }
    }

    private async Task<TransactionInfo?> VerifyTronTransactionAsync(string txHash, string currency)
    {
        try
        {
            var apiUrl = _configuration["BlockchainSettings:TronGrid:ApiUrl"];
            var apiKey = _configuration["BlockchainSettings:TronGrid:ApiKey"];
            var client = _httpClientFactory.CreateClient();

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", apiKey);
            }

            // Get transaction
            var txResponse = await client.GetAsync($"{apiUrl}/wallet/gettransactionbyid?value={txHash}");
            if (!txResponse.IsSuccessStatusCode) return null;

            var txJson = await txResponse.Content.ReadAsStringAsync();
            var tx = JsonDocument.Parse(txJson);

            // Get transaction info
            var txInfoResponse = await client.GetAsync($"{apiUrl}/wallet/gettransactioninfobyid?value={txHash}");
            if (!txInfoResponse.IsSuccessStatusCode) return null;

            var txInfoJson = await txInfoResponse.Content.ReadAsStringAsync();
            var txInfo = JsonDocument.Parse(txInfoJson);

            // Check if successful
            if (txInfo.RootElement.TryGetProperty("receipt", out var receipt))
            {
                if (receipt.TryGetProperty("result", out var result) &&
                    result.GetString() != "SUCCESS")
                {
                    return null;
                }
            }

            decimal amount = 0;
            string toAddress = "";
            string fromAddress = "";

            if (currency == "USDT")
            {
                // TRC-20 USDT
                if (txInfo.RootElement.TryGetProperty("log", out var logs) && logs.GetArrayLength() > 0)
                {
                    var log = logs[0];
                    var data = log.GetProperty("data").GetString() ?? "";
                    var topics = log.GetProperty("topics").EnumerateArray().ToList();

                    // Decode amount (6 decimals)
                    amount = Convert.ToDecimal(BigInteger.Parse(data, System.Globalization.NumberStyles.HexNumber)) / 1_000_000m;

                    // Decode addresses
                    if (topics.Count >= 3)
                    {
                        toAddress = TronAddressFromHex("41" + topics[2].GetString()?.Substring(24));
                        fromAddress = TronAddressFromHex("41" + topics[1].GetString()?.Substring(24));
                    }
                }
            }

            // Get confirmations
            var currentBlockResponse = await client.GetAsync($"{apiUrl}/wallet/getnowblock");
            var currentBlockJson = await currentBlockResponse.Content.ReadAsStringAsync();
            var currentBlock = JsonDocument.Parse(currentBlockJson);

            var currentBlockNumber = currentBlock.RootElement
                .GetProperty("block_header")
                .GetProperty("raw_data")
                .GetProperty("number")
                .GetInt64();

            long? blockNumber = null;
            if (txInfo.RootElement.TryGetProperty("blockNumber", out var blockNumProp))
            {
                blockNumber = blockNumProp.GetInt64();
            }

            var confirmations = blockNumber.HasValue ? (int)(currentBlockNumber - blockNumber.Value + 1) : 0;

            return new TransactionInfo
            {
                Amount = amount,
                ToAddress = toAddress,
                FromAddress = fromAddress,
                Confirmations = confirmations,
                BlockNumber = blockNumber,
                BlockHash = txInfo.RootElement.TryGetProperty("blockHash", out var hash) ? hash.GetString() : null,
                Fee = txInfo.RootElement.TryGetProperty("fee", out var fee) ? fee.GetDecimal() / 1_000_000m : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"TRON verification error for {txHash}");
            return null;
        }
    }

    private async Task<TransactionInfo?> VerifyEvmTransactionAsync(string txHash, string currency, string network)
    {
        try
        {
            string rpcUrl = network.ToLower() switch
            {
                "ethereum" => $"https://mainnet.infura.io/v3/{_configuration["BlockchainSettings:Infura:ProjectId"]}",
                "binance-smart-chain" => "https://bsc-dataseed.binance.org",
                "polygon" => "https://polygon-rpc.com",
                _ => throw new NotSupportedException($"Network {network} not supported")
            };

            var web3 = new Web3(rpcUrl);
            var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);

            if (tx == null || receipt == null) return null;

            var currentBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var confirmations = (int)(currentBlock.Value - receipt.BlockNumber.Value + 1);

            decimal amount = 0;
            string toAddress = tx.To;
            string fromAddress = tx.From;

            if (currency is "USDT" or "USDC")
            {
                // ERC-20 token - parse Transfer event
                var transferLog = receipt.Logs.FirstOrDefault(log =>
                    log.Topics.Length >= 3 &&
                    log.Topics[0].ToString().StartsWith("0xddf252ad")); // Transfer event signature

                if (transferLog != null)
                {
                    var amountHex = transferLog.Data;
                    amount = (decimal)BigInteger.Parse(amountHex.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber) / 1_000_000m;

                    toAddress = "0x" + transferLog.Topics[2].ToString().Substring(26);
                    fromAddress = "0x" + transferLog.Topics[1].ToString().Substring(26);
                }
            }
            else
            {
                // Native ETH/BNB
                amount = Web3.Convert.FromWei(tx.Value.Value);
            }

            var gasUsed = receipt.GasUsed.Value;
            var gasPrice = tx.GasPrice.Value;
            var fee = Web3.Convert.FromWei(gasUsed * gasPrice);

            return new TransactionInfo
            {
                Amount = amount,
                ToAddress = toAddress,
                FromAddress = fromAddress,
                Confirmations = confirmations,
                BlockNumber = (long)receipt.BlockNumber.Value,
                BlockHash = receipt.BlockHash,
                Fee = fee
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"EVM verification error for {txHash}");
            return null;
        }
    }

    private async Task<TransactionInfo?> VerifyBitcoinTransactionAsync(string txHash)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://blockchain.info/rawtx/{txHash}");

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var tx = JsonDocument.Parse(json);

            var latestBlockResponse = await client.GetAsync("https://blockchain.info/latestblock");
            var latestBlockJson = await latestBlockResponse.Content.ReadAsStringAsync();
            var latestBlock = JsonDocument.Parse(latestBlockJson);
            var currentHeight = latestBlock.RootElement.GetProperty("height").GetInt64();

            long? blockHeight = null;
            if (tx.RootElement.TryGetProperty("block_height", out var blockProp))
            {
                blockHeight = blockProp.GetInt64();
            }

            var confirmations = blockHeight.HasValue ? (int)(currentHeight - blockHeight.Value + 1) : 0;

            var outputs = tx.RootElement.GetProperty("out").EnumerateArray().ToList();
            var inputs = tx.RootElement.GetProperty("inputs").EnumerateArray().ToList();

            return new TransactionInfo
            {
                Amount = outputs[0].GetProperty("value").GetDecimal() / 100_000_000m,
                ToAddress = outputs[0].GetProperty("addr").GetString() ?? "",
                FromAddress = inputs[0].GetProperty("prev_out").GetProperty("addr").GetString() ?? "",
                Confirmations = confirmations,
                BlockNumber = blockHeight,
                Fee = tx.RootElement.GetProperty("fee").GetDecimal() / 100_000_000m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Bitcoin verification error for {txHash}");
            return null;
        }
    }

    private string TronAddressFromHex(string hex)
    {
        // Simplified TRON address conversion
        // In production, use proper TRON SDK
        return "T" + hex; // Placeholder
    }
}
```

**(Continue in next message due to length...)**
