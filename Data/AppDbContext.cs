using Microsoft.EntityFrameworkCore;
using Wihngo.Models;

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
        }
    }
}
