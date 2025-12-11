# Crypto Payment Logging Configuration

## ?? Goal
Show **ONLY** cryptocurrency payment-related logs, suppress everything else.

---

## ? Quick Solution: Update appsettings.json

Replace your `Logging` section in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      // Suppress all general logs
      "Default": "None",
      "Microsoft": "None",
      "Microsoft.AspNetCore": "None",
      "Microsoft.EntityFrameworkCore": "None",
      "Microsoft.Hosting.Lifetime": "None",
      "System": "None",
      "Hangfire": "None",
      
      // ? Enable ONLY crypto payment logs
      "Wihngo.Services.CryptoPaymentService": "Information",
      "Wihngo.Services.BlockchainVerificationService": "Information",
      "Wihngo.Controllers.CryptoPaymentsController": "Information",
      "Wihngo.BackgroundJobs.ExchangeRateUpdateJob": "Information",
      "Wihngo.BackgroundJobs.PaymentMonitorJob": "Information"
    }
  }
}
```

---

## ?? Alternative: Add to Program.cs

Add this code **before** `var app = builder.Build();`:

```csharp
// Configure crypto-only logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft", LogLevel.None);
builder.Logging.AddFilter("System", LogLevel.None);
builder.Logging.AddFilter("Hangfire", LogLevel.None);

// Enable crypto payment logs only
builder.Logging.AddFilter("Wihngo.Services.CryptoPaymentService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.BlockchainVerificationService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.CryptoPaymentsController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.BackgroundJobs.ExchangeRateUpdateJob", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.BackgroundJobs.PaymentMonitorJob", LogLevel.Information);
```

---

## ?? Advanced: Color-Coded Crypto Logs

For better visibility, add this to Program.cs **before** `var app = builder.Build();`:

```csharp
// Custom crypto payment logger with colors
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "[HH:mm:ss] ";
    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
});

// Filter to show only crypto logs
builder.Logging.AddFilter((category, level) =>
{
    var cryptoCategories = new[]
    {
        "Wihngo.Services.CryptoPaymentService",
        "Wihngo.Services.BlockchainVerificationService",
        "Wihngo.Controllers.CryptoPaymentsController",
        "Wihngo.BackgroundJobs.ExchangeRateUpdateJob",
        "Wihngo.BackgroundJobs.PaymentMonitorJob"
    };

    return cryptoCategories.Any(c => category?.StartsWith(c) == true);
});
```

---

## ?? Expected Output

After applying any of the above solutions, you'll see **ONLY**:

```
[10:30:45] [INFO] [CRYPTO] Creating payment request for $11.00
[10:30:45] [INFO] [CRYPTO] Currency: ETH, Network: sepolia
[10:30:46] [INFO] [CRYPTO] Payment request created: payment-id-123
[10:30:50] [INFO] [CRYPTO] Verifying transaction: 0xabc123...
[10:30:51] [INFO] [CRYPTO] Transaction found on Sepolia: 2/6 confirmations
[10:31:15] [INFO] [CRYPTO] Payment confirmed: 6/6 confirmations
[10:31:15] [INFO] [CRYPTO] Premium subscription activated for bird-id
```

No more Hangfire, Entity Framework, or other framework logs! ??

---

## ?? Quick Implementation Steps

### Option 1: Modify appsettings.json (Recommended)

1. Open `appsettings.Development.json`
2. Replace the `Logging` section with the one above
3. Save and restart your backend
4. Done! ?

### Option 2: Add to Program.cs

1. Open `Program.cs`
2. Find the line `var builder = WebApplication.CreateBuilder(args);`
3. Add the logging configuration code after it
4. Save and restart
5. Done! ?

---

## ?? What Gets Logged?

| Component | What You'll See |
|-----------|----------------|
| **CryptoPaymentService** | Payment creation, validation, wallet lookups |
| **BlockchainVerificationService** | Transaction verification, confirmations |
| **CryptoPaymentsController** | API requests, responses, errors |
| **ExchangeRateUpdateJob** | Rate updates from CoinGecko |
| **PaymentMonitorJob** | Payment status checks, confirmations |

---

## ?? What Gets Suppressed?

- ? Entity Framework queries
- ? Hangfire job scheduling
- ? ASP.NET Core middleware logs
- ? System and framework logs
- ? General application logs

---

## ?? Debugging: Enable Specific Logs Temporarily

If you need to debug a specific issue, temporarily add:

```csharp
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
```

Or in appsettings.json:

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

---

## ?? Log Levels Explained

| Level | When to Use |
|-------|-------------|
| `None` | Suppress completely |
| `Error` | Only errors |
| `Warning` | Warnings and errors |
| `Information` | Normal flow (recommended for crypto) |
| `Debug` | Detailed debugging |
| `Trace` | Everything (verbose) |

---

## ? Verification

After applying the changes, test with a Sepolia payment:

1. Create a payment request
2. Check console - should see ONLY crypto logs
3. Verify payment
4. Check console - should see verification logs

**No other logs should appear!** ??

---

**Restart your backend after making changes for the configuration to take effect.**
