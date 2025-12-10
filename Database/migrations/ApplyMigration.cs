// Quick migration helper - Run this to add missing columns
// Usage: dotnet run --project . -- migrate

using Npgsql;
using Microsoft.Extensions.Configuration;

public class ApplyMigration
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "migrate")
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres";

            await RunMigrationAsync(connectionString);
        }
    }

    private static async Task RunMigrationAsync(string connectionString)
    {
        Console.WriteLine("Applying migration to add missing columns to stories table...");

        var sql = @"
BEGIN;

ALTER TABLE public.stories 
ADD COLUMN IF NOT EXISTS is_highlighted BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS highlight_order INTEGER;

CREATE INDEX IF NOT EXISTS idx_stories_highlighted 
ON public.stories (is_highlighted, highlight_order) 
WHERE is_highlighted = TRUE;

COMMIT;
";

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            
            Console.WriteLine("? Migration completed successfully!");
            Console.WriteLine("  - Added is_highlighted column to stories table");
            Console.WriteLine("  - Added highlight_order column to stories table");
            Console.WriteLine("  - Created index on highlighted stories");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Migration failed: {ex.Message}");
            throw;
        }
    }
}
