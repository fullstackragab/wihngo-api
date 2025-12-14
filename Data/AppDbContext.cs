using Microsoft.EntityFrameworkCore;
using Wihngo.Models;
using Wihngo.Models.Entities;
using System.Text.RegularExpressions;

namespace Wihngo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Bird> Birds { get; set; } = null!;
        public DbSet<Story> Stories { get; set; } = null!;
        public DbSet<StoryBird> StoryBirds { get; set; } = null!;
        public DbSet<SupportTransaction> SupportTransactions { get; set; } = null!;
        public DbSet<Love> Loves { get; set; } = null!;
        public DbSet<SupportUsage> SupportUsages { get; set; } = null!; // plural set name to match entity
        public DbSet<BirdPremiumSubscription> BirdPremiumSubscriptions { get; set; } = null!;
        public DbSet<PremiumStyle> PremiumStyles { get; set; } = null!;
        public DbSet<CharityAllocation> CharityAllocations { get; set; } = null!;
        public DbSet<CharityImpactStats> CharityImpactStats { get; set; } = null!;

        // Crypto Payment Entities
        public DbSet<PlatformWallet> PlatformWallets { get; set; } = null!;
        public DbSet<CryptoPaymentRequest> CryptoPaymentRequests { get; set; } = null!;
        public DbSet<CryptoTransaction> CryptoTransactions { get; set; } = null!;
        public DbSet<CryptoExchangeRate> CryptoExchangeRates { get; set; } = null!;
        public DbSet<CryptoPaymentMethod> CryptoPaymentMethods { get; set; } = null!;
        public DbSet<OnChainDeposit> OnChainDeposits { get; set; } = null!;
        public DbSet<TokenConfiguration> TokenConfigurations { get; set; } = null!;

        // Notification Entities
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;
        public DbSet<NotificationSettings> NotificationSettings { get; set; } = null!;
        public DbSet<UserDevice> UserDevices { get; set; } = null!;

        // Invoice & Payment System Entities
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<SupportedToken> SupportedTokens { get; set; } = null!;
        public DbSet<RefundRequest> RefundRequests { get; set; } = null!;
        public DbSet<PaymentEvent> PaymentEvents { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<WebhookReceived> WebhooksReceived { get; set; } = null!;
        public DbSet<BlockchainCursor> BlockchainCursors { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // map to lowercase table names to match your DB
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Bird>().ToTable("birds");
            modelBuilder.Entity<Story>().ToTable("stories");
            modelBuilder.Entity<StoryBird>().ToTable("story_birds");
            modelBuilder.Entity<BirdPremiumSubscription>().ToTable("bird_premium_subscriptions");
            modelBuilder.Entity<PremiumStyle>().ToTable("premium_styles");
            modelBuilder.Entity<CharityAllocation>().ToTable("charity_allocations");
            modelBuilder.Entity<CharityImpactStats>().ToTable("charity_impact_stats");
            // Use underscore-separated plural table name to match SQL scripts
            modelBuilder.Entity<SupportTransaction>().ToTable("support_transactions");
            modelBuilder.Entity<Love>().ToTable("loves");
            modelBuilder.Entity<SupportUsage>().ToTable("support_usage");

            // Notification table mappings
            modelBuilder.Entity<Notification>().ToTable("notifications");
            modelBuilder.Entity<NotificationPreference>().ToTable("notification_preferences");
            modelBuilder.Entity<NotificationSettings>().ToTable("notification_settings");
            modelBuilder.Entity<UserDevice>().ToTable("user_devices");

            // Invoice & Payment System table mappings
            modelBuilder.Entity<Invoice>().ToTable("invoices");
            modelBuilder.Entity<Payment>().ToTable("payments");
            modelBuilder.Entity<SupportedToken>().ToTable("supported_tokens");
            modelBuilder.Entity<RefundRequest>().ToTable("refund_requests");
            modelBuilder.Entity<PaymentEvent>().ToTable("payment_events");
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
            modelBuilder.Entity<WebhookReceived>().ToTable("webhook_received");
            modelBuilder.Entity<BlockchainCursor>().ToTable("blockchain_cursors");

            // Configure composite primary key for Love join table
            modelBuilder.Entity<Love>().HasKey(l => new { l.UserId, l.BirdId });

            // Configure unique constraint for PremiumStyle (one per bird)
            modelBuilder.Entity<PremiumStyle>()
                .HasIndex(ps => ps.BirdId)
                .IsUnique();

            // Configure indexes for CharityAllocation
            modelBuilder.Entity<CharityAllocation>()
                .HasIndex(ca => ca.SubscriptionId);

            modelBuilder.Entity<CharityAllocation>()
                .HasIndex(ca => ca.AllocatedAt);

            // Explicitly map DonationCents to donation_cents (temporary fix)
            modelBuilder.Entity<Bird>()
                .Property(b => b.DonationCents)
                .HasColumnName("donation_cents");

            modelBuilder.Entity<User>()
                .HasMany(u => u.Birds)
                .WithOne(b => b.Owner)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Stories)
                .WithOne(s => s.Author)
                .HasForeignKey(s => s.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SupportTransactions)
                .WithOne(t => t.Supporter)
                .HasForeignKey(t => t.SupporterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Loves)
                .WithOne(l => l.User)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure StoryBird many-to-many relationship
            modelBuilder.Entity<Story>()
                .HasMany(s => s.StoryBirds)
                .WithOne(sb => sb.Story)
                .HasForeignKey(sb => sb.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoryBird>()
                .HasOne(sb => sb.Bird)
                .WithMany()
                .HasForeignKey(sb => sb.BirdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bird>()
                .HasMany(b => b.SupportTransactions)
                .WithOne(t => t.Bird)
                .HasForeignKey(t => t.BirdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bird>()
                .HasMany(b => b.Loves)
                .WithOne(l => l.Bird)
                .HasForeignKey(l => l.BirdId)
                .OnDelete(DeleteBehavior.Cascade);

            // Crypto Payment Configurations
            
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

            // OnChainDeposit indexes
            modelBuilder.Entity<OnChainDeposit>()
                .HasIndex(d => d.UserId);

            modelBuilder.Entity<OnChainDeposit>()
                .HasIndex(d => d.TxHashOrSig)
                .IsUnique();

            modelBuilder.Entity<OnChainDeposit>()
                .HasIndex(d => d.Status);

            modelBuilder.Entity<OnChainDeposit>()
                .HasIndex(d => new { d.Chain, d.AddressOrAccount });

            modelBuilder.Entity<OnChainDeposit>()
                .HasIndex(d => d.DetectedAt);

            // TokenConfiguration indexes and unique constraint
            modelBuilder.Entity<TokenConfiguration>()
                .HasIndex(t => new { t.Token, t.Chain })
                .IsUnique();

            modelBuilder.Entity<TokenConfiguration>()
                .HasIndex(t => t.IsActive);

            // Notification indexes
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.GroupId);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt);

            modelBuilder.Entity<NotificationPreference>()
                .HasIndex(np => new { np.UserId, np.NotificationType })
                .IsUnique();

            modelBuilder.Entity<NotificationSettings>()
                .HasIndex(ns => ns.UserId)
                .IsUnique();

            modelBuilder.Entity<UserDevice>()
                .HasIndex(d => d.UserId);

            modelBuilder.Entity<UserDevice>()
                .HasIndex(d => d.PushToken)
                .IsUnique();

            // Invoice & Payment System Configurations
            
            // Invoice unique constraints and indexes
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique()
                .HasFilter("invoice_number IS NOT NULL");

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.UserId);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.State);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.ExpiresAt);

            // Payment unique constraints and indexes
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TxHash)
                .IsUnique()
                .HasFilter("tx_hash IS NOT NULL");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.InvoiceId);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.ProviderTxId)
                .HasFilter("provider_tx_id IS NOT NULL");

            // SupportedToken unique constraint
            modelBuilder.Entity<SupportedToken>()
                .HasIndex(st => new { st.TokenSymbol, st.Chain })
                .IsUnique();

            // RefundRequest indexes
            modelBuilder.Entity<RefundRequest>()
                .HasIndex(r => r.InvoiceId);

            modelBuilder.Entity<RefundRequest>()
                .HasIndex(r => r.State);

            // PaymentEvent indexes
            modelBuilder.Entity<PaymentEvent>()
                .HasIndex(pe => pe.InvoiceId);

            modelBuilder.Entity<PaymentEvent>()
                .HasIndex(pe => pe.PaymentId)
                .HasFilter("payment_id IS NOT NULL");

            modelBuilder.Entity<PaymentEvent>()
                .HasIndex(pe => pe.CreatedAt);

            // AuditLog indexes
            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => new { al.EntityType, al.EntityId });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.UserId)
                .HasFilter("user_id IS NOT NULL");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.CreatedAt);

            // WebhookReceived unique constraint and indexes
            modelBuilder.Entity<WebhookReceived>()
                .HasIndex(w => new { w.Provider, w.ProviderEventId })
                .IsUnique();

            modelBuilder.Entity<WebhookReceived>()
                .HasIndex(w => w.Processed);

            // BlockchainCursor unique constraint
            modelBuilder.Entity<BlockchainCursor>()
                .HasIndex(bc => new { bc.Chain, bc.CursorType })
                .IsUnique();

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
                },
                new PlatformWallet
                {
                    Id = Guid.NewGuid(),
                    Currency = "USDT",
                    Network = "ethereum",
                    Address = "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PlatformWallet
                {
                    Id = Guid.NewGuid(),
                    Currency = "USDT",
                    Network = "binance-smart-chain",
                    Address = "0x83675000ac9915614afff618906421a2baea0020",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PlatformWallet
                {
                    Id = Guid.NewGuid(),
                    Currency = "ETH",
                    Network = "sepolia",
                    Address = "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
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

            // Apply snake_case column naming for all properties so EF maps to typical Postgres column names
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // convert table name to snake_case if not explicitly set
                var currentTableName = entity.GetTableName();
                if (string.IsNullOrEmpty(currentTableName))
                {
                    entity.SetTableName(ToSnakeCase(entity.ClrType.Name));
                }
                else
                {
                    // ensure table name is snake_case as well
                    entity.SetTableName(ToSnakeCase(currentTableName));
                }

                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }
        }

        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // simple PascalCase / camelCase to snake_case converter
            var startUnderscores = Regex.Match(input, "^_+");
            var result = Regex.Replace(input, "([a-z0-9])([A-Z])", "$1_$2");
            result = Regex.Replace(result, "([A-Z])([A-Z][a-z])", "$1_$2");
            return result.ToLowerInvariant();
        }
    }
}
