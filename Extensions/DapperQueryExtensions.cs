using System.Linq.Expressions;
using System.Text;
using Dapper;
using Wihngo.Data;

namespace Wihngo.Extensions
{
    /// <summary>
    /// Simple extensions for StubDbSet to support basic operations with raw SQL + Dapper.
    /// For complex queries, use IDbConnectionFactory directly with custom SQL.
    /// 
    /// These extensions provide minimal compatibility for simple CRUD operations.
    /// </summary>
    public static class DapperQueryExtensions
    {
        static DapperQueryExtensions()
        {
            // Configure Dapper to handle snake_case column names
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        #region Basic Query Extensions (Simple Cases Only)

        /// <summary>
        /// Execute a simple query with optional WHERE clause.
        /// For complex queries with joins, use raw SQL directly.
        /// </summary>
        public static async Task<List<T>> ToListAsync<T>(
            this StubDbSet<T> source,
            CancellationToken cancellationToken = default) where T : class
        {
            var connection = await source.GetDbFactory().CreateOpenConnectionAsync();
            try
            {
                var sql = $"SELECT * FROM {source.GetTableName()}";
                var results = await connection.QueryAsync<T>(sql);
                return results.ToList();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Get first record or null. For complex queries, use raw SQL.
        /// </summary>
        public static async Task<T?> FirstOrDefaultAsync<T>(
            this StubDbSet<T> source,
            CancellationToken cancellationToken = default) where T : class
        {
            var connection = await source.GetDbFactory().CreateOpenConnectionAsync();
            try
            {
                var sql = $"SELECT * FROM {source.GetTableName()} LIMIT 1";
                return await connection.QueryFirstOrDefaultAsync<T>(sql);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Get first record matching predicate or null.
        /// WARNING: This throws NotImplementedException - migrate to raw SQL for predicates.
        /// </summary>
        public static Task<T?> FirstOrDefaultAsync<T>(
            this StubDbSet<T> source,
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default) where T : class
        {
            throw new NotImplementedException(
                "FirstOrDefaultAsync with predicate is not supported. " +
                "Use raw SQL: var result = await connection.QueryFirstOrDefaultAsync<T>(\"SELECT * FROM table WHERE column = @Value\", new { Value = value });");
        }

        /// <summary>
        /// Get first record or throw. For complex queries, use raw SQL.
        /// </summary>
        public static async Task<T> FirstAsync<T>(
            this StubDbSet<T> source,
            CancellationToken cancellationToken = default) where T : class
        {
            var result = await source.FirstOrDefaultAsync(cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result;
        }

        /// <summary>
        /// Check if any records exist.
        /// </summary>
        public static async Task<bool> AnyAsync<T>(
            this StubDbSet<T> source,
            CancellationToken cancellationToken = default) where T : class
        {
            return await source.CountAsync(cancellationToken) > 0;
        }

        /// <summary>
        /// Check if any records matching predicate exist.
        /// WARNING: This throws NotImplementedException - migrate to raw SQL for predicates.
        /// </summary>
        public static Task<bool> AnyAsync<T>(
            this StubDbSet<T> source,
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default) where T : class
        {
            throw new NotImplementedException(
                "AnyAsync with predicate is not supported. " +
                "Use raw SQL: var exists = await connection.ExecuteScalarAsync<bool>(\"SELECT EXISTS(SELECT 1 FROM table WHERE column = @Value)\", new { Value = value });");
        }

        /// <summary>
        /// Get count of records.
        /// </summary>
        public static async Task<int> CountAsync<T>(
            this StubDbSet<T> source,
            CancellationToken cancellationToken = default) where T : class
        {
            var connection = await source.GetDbFactory().CreateOpenConnectionAsync();
            try
            {
                var sql = $"SELECT COUNT(*) FROM {source.GetTableName()}";
                return await connection.ExecuteScalarAsync<int>(sql);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        #endregion

        #region Chainable Methods (Return StubDbSet for chaining)

        /// <summary>
        /// No-op for compatibility. Use raw SQL WHERE clauses instead.
        /// </summary>
        public static StubDbSet<T> Where<T>(
            this StubDbSet<T> source,
            Expression<Func<T, bool>> predicate) where T : class
        {
            // For stub compatibility - actual filtering should be in SQL
            return source;
        }

        /// <summary>
        /// No-op for compatibility. Use raw SQL ORDER BY instead.
        /// </summary>
        public static StubDbSet<T> OrderBy<T, TKey>(
            this StubDbSet<T> source,
            Expression<Func<T, TKey>> keySelector) where T : class
        {
            return source;
        }

        /// <summary>
        /// No-op for compatibility. Use raw SQL ORDER BY DESC instead.
        /// </summary>
        public static StubDbSet<T> OrderByDescending<T, TKey>(
            this StubDbSet<T> source,
            Expression<Func<T, TKey>> keySelector) where T : class
        {
            return source;
        }

        /// <summary>
        /// No-op for compatibility. Use raw SQL OFFSET instead.
        /// </summary>
        public static StubDbSet<T> Skip<T>(this StubDbSet<T> source, int count) where T : class
        {
            return source;
        }

        /// <summary>
        /// No-op for compatibility. Use raw SQL LIMIT instead.
        /// </summary>
        public static StubDbSet<T> Take<T>(this StubDbSet<T> source, int count) where T : class
        {
            return source;
        }

        /// <summary>
        /// No-op for compatibility. Use raw SQL SELECT columns instead.
        /// </summary>
        public static StubDbSet<TResult> Select<T, TResult>(
            this StubDbSet<T> source,
            Expression<Func<T, TResult>> selector) where T : class where TResult : class
        {
            throw new NotImplementedException("Use raw SQL SELECT with specific columns and QueryAsync<TResult>");
        }

        /// <summary>
        /// No-op for compatibility. Use raw SQL JOIN instead.
        /// </summary>
        public static StubDbSet<T> Include<T, TProperty>(
            this StubDbSet<T> source,
            Expression<Func<T, TProperty>> navigationPropertyPath) where T : class
        {
            // For stub compatibility - actual joins should be in SQL
            return source;
        }

        /// <summary>
        /// No-op for compatibility. Use raw SQL nested JOIN instead.
        /// </summary>
        public static StubDbSet<T> ThenInclude<T, TPreviousProperty, TProperty>(
            this StubDbSet<T> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where T : class
        {
            return source;
        }

        /// <summary>
        /// No-op for compatibility.
        /// </summary>
        public static StubDbSet<T> AsNoTracking<T>(this StubDbSet<T> source) where T : class
        {
            return source;
        }

        #endregion

        #region Helper Methods

        private static string ToSnakeCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var result = new StringBuilder();
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

        #endregion
    }

    /// <summary>
    /// SQL helper utilities for common query patterns.
    /// </summary>
    public static class SqlHelpers
    {
        /// <summary>
        /// Convert C# property name to snake_case column name.
        /// </summary>
        public static string ToSnakeCase(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return propertyName;
            
            var result = new StringBuilder();
            result.Append(char.ToLower(propertyName[0]));
            
            for (int i = 1; i < propertyName.Length; i++)
            {
                if (char.IsUpper(propertyName[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(propertyName[i]));
                }
                else
                {
                    result.Append(propertyName[i]);
                }
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Get table name from entity type (converts to snake_case and pluralizes).
        /// </summary>
        public static string GetTableName<T>()
        {
            var name = ToSnakeCase(typeof(T).Name);
            // Pluralize table name to match database convention
            if (!name.EndsWith("s"))
            {
                name += "s";
            }
            return name;
        }

        /// <summary>
        /// Get table name with optional pluralization.
        /// </summary>
        public static string GetTableName(Type entityType, bool pluralize = false)
        {
            var name = ToSnakeCase(entityType.Name);
            if (pluralize && !name.EndsWith("s"))
            {
                name += "s";
            }
            return name;
        }
    }
}
