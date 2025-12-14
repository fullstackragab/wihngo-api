using Microsoft.Extensions.Logging;
using Npgsql;
using System.Linq.Expressions;
using Dapper;
using Wihngo.Models;
using Wihngo.Models.Entities;

namespace Wihngo.Data
{
    /// <summary>
    /// Compatibility layer for legacy code during migration from EF Core to raw SQL + Dapper.
    /// 
    /// MIGRATION GUIDE:
    /// - For SELECT queries: Use IDbConnectionFactory + Dapper QueryAsync/QueryFirstOrDefaultAsync
    /// - For INSERT: Use Dapper ExecuteAsync with INSERT statements
    /// - For UPDATE: Use Dapper ExecuteAsync with UPDATE statements
    /// - For DELETE: Use Dapper ExecuteAsync with DELETE statements
    /// 
    /// Example:
    ///   var connection = await _dbFactory.CreateOpenConnectionAsync();
    ///   var stories = await connection.QueryAsync<Story>(
    ///       "SELECT * FROM stories WHERE author_id = @AuthorId ORDER BY created_at DESC",
    ///       new { AuthorId = userId });
    /// </summary>
    [Obsolete("Migrate to raw SQL with Dapper. See class documentation for examples.", false)]
    public class AppDbContext : IDisposable
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<AppDbContext> _logger;

        public AppDbContext(IDbConnectionFactory dbFactory, ILogger<AppDbContext> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        // Provide access to factory for migration purposes
        public IDbConnectionFactory GetDbFactory() => _dbFactory;

        // DbSet properties - return minimal stubs for compilation
        public StubDbSet<User> Users => new StubDbSet<User>(_dbFactory, "users");
        public StubDbSet<Bird> Birds => new StubDbSet<Bird>(_dbFactory, "birds");
        public StubDbSet<Story> Stories => new StubDbSet<Story>(_dbFactory, "stories");
        public StubDbSet<SupportTransaction> SupportTransactions => new StubDbSet<SupportTransaction>(_dbFactory, "support_transactions");
        public StubDbSet<Love> Loves => new StubDbSet<Love>(_dbFactory, "loves");
        public StubDbSet<SupportUsage> SupportUsages => new StubDbSet<SupportUsage>(_dbFactory, "support_usages");
        public StubDbSet<BirdPremiumSubscription> BirdPremiumSubscriptions => new StubDbSet<BirdPremiumSubscription>(_dbFactory, "bird_premium_subscriptions");
        public StubDbSet<PremiumStyle> PremiumStyles => new StubDbSet<PremiumStyle>(_dbFactory, "premium_styles");
        public StubDbSet<CharityAllocation> CharityAllocations => new StubDbSet<CharityAllocation>(_dbFactory, "charity_allocations");
        public StubDbSet<CharityImpactStats> CharityImpactStats => new StubDbSet<CharityImpactStats>(_dbFactory, "charity_impact_stats");
        public StubDbSet<PlatformWallet> PlatformWallets => new StubDbSet<PlatformWallet>(_dbFactory, "platform_wallets");
        public StubDbSet<CryptoPaymentRequest> CryptoPaymentRequests => new StubDbSet<CryptoPaymentRequest>(_dbFactory, "crypto_payment_requests");
        public StubDbSet<CryptoTransaction> CryptoTransactions => new StubDbSet<CryptoTransaction>(_dbFactory, "crypto_transactions");
        public StubDbSet<CryptoExchangeRate> CryptoExchangeRates => new StubDbSet<CryptoExchangeRate>(_dbFactory, "crypto_exchange_rates");
        public StubDbSet<CryptoPaymentMethod> CryptoPaymentMethods => new StubDbSet<CryptoPaymentMethod>(_dbFactory, "crypto_payment_methods");
        public StubDbSet<OnChainDeposit> OnChainDeposits => new StubDbSet<OnChainDeposit>(_dbFactory, "on_chain_deposits");
        public StubDbSet<TokenConfiguration> TokenConfigurations => new StubDbSet<TokenConfiguration>(_dbFactory, "token_configurations");
        public StubDbSet<Notification> Notifications => new StubDbSet<Notification>(_dbFactory, "notifications");
        public StubDbSet<NotificationPreference> NotificationPreferences => new StubDbSet<NotificationPreference>(_dbFactory, "notification_preferences");
        public StubDbSet<NotificationSettings> NotificationSettings => new StubDbSet<NotificationSettings>(_dbFactory, "notification_settings");
        public StubDbSet<UserDevice> UserDevices => new StubDbSet<UserDevice>(_dbFactory, "user_devices");
        public StubDbSet<Invoice> Invoices => new StubDbSet<Invoice>(_dbFactory, "invoices");
        public StubDbSet<Payment> Payments => new StubDbSet<Payment>(_dbFactory, "payments");
        public StubDbSet<SupportedToken> SupportedTokens => new StubDbSet<SupportedToken>(_dbFactory, "supported_tokens");
        public StubDbSet<RefundRequest> RefundRequests => new StubDbSet<RefundRequest>(_dbFactory, "refund_requests");
        public StubDbSet<PaymentEvent> PaymentEvents => new StubDbSet<PaymentEvent>(_dbFactory, "payment_events");
        public StubDbSet<AuditLog> AuditLogs => new StubDbSet<AuditLog>(_dbFactory, "audit_logs");
        public StubDbSet<WebhookReceived> WebhooksReceived => new StubDbSet<WebhookReceived>(_dbFactory, "webhooks_received");
        public StubDbSet<BlockchainCursor> BlockchainCursors => new StubDbSet<BlockchainCursor>(_dbFactory, "blockchain_cursors");

        public StubDatabase Database => new StubDatabase();

        public StubEntityEntry<T> Entry<T>(T entity) where T : class => new StubEntityEntry<T>();
        
        public StubDbSet<T> Set<T>() where T : class
        {
            var tableName = ToSnakeCase(typeof(T).Name);
            return new StubDbSet<T>(_dbFactory, tableName);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("SaveChangesAsync called - no-op. Migrate to ExecuteAsync with INSERT/UPDATE/DELETE SQL.");
            return Task.FromResult(0);
        }

        private static string ToSnakeCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(text[0]));
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(text[i]));
                }
                else
                {
                    result.Append(text[i]);
                }
            }
            return result.ToString();
        }

        public void Dispose() { }
    }

    [Obsolete("Migrate to raw SQL.", false)]
    public class StubEntityEntry<T> where T : class
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Use QueryFirstOrDefaultAsync to reload entity.");
        }
    }

    [Obsolete("Migrate to raw SQL.", false)]
    public class StubDbSet<T> : IQueryable<T> where T : class
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly string _tableName;

        public StubDbSet(IDbConnectionFactory dbFactory, string tableName)
        {
            _dbFactory = dbFactory;
            _tableName = tableName;
        }

        public Type ElementType => typeof(T);
        public Expression Expression => Expression.Constant(this);
        public IQueryProvider Provider => throw new NotImplementedException("Use raw SQL");

        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException("Use QueryAsync");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new NotImplementedException("Use QueryAsync");

        public void Add(T entity) => throw new NotImplementedException("Use ExecuteAsync with INSERT");
        public void AddRange(IEnumerable<T> entities) => throw new NotImplementedException("Use ExecuteAsync with INSERT");
        public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(T entity) => throw new NotImplementedException("Use ExecuteAsync with DELETE");
        public void RemoveRange(IEnumerable<T> entities) => throw new NotImplementedException("Use ExecuteAsync with DELETE");

        public async Task<T?> FindAsync(params object[] keyValues)
        {
            if (keyValues.Length == 0) return null;
            
            var connection = await _dbFactory.CreateOpenConnectionAsync();
            try
            {
                var sql = $"SELECT * FROM {_tableName} WHERE {GetPrimaryKeyColumn()} = @Id LIMIT 1";
                return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = keyValues[0] });
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        internal IDbConnectionFactory GetDbFactory() => _dbFactory;
        internal string GetTableName() => _tableName;

        private string GetPrimaryKeyColumn()
        {
            var idProperty = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name.EndsWith("Id"));
            return idProperty != null ? ToSnakeCase(idProperty.Name) : "id";
        }

        private static string ToSnakeCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(text[0]));
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(text[i]));
                }
                else
                {
                    result.Append(text[i]);
                }
            }
            return result.ToString();
        }
    }

    [Obsolete("Migrate to raw SQL.", false)]
    public class StubDatabase
    {
        public Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters) => throw new NotImplementedException("Use ExecuteAsync");
        public Task<int> ExecuteSqlInterpolatedAsync(FormattableString sql, CancellationToken cancellationToken = default) => throw new NotImplementedException("Use ExecuteAsync");
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public bool EnsureCreated() => throw new NotImplementedException();
        public Task EnsureCreatedAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<StubTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(new StubTransaction());
        public object GetDbConnection() => throw new NotImplementedException();
        public IQueryable<T> SqlQuery<T>(FormattableString sql) => throw new NotImplementedException();
    }

    [Obsolete("Migrate to NpgsqlTransaction.", false)]
    public class StubTransaction : IAsyncDisposable, IDisposable
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RollbackAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }
}
