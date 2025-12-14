using Dapper;
using Npgsql;
using System.Text;
using Wihngo.Data;

namespace Wihngo.Helpers
{
    /// <summary>
    /// SQL query helper utilities for common database operations using Dapper.
    /// Provides reusable patterns for SELECT, INSERT, UPDATE, DELETE operations.
    /// </summary>
    public static class SqlQueryHelper
    {
        /// <summary>
        /// Execute a SELECT query and return a list of entities.
        /// </summary>
        public static async Task<List<T>> QueryListAsync<T>(
            this IDbConnectionFactory dbFactory,
            string sql,
            object? parameters = null)
        {
            var connection = await dbFactory.CreateOpenConnectionAsync();
            try
            {
                var results = await connection.QueryAsync<T>(sql, parameters);
                return results.ToList();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Execute a SELECT query and return a single entity or null.
        /// </summary>
        public static async Task<T?> QuerySingleOrDefaultAsync<T>(
            this IDbConnectionFactory dbFactory,
            string sql,
            object? parameters = null)
        {
            var connection = await dbFactory.CreateOpenConnectionAsync();
            try
            {
                return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Execute a COUNT query and return the count.
        /// </summary>
        public static async Task<int> QueryCountAsync(
            this IDbConnectionFactory dbFactory,
            string tableName,
            string? whereClause = null,
            object? parameters = null)
        {
            var connection = await dbFactory.CreateOpenConnectionAsync();
            try
            {
                var sql = $"SELECT COUNT(*) FROM {tableName}";
                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    sql += $" WHERE {whereClause}";
                }
                
                return await connection.ExecuteScalarAsync<int>(sql, parameters);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Execute an EXISTS query and return true/false.
        /// </summary>
        public static async Task<bool> QueryExistsAsync(
            this IDbConnectionFactory dbFactory,
            string tableName,
            string whereClause,
            object parameters)
        {
            var connection = await dbFactory.CreateOpenConnectionAsync();
            try
            {
                var sql = $"SELECT EXISTS(SELECT 1 FROM {tableName} WHERE {whereClause})";
                return await connection.ExecuteScalarAsync<bool>(sql, parameters);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Execute an INSERT, UPDATE, or DELETE query and return rows affected.
        /// </summary>
        public static async Task<int> ExecuteAsync(
            this IDbConnectionFactory dbFactory,
            string sql,
            object? parameters = null)
        {
            var connection = await dbFactory.CreateOpenConnectionAsync();
            try
            {
                return await connection.ExecuteAsync(sql, parameters);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Execute a scalar query (like getting MAX, MIN, SUM, etc).
        /// </summary>
        public static async Task<T> ExecuteScalarAsync<T>(
            this IDbConnectionFactory dbFactory,
            string sql,
            object? parameters = null)
        {
            var connection = await dbFactory.CreateOpenConnectionAsync();
            try
            {
                return await connection.ExecuteScalarAsync<T>(sql, parameters);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Build a paginated SELECT query with ORDER BY, OFFSET, and LIMIT.
        /// </summary>
        public static string BuildPaginatedQuery(
            string baseQuery,
            string orderBy,
            int page,
            int pageSize)
        {
            var offset = (page - 1) * pageSize;
            return $"{baseQuery} ORDER BY {orderBy} OFFSET {offset} LIMIT {pageSize}";
        }

        /// <summary>
        /// Convert C# PascalCase property name to PostgreSQL snake_case column name.
        /// </summary>
        public static string ToSnakeCase(string text)
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

        /// <summary>
        /// Get the standard table name for an entity type.
        /// </summary>
        public static string GetTableName<T>()
        {
            return ToSnakeCase(typeof(T).Name);
        }

        /// <summary>
        /// Build an INSERT SQL statement for an entity.
        /// </summary>
        public static string BuildInsertSql(string tableName, params string[] columns)
        {
            var columnList = string.Join(", ", columns.Select(ToSnakeCase));
            var paramList = string.Join(", ", columns.Select(c => $"@{c}"));
            return $"INSERT INTO {tableName} ({columnList}) VALUES ({paramList})";
        }

        /// <summary>
        /// Build an UPDATE SQL statement for an entity.
        /// </summary>
        public static string BuildUpdateSql(string tableName, string keyColumn, params string[] columns)
        {
            var setClause = string.Join(", ", columns.Select(c => $"{ToSnakeCase(c)} = @{c}"));
            return $"UPDATE {tableName} SET {setClause} WHERE {ToSnakeCase(keyColumn)} = @{keyColumn}";
        }

        /// <summary>
        /// Build a DELETE SQL statement.
        /// </summary>
        public static string BuildDeleteSql(string tableName, string keyColumn)
        {
            return $"DELETE FROM {tableName} WHERE {ToSnakeCase(keyColumn)} = @{keyColumn}";
        }
    }

    /// <summary>
    /// Common SQL query templates for frequently used patterns.
    /// </summary>
    public static class SqlTemplates
    {
        public const string SelectAll = "SELECT * FROM {0}";
        public const string SelectById = "SELECT * FROM {0} WHERE {1} = @Id";
        public const string Count = "SELECT COUNT(*) FROM {0}";
        public const string CountWhere = "SELECT COUNT(*) FROM {0} WHERE {1}";
        public const string Exists = "SELECT EXISTS(SELECT 1 FROM {0} WHERE {1})";
        public const string DeleteById = "DELETE FROM {0} WHERE {1} = @Id";
        
        /// <summary>
        /// Build a LEFT JOIN query for two tables.
        /// </summary>
        public static string LeftJoin(
            string mainTable,
            string mainAlias,
            string joinTable,
            string joinAlias,
            string mainColumn,
            string joinColumn)
        {
            return $@"
                SELECT {mainAlias}.*, {joinAlias}.*
                FROM {mainTable} {mainAlias}
                LEFT JOIN {joinTable} {joinAlias} 
                    ON {mainAlias}.{mainColumn} = {joinAlias}.{joinColumn}";
        }
    }

    /// <summary>
    /// Extension methods for working with Dapper parameters.
    /// </summary>
    public static class DapperParameterExtensions
    {
        /// <summary>
        /// Create a Dapper parameter object from an anonymous type or dictionary.
        /// </summary>
        public static DynamicParameters ToDynamicParameters(this object? parameters)
        {
            var dynamicParams = new DynamicParameters();
            
            if (parameters == null)
                return dynamicParams;

            if (parameters is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    dynamicParams.Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                dynamicParams.AddDynamicParams(parameters);
            }

            return dynamicParams;
        }
    }
}
