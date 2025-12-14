using Npgsql;

namespace Wihngo.Data
{
    public interface IDbConnectionFactory
    {
        NpgsqlConnection CreateConnection();
        Task<NpgsqlConnection> CreateOpenConnectionAsync();
    }
}
