using Npgsql;

namespace Wihngo.Data
{
    public class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public NpgsqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<NpgsqlConnection> CreateOpenConnectionAsync()
        {
            var connection = CreateConnection();
            await connection.OpenAsync();
            return connection;
        }
    }
}
