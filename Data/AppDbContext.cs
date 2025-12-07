using Microsoft.EntityFrameworkCore;
using Wihngo.Models;
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
        public DbSet<SupportTransaction> SupportTransactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // map to lowercase table names to match your DB
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Bird>().ToTable("birds");
            modelBuilder.Entity<Story>().ToTable("stories");
            modelBuilder.Entity<SupportTransaction>().ToTable("supporttransactions");

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

            modelBuilder.Entity<Bird>()
                .HasMany(b => b.Stories)
                .WithOne(s => s.Bird)
                .HasForeignKey(s => s.BirdId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bird>()
                .HasMany(b => b.SupportTransactions)
                .WithOne(t => t.Bird)
                .HasForeignKey(t => t.BirdId)
                .OnDelete(DeleteBehavior.Cascade);

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
