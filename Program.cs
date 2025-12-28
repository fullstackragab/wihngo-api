using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using Scalar.AspNetCore;
using Dapper;

// Configure Dapper to map snake_case database columns to PascalCase C# properties
// This MUST be set before any Dapper queries are executed
DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

// Load local secrets file (not committed to git)
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// ========================================
// 🔧 CRYPTO-ONLY LOGGING CONFIGURATION
// ========================================
// Clear default providers and configure crypto-payment-only logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Suppress all general framework logs
builder.Logging.AddFilter("Microsoft", LogLevel.None);
builder.Logging.AddFilter("System", LogLevel.None);

// IMPORTANT: Enable ALL Wihngo errors (this catches any service exceptions)
builder.Logging.AddFilter("Wihngo", LogLevel.Error);
// TEMPORARY: Enable Hangfire logs to diagnose dashboard issue
builder.Logging.AddFilter("Hangfire", LogLevel.Warning);
// TEMPORARILY enable ALL ASP.NET Core logs to see requests
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

// ✅ Enable P2P payment logs (USDC on Solana)
builder.Logging.AddFilter("Wihngo.Services.P2PPaymentService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.SolanaTransactionService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.WalletService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.LedgerService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.GasSponsorshipService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.P2PPaymentsController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.WalletsController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.BackgroundJobs.PaymentConfirmationJob", LogLevel.Information);

// ? Enable auth and security logs
builder.Logging.AddFilter("Wihngo.Controllers.AuthController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.UsersController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.StoriesController", LogLevel.Debug);
builder.Logging.AddFilter("Wihngo.Services.TokenService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Middleware.RateLimitingMiddleware", LogLevel.Information);

// ? Enable AI story generation logs (all levels including debug)
builder.Logging.AddFilter("Wihngo.Services.AiStoryGenerationService", LogLevel.Debug);
// Add ASP.NET Core authentication logs
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Information);

// ? Enable media/S3 logs
builder.Logging.AddFilter("Wihngo.Services.S3Service", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.MediaController", LogLevel.Information);

// ? Enable content moderation logs
builder.Logging.AddFilter("Wihngo.Services.ContentModerationService", LogLevel.Debug);

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
    // Prevent circular reference errors when returning entities with navigation properties
    opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

    // Make property name matching case-insensitive (accepts both camelCase and PascalCase)
    opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

    // Allow enums to be serialized/deserialized as strings (e.g., "FunnyMoment" instead of 3)
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

// Register AutoMapper with explicit profile
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());

// Register Memory Cache for rate limiting and caching
builder.Services.AddMemoryCache();

// ========================================
// 🗄️ DATABASE CONNECTION WITH RETRY LOGIC
// ========================================
// Register database connection factory using Npgsql directly (no EF)
// Read connection string from environment variable (secure) or fallback to appsettings.json
var connectionString = builder.Configuration["ConnectionStrings__DefaultConnection"] 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Database connection string is not configured. Set ConnectionStrings__DefaultConnection environment variable.");

Console.WriteLine("");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("🔌 DATABASE CONNECTION DIAGNOSTICS");
Console.WriteLine("═══════════════════════════════════════════════");

// Parse and display connection details (safely)
var dbDetails = ParseConnectionString(connectionString);
Console.WriteLine($"📍 Host: {dbDetails.Host}");
Console.WriteLine($"🔢 Port: {dbDetails.Port}");
Console.WriteLine($"💾 Database: {dbDetails.Database}");
Console.WriteLine($"👤 Username: {dbDetails.Username}");
Console.WriteLine($"🔐 Password: {(string.IsNullOrEmpty(dbDetails.Password) ? "NOT SET" : "***configured***")}");
Console.WriteLine($"🔒 SSL Mode: {dbDetails.SslMode}");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("");

// Helper method to parse connection string
static (string Host, string Port, string Database, string Username, string Password, string SslMode) ParseConnectionString(string connectionString)
{
    var parts = connectionString.Split(';');
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    
    foreach (var part in parts)
    {
        var kvp = part.Split('=', 2);
        if (kvp.Length == 2)
        {
            dict[kvp[0].Trim()] = kvp[1].Trim();
        }
    }
    
    return (
        dict.GetValueOrDefault("Host", "unknown"),
        dict.GetValueOrDefault("Port", "5432"),
        dict.GetValueOrDefault("Database", "unknown"),
        dict.GetValueOrDefault("Username", "unknown"),
        dict.GetValueOrDefault("Password", ""),
        dict.GetValueOrDefault("SSL Mode", "none")
    );
}

// Test database connection with retry logic
bool isDatabaseAvailable = false;
int maxRetries = 3;
int retryDelayMs = 2000;

Console.WriteLine("🔄 Testing database connection...");
for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        using var testConnection = new Npgsql.NpgsqlConnection(connectionString);
        await testConnection.OpenAsync();
        
        Console.WriteLine($"✅ Database connection successful on attempt {attempt}!");
        isDatabaseAvailable = true;
        
        // Get PostgreSQL version with Dapper
        var version = await testConnection.ExecuteScalarAsync<string>("SELECT version()");
        Console.WriteLine($"📊 PostgreSQL Version: {version?.Split('\n')[0]}");
        
        await testConnection.CloseAsync();
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Attempt {attempt}/{maxRetries} failed: {ex.GetType().Name}");
        Console.WriteLine($"   Message: {ex.Message}");
        
        if (ex.InnerException != null)
        {
            Console.WriteLine($"   Inner: {ex.InnerException.Message}");
        }
        
        if (attempt < maxRetries)
        {
            Console.WriteLine($"   ⏳ Retrying in {retryDelayMs}ms...");
            await Task.Delay(retryDelayMs);
        }
        else
        {
            Console.WriteLine("");
            Console.WriteLine("⚠️  DATABASE CONNECTION FAILED AFTER ALL RETRIES");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("Possible causes:");
            Console.WriteLine("  1. Database server is not running");
            Console.WriteLine("  2. Network/firewall blocking connection");
            Console.WriteLine("  3. Invalid credentials");
            Console.WriteLine("  4. SSL certificate issues");
            Console.WriteLine("");
            Console.WriteLine("The application will start but database-dependent");
            Console.WriteLine("features will not work until connection is restored.");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("");
        }
    }
}

// Register database connection factory (replaces EF DbContext)
builder.Services.AddSingleton<IDbConnectionFactory>(sp => 
    new NpgsqlConnectionFactory(connectionString));

// TEMPORARY: Register stub AppDbContext for backward compatibility during migration
// This allows existing code to compile while we migrate to raw SQL
builder.Services.AddScoped<AppDbContext>();

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
builder.Services.Configure<WiseConfiguration>(builder.Configuration.GetSection("Wise"));
builder.Services.Configure<PayoutConfiguration>(builder.Configuration.GetSection("Payout"));

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

// Email Settings (SendGrid) - Used by AuthEmailService
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

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
builder.Services.AddSingleton<IAuthEmailService, AuthEmailService>();

// AWS S3 Media Services
builder.Services.AddScoped<IS3Service, S3Service>();

// P2P Payment Configuration
builder.Services.Configure<P2PPaymentConfiguration>(builder.Configuration.GetSection("P2PPayment"));
builder.Services.Configure<SolanaConfig>(builder.Configuration.GetSection("Solana"));

// P2P Payment Services (NEW - USDC on Solana)
builder.Services.AddScoped<ISolanaTransactionService, SolanaTransactionService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IGasSponsorshipService, GasSponsorshipService>();
builder.Services.AddScoped<IInvoiceEmailService, InvoiceEmailService>();
builder.Services.AddScoped<IP2PPaymentService, P2PPaymentService>();
builder.Services.AddScoped<ISupportIntentService, SupportIntentService>();
builder.Services.AddScoped<PaymentConfirmationJob>();

// Notification Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<NotificationCleanupJob>();
builder.Services.AddScoped<DailyDigestJob>();

// Charity Services (donations to bird charities - separate from premium)
builder.Services.AddScoped<ICharityService, CharityService>();
builder.Services.AddScoped<CharityAllocationJob>();

// Payout Services
builder.Services.AddScoped<IPayoutService, PayoutService>();
builder.Services.AddScoped<IPayoutValidationService, PayoutValidationService>();
builder.Services.AddScoped<IPayoutCalculationService, PayoutCalculationService>();
builder.Services.AddScoped<IWisePayoutService, WisePayoutService>();
builder.Services.AddScoped<IPayPalPayoutService, PayPalPayoutService>();
builder.Services.AddScoped<MonthlyPayoutJob>();

// Memorial Services
builder.Services.AddScoped<IMemorialService, MemorialService>();

// Bird Activity Services
builder.Services.AddScoped<IBirdActivityService, BirdActivityService>();

// AI Story Generation Services
builder.Services.AddScoped<IAiStoryGenerationService, AiStoryGenerationService>();
builder.Services.AddScoped<IWhisperTranscriptionService, WhisperTranscriptionService>();

// Content Moderation Services
builder.Services.Configure<ContentModerationConfiguration>(builder.Configuration.GetSection("ContentModeration"));
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();

// Smart Feed Services
builder.Services.AddSingleton<ILanguageDetectionService, LanguageDetectionService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<IBirdFollowService, BirdFollowService>();
builder.Services.AddScoped<IFeedRankingService, FeedRankingService>();
builder.Services.AddScoped<StoryLanguageBackfillService>();

// Kind Words (Comments) Services
builder.Services.AddScoped<IKindWordsService, KindWordsService>();

// 📋 HANGFIRE - CONDITIONAL SETUP
if (isDatabaseAvailable)
{
    Console.WriteLine("🔧 Configuring Hangfire with PostgreSQL storage...");
    try
    {
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
        Console.WriteLine("✅ Hangfire configured successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Hangfire configuration failed: {ex.Message}");
        Console.WriteLine("   Background jobs will not be available.");
    }
}
else
{
    Console.WriteLine("⚠️  Skipping Hangfire setup - database not available");
}

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
// 🔍 DATABASE INITIALIZATION (BEST EFFORT)
// ========================================
if (isDatabaseAvailable)
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Checking database connection...");
            
            using var connection = await dbFactory.CreateOpenConnectionAsync();
            logger.LogInformation("✅ Database connection verified");
            
            // In development, seed comprehensive dummy data
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("🌱 Seeding development data...");
                // TEMPORARILY DISABLED: Database seeder needs migration to raw SQL
                // await Wihngo.Database.DatabaseSeeder.SeedDevelopmentDataAsync(app.Services);
                logger.LogWarning("⚠️  Development data seeding is temporarily disabled - needs migration to raw SQL");
            }
            else
            {
                // In production, just seed essential data
                await SeedDatabaseAsync(dbFactory, logger);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Database initialization warning: {ex.Message}");
            Console.WriteLine("   Application will start without database features.");
        }
    }
}
else
{
    Console.WriteLine("⚠️  Skipping database initialization - connection not available");
}

// Add this method before app.Run()
async Task SeedDatabaseAsync(IDbConnectionFactory dbFactory, ILogger logger)
{
    try
    {
        using var connection = await dbFactory.CreateOpenConnectionAsync();
        
        // Create invoice sequence if it doesn't exist
        await connection.ExecuteAsync(@"
            DO $$ 
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'wihngo_invoice_seq') THEN
                    CREATE SEQUENCE wihngo_invoice_seq START 1;
                END IF;
            END $$;");
        
        logger.LogInformation("✅ Invoice sequence created");
        
        // Seed supported tokens if not exist (ONLY USDC and EURC on Solana)
        var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM supported_tokens");
        
        if (count == 0)
        {
            var tokens = new[]
            {
                new { Id = Guid.NewGuid(), TokenSymbol = "USDC", Chain = "solana", MintAddress = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v", Decimals = 6, IsActive = true, TolerancePercent = 0.5m, CreatedAt = DateTime.UtcNow },
                new { Id = Guid.NewGuid(), TokenSymbol = "EURC", Chain = "solana", MintAddress = "HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr", Decimals = 6, IsActive = true, TolerancePercent = 0.5m, CreatedAt = DateTime.UtcNow }
            };
            
            await connection.ExecuteAsync(@"
                INSERT INTO supported_tokens (id, token_symbol, chain, mint_address, decimals, is_active, tolerance_percent, created_at)
                VALUES (@Id, @TokenSymbol, @Chain, @MintAddress, @Decimals, @IsActive, @TolerancePercent, @CreatedAt)",
                tokens);
            
            logger.LogInformation("✅ Seeded {Count} supported tokens (USDC and EURC on Solana)", tokens.Length);
        }
        
        logger.LogInformation("✅ Database seeding complete");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding database");
    }
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Configure Swagger/OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Map controllers
app.MapControllers();

// Configure Hangfire Dashboard and Recurring Jobs (if database is available)
if (isDatabaseAvailable)
{
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    // Register recurring jobs
    RecurringJob.AddOrUpdate<PaymentConfirmationJob>(
        "check-pending-payments",
        job => job.CheckPendingPaymentsAsync(),
        "*/10 * * * * *"); // Every 10 seconds for payment confirmations

    Console.WriteLine("✅ PaymentConfirmationJob scheduled (every 10 seconds)");
}

Console.WriteLine("");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("🚀 APPLICATION STARTING");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"API Documentation: {(app.Environment.IsDevelopment() ? "https://localhost:7297/scalar/v1" : "Disabled")}");
Console.WriteLine($"OpenAPI Spec: {(app.Environment.IsDevelopment() ? "https://localhost:7297/openapi/v1.json" : "Disabled")}");
Console.WriteLine($"Hangfire Dashboard: {(isDatabaseAvailable ? "https://localhost:7297/hangfire" : "Disabled")}");
Console.WriteLine("");

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("🔑 TEST USER CREDENTIALS (Development Only):");
    Console.WriteLine("   Email: alice@example.com");
    Console.WriteLine("   Password: Password123!");
    Console.WriteLine("");
    Console.WriteLine("📊 GET USER STATS:");
    Console.WriteLine("   All Users: GET http://localhost:5162/api/dev/users");
    Console.WriteLine("   Quick Creds: GET http://localhost:5162/api/dev/test-credentials");
    Console.WriteLine("   User Details: GET http://localhost:5162/api/dev/users/{email}");
    Console.WriteLine("");
}

Console.WriteLine("📱 MOBILE APP CONNECTION URLS:");
Console.WriteLine("   For localhost/emulator: https://localhost:7297/api/");

// Display network IP addresses for mobile device connections
try
{
    var hostName = System.Net.Dns.GetHostName();
    var hostEntry = System.Net.Dns.GetHostEntry(hostName);
    var localIPs = hostEntry.AddressList
        .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        .ToList();
    
    if (localIPs.Any())
    {
        Console.WriteLine("   For mobile devices on same network:");
        foreach (var ip in localIPs)
        {
            Console.WriteLine($"   - https://{ip}:7297/api/");
            Console.WriteLine($"     (or http://{ip}:5162/api/)");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"   ⚠️ Could not determine network IP: {ex.Message}");
}

Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("");

app.Run();
