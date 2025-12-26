using Microsoft.Extensions.Logging;
using Dapper;
using System.Linq.Expressions;
using System.Data;
using Wihngo.Models;
using Wihngo.Models.Entities;
using Wihngo.Models.Payout;

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
        public StubDbSet<User> Users => new StubDbSet<User>(_dbFactory, "users", "user_id", _logger);
        public StubDbSet<Bird> Birds => new StubDbSet<Bird>(_dbFactory, "birds", "bird_id", _logger);
        public StubDbSet<Story> Stories => new StubDbSet<Story>(_dbFactory, "stories", "story_id", _logger);
        public StubDbSet<SupportTransaction> SupportTransactions => new StubDbSet<SupportTransaction>(_dbFactory, "support_transactions", "transaction_id", _logger);
        public StubDbSet<Love> Loves => new StubDbSet<Love>(_dbFactory, "loves", "love_id", _logger);
        public StubDbSet<SupportUsage> SupportUsages => new StubDbSet<SupportUsage>(_dbFactory, "support_usages", "usage_id", _logger);
        public StubDbSet<BirdPremiumSubscription> BirdPremiumSubscriptions => new StubDbSet<BirdPremiumSubscription>(_dbFactory, "bird_premium_subscriptions", "subscription_id", _logger);
        public StubDbSet<PremiumStyle> PremiumStyles => new StubDbSet<PremiumStyle>(_dbFactory, "premium_styles", "id", _logger);
        public StubDbSet<CharityAllocation> CharityAllocations => new StubDbSet<CharityAllocation>(_dbFactory, "charity_allocations", "allocation_id", _logger);
        public StubDbSet<CharityImpactStats> CharityImpactStats => new StubDbSet<CharityImpactStats>(_dbFactory, "charity_impact_stats", "id", _logger);
        public StubDbSet<OnChainDeposit> OnChainDeposits => new StubDbSet<OnChainDeposit>(_dbFactory, "onchain_deposits", "id", _logger);
        public StubDbSet<TokenConfiguration> TokenConfigurations => new StubDbSet<TokenConfiguration>(_dbFactory, "token_configurations", "id", _logger);
        public StubDbSet<Notification> Notifications => new StubDbSet<Notification>(_dbFactory, "notifications", "id", _logger);
        public StubDbSet<NotificationPreference> NotificationPreferences => new StubDbSet<NotificationPreference>(_dbFactory, "notification_preferences", "id", _logger);
        public StubDbSet<NotificationSettings> NotificationSettings => new StubDbSet<NotificationSettings>(_dbFactory, "notification_settings", "id", _logger);
        public StubDbSet<UserDevice> UserDevices => new StubDbSet<UserDevice>(_dbFactory, "user_devices", "id", _logger);
        public StubDbSet<SupportedToken> SupportedTokens => new StubDbSet<SupportedToken>(_dbFactory, "supported_tokens", "id", _logger);
        public StubDbSet<RefundRequest> RefundRequests => new StubDbSet<RefundRequest>(_dbFactory, "refund_requests", "id", _logger);
        public StubDbSet<AuditLog> AuditLogs => new StubDbSet<AuditLog>(_dbFactory, "audit_logs", "id", _logger);
        public StubDbSet<WebhookReceived> WebhooksReceived => new StubDbSet<WebhookReceived>(_dbFactory, "webhooks_received", "id", _logger);
        public StubDbSet<BlockchainCursor> BlockchainCursors => new StubDbSet<BlockchainCursor>(_dbFactory, "blockchain_cursors", "id", _logger);
        public StubDbSet<StoryLike> StoryLikes => new StubDbSet<StoryLike>(_dbFactory, "story_likes", "like_id", _logger);
        public StubDbSet<Comment> Comments => new StubDbSet<Comment>(_dbFactory, "comments", "comment_id", _logger);
        public StubDbSet<CommentLike> CommentLikes => new StubDbSet<CommentLike>(_dbFactory, "comment_likes", "like_id", _logger);

        // Smart feed DbSets
        public StubDbSet<UserFeedInteraction> UserFeedInteractions => new StubDbSet<UserFeedInteraction>(_dbFactory, "user_feed_interactions", "interaction_id", _logger);
        public StubDbSet<UserBirdFollow> UserBirdFollows => new StubDbSet<UserBirdFollow>(_dbFactory, "user_bird_follows", "follow_id", _logger);

        // Payout system DbSets
        public StubDbSet<PayoutMethod> PayoutMethods => new StubDbSet<PayoutMethod>(_dbFactory, "payout_methods", "id", _logger);
        public StubDbSet<PayoutTransaction> PayoutTransactions => new StubDbSet<PayoutTransaction>(_dbFactory, "payout_transactions", "id", _logger);
        public StubDbSet<PayoutBalance> PayoutBalances => new StubDbSet<PayoutBalance>(_dbFactory, "payout_balances", "id", _logger);

        // Memorial system DbSets
        public StubDbSet<MemorialMessage> MemorialMessages => new StubDbSet<MemorialMessage>(_dbFactory, "memorial_messages", "id", _logger);
        public StubDbSet<MemorialFundRedirection> MemorialFundRedirections => new StubDbSet<MemorialFundRedirection>(_dbFactory, "memorial_fund_redirections", "id", _logger);

        public StubDatabase Database => new StubDatabase(_dbFactory, _logger);

        public StubEntityEntry<T> Entry<T>(T entity) where T : class => new StubEntityEntry<T>(_logger);
        
        public StubDbSet<T> Set<T>() where T : class
        {
            var tableName = ToSnakeCase(typeof(T).Name);
            // Pluralize table name to match database convention
            if (!tableName.EndsWith("s"))
            {
                tableName += "s";
            }
            var primaryKey = "id"; // Default
            return new StubDbSet<T>(_dbFactory, tableName, primaryKey, _logger);
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
        private readonly ILogger? _logger;

        public StubEntityEntry(ILogger? logger = null)
        {
            _logger = logger;
        }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogWarning("ReloadAsync called on StubEntityEntry. This is a no-op. Use QueryFirstOrDefaultAsync to reload entity.");
            // No-op implementation - return completed task
            return Task.CompletedTask;
        }
    }

    [Obsolete("Migrate to raw SQL.", false)]
    public class StubDbSet<T> : IQueryable<T> where T : class
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly string _tableName;
        private readonly string _primaryKeyColumn;
        private readonly ILogger? _logger;

        public StubDbSet(IDbConnectionFactory dbFactory, string tableName, string primaryKeyColumn = "id", ILogger? logger = null)
        {
            _dbFactory = dbFactory;
            _tableName = tableName;
            _primaryKeyColumn = primaryKeyColumn;
            _logger = logger;
        }

        public Type ElementType => typeof(T);
        public Expression Expression => Expression.Constant(this);
        public IQueryProvider Provider 
        {
            get
            {
                _logger?.LogWarning("IQueryProvider accessed on StubDbSet<{Type}>. Use raw SQL with Dapper.", typeof(T).Name);
                throw new NotSupportedException($"LINQ queries not supported. Use Dapper: await connection.QueryAsync<{typeof(T).Name}>(\"SELECT * FROM {_tableName} WHERE ...\", parameters)");
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            _logger?.LogWarning("GetEnumerator called on StubDbSet<{Type}>. Use QueryAsync.", typeof(T).Name);
            throw new NotSupportedException($"Direct enumeration not supported. Use Dapper: await connection.QueryAsync<{typeof(T).Name}>(\"SELECT * FROM {_tableName}\")");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            _logger?.LogWarning("IEnumerable.GetEnumerator called on StubDbSet<{Type}>. Use QueryAsync.", typeof(T).Name);
            throw new NotSupportedException($"Direct enumeration not supported. Use Dapper: await connection.QueryAsync<{typeof(T).Name}>(\"SELECT * FROM {_tableName}\")");
        }

        public void Add(T entity)
        {
            _logger?.LogWarning("Add called on StubDbSet<{Type}>. Use ExecuteAsync with INSERT.", typeof(T).Name);
            throw new NotSupportedException($"Add not supported. Use Dapper: await connection.ExecuteAsync(\"INSERT INTO {_tableName} (...) VALUES (...)\", entity)");
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _logger?.LogWarning("AddRange called on StubDbSet<{Type}>. Use ExecuteAsync with INSERT.", typeof(T).Name);
            throw new NotSupportedException($"AddRange not supported. Use Dapper: await connection.ExecuteAsync(\"INSERT INTO {_tableName} (...) VALUES (...)\", entities)");
        }

        public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            _logger?.LogWarning("AddRangeAsync called on StubDbSet<{Type}>. Use ExecuteAsync with INSERT.", typeof(T).Name);
            throw new NotSupportedException($"AddRangeAsync not supported. Use Dapper: await connection.ExecuteAsync(\"INSERT INTO {_tableName} (...) VALUES (...)\", entities)");
        }

        public void Remove(T entity)
        {
            _logger?.LogWarning("Remove called on StubDbSet<{Type}>. Use ExecuteAsync with DELETE.", typeof(T).Name);
            throw new NotSupportedException($"Remove not supported. Use Dapper: await connection.ExecuteAsync(\"DELETE FROM {_tableName} WHERE {_primaryKeyColumn} = @Id\", new {{ Id = entityId }})");
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _logger?.LogWarning("RemoveRange called on StubDbSet<{Type}>. Use ExecuteAsync with DELETE.", typeof(T).Name);
            throw new NotSupportedException($"RemoveRange not supported. Use Dapper: await connection.ExecuteAsync(\"DELETE FROM {_tableName} WHERE {_primaryKeyColumn} = @Id\", entities)");
        }

        public async Task<T?> FindAsync(params object[] keyValues)
        {
            if (keyValues.Length == 0)
            {
                _logger?.LogWarning("FindAsync called with no key values on StubDbSet<{Type}>", typeof(T).Name);
                return null;
            }
            
            _logger?.LogInformation("FindAsync called on StubDbSet<{Type}> - executing Dapper query", typeof(T).Name);
            
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            var sql = $"SELECT * FROM {_tableName} WHERE {_primaryKeyColumn} = @Id LIMIT 1";
            return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = keyValues[0] });
        }

        internal IDbConnectionFactory GetDbFactory() => _dbFactory;
        internal string GetTableName() => _tableName;
    }

    [Obsolete("Migrate to raw SQL.", false)]
    public class StubDatabase
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger? _logger;

        public StubDatabase(IDbConnectionFactory dbFactory, ILogger? logger = null)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            _logger?.LogWarning("ExecuteSqlRawAsync called - converting to Dapper ExecuteAsync");
            
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            return await connection.ExecuteAsync(sql, parameters);
        }

        public async Task<int> ExecuteSqlInterpolatedAsync(FormattableString sql, CancellationToken cancellationToken = default)
        {
            _logger?.LogWarning("ExecuteSqlInterpolatedAsync called - this should be converted to parameterized queries");
            
            // Convert FormattableString to parameterized query
            var paramNames = new List<string>();
            var paramValues = new Dictionary<string, object?>();
            
            for (int i = 0; i < sql.ArgumentCount; i++)
            {
                var paramName = $"p{i}";
                paramNames.Add($"@{paramName}");
                paramValues[paramName] = sql.GetArgument(i);
            }
            
            var parameterizedSql = string.Format(sql.Format, paramNames.Cast<object>().ToArray());
            
            using var connection = await _dbFactory.CreateOpenConnectionAsync();
            return await connection.ExecuteAsync(parameterizedSql, paramValues);
        }

        public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbFactory.CreateOpenConnectionAsync();
                return connection.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }

        public bool EnsureCreated()
        {
            _logger?.LogWarning("EnsureCreated called - database schema should be managed via migration scripts");
            return false;
        }

        public Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogWarning("EnsureCreatedAsync called - database schema should be managed via migration scripts");
            return Task.CompletedTask;
        }

        public async Task<StubTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("BeginTransactionAsync called - creating real transaction");
            var connection = await _dbFactory.CreateOpenConnectionAsync();
            var transaction = await connection.BeginTransactionAsync(cancellationToken);
            return new StubTransaction(connection, transaction, _logger);
        }

        public object GetDbConnection()
        {
            _logger?.LogWarning("GetDbConnection called - use IDbConnectionFactory.CreateOpenConnectionAsync() instead");
            throw new NotSupportedException("Use: var connection = await _dbFactory.CreateOpenConnectionAsync();");
        }

        public IQueryable<T> SqlQuery<T>(FormattableString sql)
        {
            _logger?.LogWarning("SqlQuery called - use Dapper QueryAsync instead");
            throw new NotSupportedException($"Use: await connection.QueryAsync<{typeof(T).Name}>(sql, parameters)");
        }
    }

    [Obsolete("Migrate to NpgsqlTransaction.", false)]
    public class StubTransaction : IAsyncDisposable, IDisposable
    {
        private readonly IDbConnection? _connection;
        private readonly IDbTransaction? _transaction;
        private readonly ILogger? _logger;
        private bool _disposed;

        public StubTransaction(IDbConnection? connection = null, IDbTransaction? transaction = null, ILogger? logger = null)
        {
            _connection = connection;
            _transaction = transaction;
            _logger = logger;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                _logger?.LogInformation("Committing transaction");
                _transaction.Commit();
            }
            else
            {
                _logger?.LogWarning("CommitAsync called on stub transaction with no underlying transaction");
            }
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                _logger?.LogWarning("Rolling back transaction");
                _transaction.Rollback();
            }
            else
            {
                _logger?.LogWarning("RollbackAsync called on stub transaction with no underlying transaction");
            }
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
