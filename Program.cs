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

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer();

// JWT configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "please_change_this_secret";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});


var app = builder.Build();

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

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

// Schedule background jobs
RecurringJob.AddOrUpdate<ExchangeRateUpdateJob>(
    "update-exchange-rates",
    job => job.UpdateExchangeRatesAsync(),
    "*/5 * * * *" // Every 5 minutes
);

RecurringJob.AddOrUpdate<PaymentMonitorJob>(
    "monitor-payments",
    job => job.MonitorPendingPaymentsAsync(),
    "* * * * *" // Every minute
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
