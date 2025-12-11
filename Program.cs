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
builder.Logging.AddFilter("Hangfire", LogLevel.Warning);  // Changed from None to see errors
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);  // Enable to see HTTP errors
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);  // Show startup info

// ? Enable ONLY crypto payment logs
builder.Logging.AddFilter("Wihngo.Services.CryptoPaymentService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Services.BlockchainVerificationService", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.CryptoPaymentsController", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.BackgroundJobs.ExchangeRateUpdateJob", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.BackgroundJobs.PaymentMonitorJob", LogLevel.Information);

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register AutoMapper with explicit profile
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());

// Register DbContext using PostgreSQL (Npgsql)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

// HttpClient
builder.Services.AddHttpClient();

// Token Service
builder.Services.AddScoped<TokenService>();

// Crypto Payment Services
builder.Services.AddScoped<ICryptoPaymentService, CryptoPaymentService>();
builder.Services.AddScoped<IBlockchainService, BlockchainVerificationService>();
builder.Services.AddScoped<ExchangeRateUpdateJob>();
builder.Services.AddScoped<PaymentMonitorJob>();

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
Console.WriteLine("???????????????????????????????????????");
Console.WriteLine("?? HANGFIRE DIAGNOSTIC");
Console.WriteLine("???????????????????????????????????????");
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
Console.WriteLine("???????????????????????????????????????");
Console.WriteLine("");

// Ensure database is created (best-effort)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
    catch
    {
        // ignore if DB not reachable during build step
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

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
        cryptoRates = "/api/payments/crypto/rates"
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
Console.WriteLine("???????????????????????????????????????");
Console.WriteLine("?? APPLICATION STARTED");
Console.WriteLine("???????????????????????????????????????");
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
Console.WriteLine("?? NOTE: Use the port shown in 'Now listening on:'");
Console.WriteLine("         message above this section!");
Console.WriteLine("???????????????????????????????????????");
Console.WriteLine("");

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
