using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Wihngo.Data;
using System.Text.Json.Serialization;
using Wihngo.Mapping;
using Hangfire;
using Hangfire.PostgreSql;
using Wihngo.Services;
using Wihngo.Services.Interfaces;
using Wihngo.BackgroundJobs;
using Wihngo.Configuration;
using Wihngo.Models.Entities;
using Wihngo.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// ?? CRYPTO-ONLY LOGGING CONFIGURATION
// ========================================
// Clear default providers and configure crypto-payment-only logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Suppress all general framework logs
builder.Logging.AddFilter("Microsoft", LogLevel.None);
builder.Logging.AddFilter("System", LogLevel.None);
// TEMPORARY: Enable Hangfire logs to diagnose dashboard issue
builder.Logging.AddFilter("Hangfire", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

// ? Enable ONLY crypto payment logs
builder.Logging.AddFilter("Wihngo.Services.CryptoPaymentService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.BlockchainVerificationService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.CryptoPaymentsController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.BackgroundJobs.ExchangeRateUpdateJob", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.BackgroundJobs.PaymentMonitorJob", LogLevel.Information);

// ? Enable on-chain deposit monitoring logs
builder.Logging.AddFilter("Wihngo.Services.OnChainDepositService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.OnChainDepositBackgroundService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.EvmBlockchainMonitor", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.SolanaBlockchainMonitor", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.StellarBlockchainMonitor", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.OnChainDepositController", LogLevel.Information);

// ? Enable invoice/payment system logs
builder.Logging.AddFilter("Wihngo.Services.InvoiceService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.PayPalService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.SolanaListenerService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.EvmListenerService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.InvoicesController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.PaymentsController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.WebhooksController", LogLevel.Information);

// ? Enable auth and security logs
builder.Logging.AddFilter("Wihngo.Controllers.AuthController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.TokenService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Middleware.RateLimitingMiddleware", LogLevel.Information);

// ? Enable media/S3 logs
builder.Logging.AddFilter("Wihngo.Services.S3Service", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.MediaController", LogLevel.Information);

// Configure console logging format
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "[HH:mm:ss] ";
    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
});
// ========================================

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    // Prevent circular reference errors when returning EF entities with navigation properties
    opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddOpenApi();

// Register AutoMapper with explicit profile
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());

// Register DbContext using PostgreSQL (Npgsql)
// Read connection string from environment variable (secure) or fallback to appsettings.json
var connectionString = builder.Configuration["DEFAULT_CONNECTION"] 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Database connection string is not configured. Set DEFAULT_CONNECTION environment variable.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

// Log database configuration (without exposing connection details)
var dbLogger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
dbLogger.LogInformation("Database Configuration:");
dbLogger.LogInformation("  Connection: {Status}", string.IsNullOrEmpty(connectionString) ? "NOT SET" : "***configured***");
var dbName = ExtractDatabaseName(connectionString);
if (!string.IsNullOrEmpty(dbName))
{
    dbLogger.LogInformation("  Database: {Database}", dbName);
}

// Helper method to extract database name from connection string
static string? ExtractDatabaseName(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString)) return null;
    
    var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value : null;
}

// Configuration Options
builder.Services.Configure<SecurityConfiguration>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<InvoiceConfiguration>(builder.Configuration.GetSection("Invoice"));
builder.Services.Configure<SolanaConfiguration>(builder.Configuration.GetSection("Solana"));
builder.Services.Configure<Wihngo.Configuration.BaseConfiguration>(builder.Configuration.GetSection("Base"));
builder.Services.Configure<PayPalConfiguration>(builder.Configuration.GetSection("PayPal"));

// SendGrid/SMTP Email Configuration - Read from environment variables or appsettings.json
builder.Services.Configure<SmtpConfiguration>(config =>
{
    // Try to read from environment variables first
    var sendGridApiKey = builder.Configuration["SENDGRID_API_KEY"] 
                         ?? builder.Configuration["Smtp:SendGridApiKey"];
    var provider = builder.Configuration["EMAIL_PROVIDER"] 
                   ?? builder.Configuration["Smtp:Provider"] 
                   ?? "SMTP";
    var host = builder.Configuration["SMTP_HOST"] 
               ?? builder.Configuration["Smtp:Host"] 
               ?? "smtp.sendgrid.net";
    var port = int.TryParse(
        builder.Configuration["SMTP_PORT"] ?? builder.Configuration["Smtp:Port"],
        out var smtpPort) ? smtpPort : 587;
    var useSsl = bool.TryParse(
        builder.Configuration["SMTP_USE_SSL"] ?? builder.Configuration["Smtp:UseSsl"],
        out var ssl) ? ssl : true;
    var username = builder.Configuration["SMTP_USERNAME"] 
                   ?? builder.Configuration["Smtp:Username"] 
                   ?? "apikey"; // SendGrid uses 'apikey' as username
    var password = builder.Configuration["SMTP_PASSWORD"] 
                   ?? builder.Configuration["Smtp:Password"];
    var fromEmail = builder.Configuration["EMAIL_FROM"] 
                    ?? builder.Configuration["Smtp:FromEmail"] 
                    ?? "noreply@wihngo.com";
    var fromName = builder.Configuration["EMAIL_FROM_NAME"] 
                   ?? builder.Configuration["Smtp:FromName"] 
                   ?? "Wihngo";

    config.Provider = provider;
    config.Host = host;
    config.Port = port;
    config.UseSsl = useSsl;
    config.Username = username;
    config.Password = password ?? string.Empty;
    config.FromEmail = fromEmail;
    config.FromName = fromName;
    config.SendGridApiKey = sendGridApiKey;
    
    // Log configuration (without sensitive data)
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Email Configuration loaded:");
    logger.LogInformation("  Provider: {Provider}", provider);
    logger.LogInformation("  SendGrid API Key: {HasKey}", string.IsNullOrEmpty(sendGridApiKey) ? "NOT SET" : "***configured***");
    logger.LogInformation("  SMTP Host: {Host}", host);
    logger.LogInformation("  SMTP Port: {Port}", port);
    logger.LogInformation("  From Email: {FromEmail}", fromEmail);
});

// AWS Configuration - Read from environment variables or appsettings.json
builder.Services.Configure<AwsConfiguration>(config =>
{
    // Try to read from environment variables first
    var accessKeyId = builder.Configuration["AWS_ACCESS_KEY_ID"] 
                      ?? builder.Configuration["AWS:AccessKeyId"];
    var secretAccessKey = builder.Configuration["AWS_SECRET_ACCESS_KEY"] 
                          ?? builder.Configuration["AWS:SecretAccessKey"];
    var bucketName = builder.Configuration["AWS_BUCKET_NAME"] 
                     ?? builder.Configuration["AWS:BucketName"] 
                     ?? "amzn-s3-wihngo-bucket";
    var region = builder.Configuration["AWS_REGION"] 
                 ?? builder.Configuration["AWS:Region"] 
                 ?? "us-east-1";
    var expirationMinutes = int.TryParse(
        builder.Configuration["AWS_PRESIGNED_URL_EXPIRATION_MINUTES"] 
        ?? builder.Configuration["AWS:PresignedUrlExpirationMinutes"], 
        out var minutes) ? minutes : 10;

    config.AccessKeyId = accessKeyId ?? string.Empty;
    config.SecretAccessKey = secretAccessKey ?? string.Empty;
    config.BucketName = bucketName;
    config.Region = region;
    config.PresignedUrlExpirationMinutes = expirationMinutes;
    
    // Log configuration (without sensitive data)
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogInformation("AWS Configuration loaded:");
    logger.LogInformation("  Access Key: {HasKey}", string.IsNullOrEmpty(accessKeyId) ? "NOT SET" : "***" + accessKeyId[^4..]);
    logger.LogInformation("  Secret Key: {HasSecret}", string.IsNullOrEmpty(secretAccessKey) ? "NOT SET" : "***configured***");
    logger.LogInformation("  Bucket: {Bucket}", bucketName);
    logger.LogInformation("  Region: {Region}", region);
});

// HttpClient
builder.Services.AddHttpClient();

// Auth & Security Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordValidationService, PasswordValidationService>();
builder.Services.AddScoped<IAuthEmailService, AuthEmailService>();

// AWS S3 Media Services
builder.Services.AddScoped<IS3Service, S3Service>();

// Crypto Payment Services
builder.Services.AddScoped<ICryptoPaymentService, CryptoPaymentService>();
builder.Services.AddScoped<IBlockchainService, BlockchainVerificationService>();
builder.Services.AddScoped<IHdWalletService, HdWalletService>();
builder.Services.AddScoped<ExchangeRateUpdateJob>();
builder.Services.AddScoped<PaymentMonitorJob>();

// On-Chain Deposit Services
builder.Services.AddScoped<IOnChainDepositService, OnChainDepositService>();
builder.Services.AddScoped<IEvmBlockchainMonitor, EvmBlockchainMonitor>();
builder.Services.AddScoped<ISolanaBlockchainMonitor, SolanaBlockchainMonitor>();
builder.Services.AddScoped<IStellarBlockchainMonitor, StellarBlockchainMonitor>();
builder.Services.AddHostedService<OnChainDepositBackgroundService>();

// Invoice & Payment System Services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
builder.Services.AddScoped<IInvoiceEmailService, InvoiceEmailService>();
builder.Services.AddScoped<IPayPalService, PayPalService>();
builder.Services.AddScoped<IPaymentAuditService, PaymentAuditService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<ReconciliationJob>();

// Blockchain Listener Services
builder.Services.AddHostedService<SolanaListenerService>();
builder.Services.AddHostedService<EvmListenerService>();

// Notification Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<NotificationCleanupJob>();
builder.Services.AddScoped<DailyDigestJob>();
builder.Services.AddScoped<PremiumExpiryNotificationJob>();

// Premium Subscription Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPremiumSubscriptionService, PremiumSubscriptionService>();
builder.Services.AddScoped<ICharityService, CharityService>();
builder.Services.AddScoped<CharityAllocationJob>();

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
    {
        DistributedLockTimeout = TimeSpan.FromMinutes(1),
        PrepareSchemaIfNecessary = true,
        EnableTransactionScopeEnlistment = true
    }));

builder.Services.AddHangfireServer();

// JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // No grace period for token expiry
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                message = "Authentication required",
                code = "UNAUTHORIZED"
            });
            return context.Response.WriteAsync(result);
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders("Token-Expired");
        });
});

var app = builder.Build();

// ========================================
// ?? DIAGNOSTIC: Test Hangfire Initialization
// ========================================
Console.WriteLine("?????????????????????????????");
Console.WriteLine("?? HANGFIRE DIAGNOSTIC");
Console.WriteLine("?????????????????????????????");
try
{
    using var scope = app.Services.CreateScope();
    var storage = scope.ServiceProvider.GetService<JobStorage>();
    
    if (storage != null)
    {
        Console.WriteLine("? Hangfire storage initialized successfully");
        Console.WriteLine($"   Type: {storage.GetType().Name}");
        
        // Test connection
        var connection = storage.GetConnection();
        Console.WriteLine("? Hangfire database connection successful");
    }
    else
    {
        Console.WriteLine("? Hangfire storage is NULL - check configuration!");
    }
}
catch (Exception ex)
{
    Console.WriteLine("? HANGFIRE INITIALIZATION FAILED!");
    Console.WriteLine($"   Error: {ex.Message}");
    Console.WriteLine($"   Make sure PostgreSQL is running");
    Console.WriteLine($"   Database: wihngo");
}
Console.WriteLine("?????????????????????????????");
Console.WriteLine("");

// Ensure database is created (best-effort)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Checking database connection...");
        
        // For a fresh database, create all tables
        var created = db.Database.EnsureCreated();
        
        if (created)
        {
            logger.LogInformation("? Database created successfully!");
            
            // Seed initial data
            await SeedDatabaseAsync(db, logger);
        }
        else
        {
            logger.LogInformation("? Database already exists");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"?? Database initialization warning: {ex.Message}");
        // Continue anyway - migrations might handle it
    }
}

// Add this method before app.Run()
async Task SeedDatabaseAsync(AppDbContext db, ILogger logger)
{
    try
    {
        // Create invoice sequence if it doesn't exist
        await db.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'wihngo_invoice_seq') THEN
                    CREATE SEQUENCE wihngo_invoice_seq START 1;
                END IF;
            END $$;
        ");
        
        logger.LogInformation("? Invoice sequence created");
        
        // Seed supported tokens if not exist
        if (!await db.SupportedTokens.AnyAsync())
        {
            var tokens = new[]
            {
                new SupportedToken
                {
                    Id = Guid.NewGuid(),
                    TokenSymbol = "USDC",
                    Chain = "solana",
                    MintAddress = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
                    Decimals = 6,
                    IsActive = true,
                    TolerancePercent = 0.5m,
                    CreatedAt = DateTime.UtcNow
                },
                new SupportedToken
                {
                    Id = Guid.NewGuid(),
                    TokenSymbol = "EURC",
                    Chain = "solana",
                    MintAddress = "HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr",
                    Decimals = 6,
                    IsActive = true,
                    TolerancePercent = 0.5m,
                    CreatedAt = DateTime.UtcNow
                },
                new SupportedToken
                {
                    Id = Guid.NewGuid(),
                    TokenSymbol = "USDC",
                    Chain = "base",
                    MintAddress = "0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913",
                    Decimals = 6,
                    IsActive = true,
                    TolerancePercent = 0.5m,
                    CreatedAt = DateTime.UtcNow
                },
                new SupportedToken
                {
                    Id = Guid.NewGuid(),
                    TokenSymbol = "EURC",
                    Chain = "base",
                    MintAddress = "0x60a3E35Cc302bFA44Cb288Bc5a4F316Fdb1adb42",
                    Decimals = 6,
                    IsActive = true,
                    TolerancePercent = 0.5m,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await db.SupportedTokens.AddRangeAsync(tokens);
            await db.SaveChangesAsync();
            
            logger.LogInformation("? Seeded {Count} supported tokens", tokens.Length);
        }
        
        logger.LogInformation("? Database seeding complete");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "? Error seeding database");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Apply rate limiting middleware before authentication
app.UseRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

// ========================================
// ?? HANGFIRE DASHBOARD CONFIGURATION
// ========================================
Console.WriteLine("?? Registering Hangfire Dashboard at /hangfire");
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
Console.WriteLine("? Hangfire Dashboard registered");
Console.WriteLine("");


// ========================================
// ?? TEST ENDPOINTS
// ========================================
app.MapGet("/test", () => new
{
    status = "OK",
    timestamp = DateTime.UtcNow,
    message = "Backend is running",
    endpoints = new
    {
        hangfireDashboard = "/hangfire",
        cryptoRates = "/api/payments/crypto/rates",
        auth = new
        {
            register = "/api/auth/register",
            login = "/api/auth/login",
            refreshToken = "/api/auth/refresh-token",
            validate = "/api/auth/validate",
            changePassword = "/api/auth/change-password",
            forgotPassword = "/api/auth/forgot-password",
            resetPassword = "/api/auth/reset-password",
            confirmEmail = "/api/auth/confirm-email",
            logout = "/api/auth/logout"
        }
    }
}).WithName("HealthCheck").WithTags("Diagnostic");

app.MapGet("/hangfire-test", () => "Hangfire routing is working!").WithTags("Diagnostic");

app.MapControllers();

// Schedule background jobs with retry logic to handle lock contention
for (int attempt = 0; attempt < 3; attempt++)
{
    try
    {
        RecurringJob.AddOrUpdate<ExchangeRateUpdateJob>(
            "update-exchange-rates",
            job => job.UpdateExchangeRatesAsync(),
            "*/5 * * * *" // Every 5 minutes
        );

        RecurringJob.AddOrUpdate<PaymentMonitorJob>(
            "monitor-payments",
            job => job.MonitorPendingPaymentsAsync(),
            "*/30 * * * * *" // Every 30 seconds for faster payment updates
        );

        RecurringJob.AddOrUpdate<PaymentMonitorJob>(
            "expire-payments",
            job => job.ExpireOldPaymentsAsync(),
            "0 * * * *" // Every hour
        );

        // Notification background jobs
        RecurringJob.AddOrUpdate<NotificationCleanupJob>(
            "cleanup-notifications",
            job => job.CleanupOldNotificationsAsync(),
            "0 2 * * *" // Daily at 2 AM UTC
        );

        RecurringJob.AddOrUpdate<DailyDigestJob>(
            "send-daily-digests",
            job => job.SendDailyDigestsAsync(),
            "0 * * * *" // Every hour (checks user preferences)
        );

        RecurringJob.AddOrUpdate<PremiumExpiryNotificationJob>(
            "check-premium-expiry",
            job => job.CheckExpiringPremiumAsync(),
            "0 10 * * *" // Daily at 10 AM UTC
        );

        RecurringJob.AddOrUpdate<CharityAllocationJob>(
            "process-charity-allocations",
            job => job.ProcessMonthlyAllocationsAsync(),
            Cron.Monthly // Monthly on the 1st
        );

        // Invoice & Payment System Jobs
        RecurringJob.AddOrUpdate<ReconciliationJob>(
            "reconcile-payments",
            job => job.ReconcilePaymentsAsync(),
            "0 3 * * *" // Daily at 3 AM UTC
        );
        
        break; // Success - exit retry loop
    }
    catch (PostgreSqlDistributedLockException ex)
    {
        if (attempt == 2)
        {
            // Last attempt failed - log and rethrow
            app.Logger.LogError(ex, "Failed to register recurring jobs after {Attempts} attempts", attempt + 1);
            throw;
        }
        
        // Wait before retrying
        app.Logger.LogWarning("Lock timeout on attempt {Attempt}, retrying in 2 seconds...", attempt + 1);
        Thread.Sleep(2000);
    }
}

// ========================================
// ?? STARTUP INFORMATION
// ========================================
Console.WriteLine("");
Console.WriteLine("?????????????????????????????");
Console.WriteLine("?? APPLICATION STARTED");
Console.WriteLine("?????????????????????????????");
Console.WriteLine($"? Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("");
Console.WriteLine("?? Available Endpoints:");
Console.WriteLine("   ?? Hangfire Dashboard:");
Console.WriteLine("      http://localhost:5000/hangfire");
Console.WriteLine("      https://localhost:7001/hangfire");
Console.WriteLine("");
Console.WriteLine("   ?? Test Endpoints:");
Console.WriteLine("      http://localhost:5000/test");
Console.WriteLine("      http://localhost:5000/hangfire-test");
Console.WriteLine("");
Console.WriteLine("   ?? Crypto Payment API:");
Console.WriteLine("      http://localhost:5000/api/payments/crypto/rates");
Console.WriteLine("");
Console.WriteLine("   ?? Authentication API:");
Console.WriteLine("      http://localhost:5000/api/auth/register");
Console.WriteLine("      http://localhost:5000/api/auth/login");
Console.WriteLine("      http://localhost:5000/api/auth/refresh-token");
Console.WriteLine("");
Console.WriteLine("?? NOTE: Use the port shown in 'Now listening on:'");
Console.WriteLine("         message above this section!");
Console.WriteLine("?????????????????????????????");
Console.WriteLine(".");

app.Run();

// Hangfire Authorization Filter
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In production, implement proper authentication
        return true;
    }
}
