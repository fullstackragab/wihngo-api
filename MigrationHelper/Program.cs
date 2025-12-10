// Migration Helper - Add missing columns to stories table
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString) && args.Length > 0)
{
    connectionString = args[0];
}

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Usage: dotnet run <connection-string>");
    Console.WriteLine("Or set DATABASE_URL environment variable");
    return;
}

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
    
    Console.WriteLine("? Connected to database");
    
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("? Migration completed successfully!");
    Console.WriteLine("  - Added is_highlighted column to stories table");
    Console.WriteLine("  - Added highlight_order column to stories table");
    Console.WriteLine("  - Created index on highlighted stories");
    Console.WriteLine("\nYou can now restart your application. The error should be resolved.");
}
catch (Exception ex)
{
    Console.WriteLine($"? Migration failed: {ex.Message}");
    Console.WriteLine($"  {ex.StackTrace}");
    Environment.Exit(1);
}
