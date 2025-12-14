using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wihngo.Data;
using Wihngo.Models;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;

namespace Wihngo.Database;

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            logger.LogInformation("?? Starting database seeding...");

            // 1. Seed Supported Tokens (if not exists)
            await SeedSupportedTokensAsync(context, logger);

            // 2. Seed Users
            var users = await SeedUsersAsync(context, logger);

            // 3. Seed Birds
            var birds = await SeedBirdsAsync(context, logger, users);

            // 4. Seed Loves
            await SeedLovesAsync(context, logger, users, birds);

            // 5. Seed Support Transactions
            await SeedSupportTransactionsAsync(context, logger, users, birds);

            // 6. Seed Stories
            await SeedStoriesAsync(context, logger, birds);

            // 7. Seed Notifications
            await SeedNotificationsAsync(context, logger, users);

            // 8. Seed Invoices
            await SeedInvoicesAsync(context, logger, users);

            // 9. Seed Crypto Payment Requests
            await SeedCryptoPaymentRequestsAsync(context, logger, users);

            logger.LogInformation("? Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? Error during database seeding");
            throw;
        }
    }

    private static async Task SeedSupportedTokensAsync(AppDbContext context, ILogger logger)
    {
        if (await context.SupportedTokens.AnyAsync())
        {
            logger.LogInformation("??  Supported tokens already exist, skipping...");
            return;
        }

        var tokens = new[]
        {
            new SupportedToken
            {
                Id = Guid.NewGuid(),
                TokenSymbol = "USDC",
                Chain = "solana",
                MintAddress = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
                Decimals = 6,
                IsActive = true,
                TolerancePercent = 0.5m,
                CreatedAt = DateTime.UtcNow
            },
            new SupportedToken
            {
                Id = Guid.NewGuid(),
                TokenSymbol = "EURC",
                Chain = "solana",
                MintAddress = "HzwqbKZw8HxMN6bF2yFZNrht3c2iXXzpKcFu7uBEDKtr",
                Decimals = 6,
                IsActive = true,
                TolerancePercent = 0.5m,
                CreatedAt = DateTime.UtcNow
            },
            new SupportedToken
            {
                Id = Guid.NewGuid(),
                TokenSymbol = "USDC",
                Chain = "base",
                MintAddress = "0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913",
                Decimals = 6,
                IsActive = true,
                TolerancePercent = 0.5m,
                CreatedAt = DateTime.UtcNow
            },
            new SupportedToken
            {
                Id = Guid.NewGuid(),
                TokenSymbol = "EURC",
                Chain = "base",
                MintAddress = "0x60a3E35Cc302bFA44Cb288Bc5a4F316Fdb1adb42",
                Decimals = 6,
                IsActive = true,
                TolerancePercent = 0.5m,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.SupportedTokens.AddRangeAsync(tokens);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {tokens.Length} supported tokens");
    }

    private static async Task<List<User>> SeedUsersAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("??  Users already exist, returning existing...");
            return await context.Users.Take(5).ToListAsync();
        }

        var users = new List<User>
        {
            new User
            {
                UserId = Guid.NewGuid(),
                Name = "Alice Johnson",
                Email = "alice@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow.AddHours(-2),
                LastPasswordChangeAt = DateTime.UtcNow.AddDays(-30)
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Name = "Bob Smith",
                Email = "bob@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                LastLoginAt = DateTime.UtcNow.AddHours(-5),
                LastPasswordChangeAt = DateTime.UtcNow.AddDays(-25)
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Name = "Carol Williams",
                Email = "carol@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                LastLoginAt = DateTime.UtcNow.AddDays(-1),
                LastPasswordChangeAt = DateTime.UtcNow.AddDays(-20)
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Name = "David Brown",
                Email = "david@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                LastLoginAt = DateTime.UtcNow.AddHours(-12),
                LastPasswordChangeAt = DateTime.UtcNow.AddDays(-15)
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Name = "Eve Davis",
                Email = "eve@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                LastLoginAt = DateTime.UtcNow,
                LastPasswordChangeAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {users.Count} users");
        return users;
    }

    private static async Task<List<Bird>> SeedBirdsAsync(AppDbContext context, ILogger logger, List<User> users)
    {
        if (await context.Birds.AnyAsync())
        {
            logger.LogInformation("??  Birds already exist, returning existing...");
            return await context.Birds.Take(10).ToListAsync();
        }

        var birdData = new[]
        {
            ("Sunny", "Anna's Hummingbird", "A vibrant backyard regular who guards her favorite feeder", "https://example.com/sunny.jpg", "https://example.com/sunny.mp4"),
            ("Flash", "Ruby-throated Hummingbird", "Named for his incredible speed and iridescent throat", "https://example.com/flash.jpg", "https://example.com/flash.mp4"),
            ("Bella", "Black-chinned Hummingbird", "A gentle soul who shares feeders with everyone", "https://example.com/bella.jpg", "https://example.com/bella.mp4"),
            ("Spike", "Allen's Hummingbird", "Territorial and fierce, but beautiful to watch", "https://example.com/spike.jpg", "https://example.com/spike.mp4"),
            ("Luna", "Calliope Hummingbird", "Our smallest visitor with the biggest personality", "https://example.com/luna.jpg", "https://example.com/luna.mp4"),
            ("Zippy", "Rufous Hummingbird", "Travels 3,000 miles for migration - a true warrior", "https://example.com/zippy.jpg", "https://example.com/zippy.mp4"),
            ("Jewel", "Costa's Hummingbird", "Desert beauty with a purple crown", "https://example.com/jewel.jpg", "https://example.com/jewel.mp4"),
            ("Blaze", "Broad-tailed Hummingbird", "His wings whistle like a cricket", "https://example.com/blaze.jpg", "https://example.com/blaze.mp4"),
            ("Misty", "Buff-bellied Hummingbird", "Rare visitor from Mexico who stole our hearts", "https://example.com/misty.jpg", "https://example.com/misty.mp4"),
            ("Emerald", "Magnificent Hummingbird", "Living up to his name every single day", "https://example.com/emerald.jpg", "https://example.com/emerald.mp4")
        };

        var birds = new List<Bird>();
        var random = new Random(42); // Seed for consistent results

        for (int i = 0; i < birdData.Length; i++)
        {
            var (name, species, tagline, image, video) = birdData[i];
            var owner = users[i % users.Count];

            var bird = new Bird
            {
                BirdId = Guid.NewGuid(),
                OwnerId = owner.UserId,
                Name = name,
                Species = species,
                Tagline = tagline,
                Description = $"{name} is a wonderful {species} that brings joy to everyone who sees them. They love nectar, flowers, and showing off their amazing flying skills!",
                ImageUrl = image,
                VideoUrl = video,
                LovedCount = random.Next(10, 150),
                DonationCents = random.Next(100, 50000),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 60))
            };

            birds.Add(bird);
        }

        await context.Birds.AddRangeAsync(birds);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {birds.Count} birds");
        return birds;
    }

    private static async Task SeedLovesAsync(AppDbContext context, ILogger logger, List<User> users, List<Bird> birds)
    {
        if (await context.Loves.AnyAsync())
        {
            logger.LogInformation("??  Loves already exist, skipping...");
            return;
        }

        var loves = new List<Love>();
        var random = new Random(42);

        // Each user loves 3-7 random birds
        foreach (var user in users)
        {
            var lovedCount = random.Next(3, 8);
            var lovedBirds = birds.OrderBy(x => random.Next()).Take(lovedCount);

            foreach (var bird in lovedBirds)
            {
                loves.Add(new Love
                {
                    UserId = user.UserId,
                    BirdId = bird.BirdId
                });
            }
        }

        await context.Loves.AddRangeAsync(loves);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {loves.Count} loves");
    }

    private static async Task SeedSupportTransactionsAsync(AppDbContext context, ILogger logger, List<User> users, List<Bird> birds)
    {
        if (await context.SupportTransactions.AnyAsync())
        {
            logger.LogInformation("??  Support transactions already exist, skipping...");
            return;
        }

        var transactions = new List<SupportTransaction>();
        var random = new Random(42);
        var messages = new[]
        {
            "Keep up the great work!",
            "Love seeing updates about your bird!",
            "This made my day ??",
            "Such a beautiful bird!",
            "Thank you for sharing!",
            "More videos please!",
            null, // Some transactions without messages
            null
        };

        // Create 20-30 support transactions
        for (int i = 0; i < random.Next(20, 31); i++)
        {
            var supporter = users[random.Next(users.Count)];
            var bird = birds[random.Next(birds.Count)];
            var amount = random.Next(5, 101); // $5 to $100

            transactions.Add(new SupportTransaction
            {
                TransactionId = Guid.NewGuid(),
                BirdId = bird.BirdId,
                SupporterId = supporter.UserId,
                Amount = amount,
                Message = messages[random.Next(messages.Length)],
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30))
            });
        }

        await context.SupportTransactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {transactions.Count} support transactions");
    }

    private static async Task SeedStoriesAsync(AppDbContext context, ILogger logger, List<Bird> birds)
    {
        if (await context.Stories.AnyAsync())
        {
            logger.LogInformation("??  Stories already exist, skipping...");
            return;
        }

        var storyTemplates = new[]
        {
            ("First Sighting!", "Today we saw {name} for the very first time! What an amazing moment. They came to the feeder around 7am and stayed for almost 10 minutes.", StoryMode.NewBeginning),
            ("Favorite Flower", "{name} has discovered the salvia patch! Watching them dart between the red blooms is absolutely mesmerizing.", StoryMode.PeacefulMoment),
            ("Territorial Display", "Witnessed {name} performing an incredible territorial display today. The dive was breathtaking - they must have reached 60mph!", StoryMode.FunnyMoment),
            ("Bath Time", "Caught {name} taking a bath in the fountain! The water droplets sparkled on their feathers like diamonds.", StoryMode.DailyLife),
            ("Nest Building", "Exciting news! {name} is building a nest in our Japanese maple tree. We're being very careful not to disturb them.", StoryMode.ProgressAndWins)
        };

        var stories = new List<Story>();
        var storyBirds = new List<StoryBird>();
        var random = new Random(42);

        foreach (var bird in birds.Take(7)) // Add stories for first 7 birds
        {
            var storyCount = random.Next(1, 4); // 1-3 stories per bird
            for (int i = 0; i < storyCount; i++)
            {
                var template = storyTemplates[random.Next(storyTemplates.Length)];
                var storyId = Guid.NewGuid();
                
                // Create story with optional mode (sometimes null)
                var story = new Story
                {
                    StoryId = storyId,
                    AuthorId = bird.OwnerId,
                    Content = template.Item2.Replace("{name}", bird.Name),
                    Mode = random.Next(100) < 80 ? template.Item3 : null, // 80% have mood, 20% don't
                    ImageUrl = random.Next(100) < 50 ? $"https://example.com/story_{Guid.NewGuid()}.jpg" : null,
                    VideoUrl = random.Next(100) < 20 ? $"https://example.com/story_{Guid.NewGuid()}.mp4" : null,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                };
                
                stories.Add(story);
                
                // Create StoryBird relationship
                storyBirds.Add(new StoryBird
                {
                    StoryBirdId = Guid.NewGuid(),
                    StoryId = storyId,
                    BirdId = bird.BirdId,
                    CreatedAt = story.CreatedAt
                });
            }
        }

        await context.Stories.AddRangeAsync(stories);
        await context.SaveChangesAsync();
        
        await context.StoryBirds.AddRangeAsync(storyBirds);
        await context.SaveChangesAsync();
        
        logger.LogInformation($"? Seeded {stories.Count} stories with {storyBirds.Count} story-bird relationships");
    }

    private static async Task SeedNotificationsAsync(AppDbContext context, ILogger logger, List<User> users)
    {
        if (await context.Notifications.AnyAsync())
        {
            logger.LogInformation("??  Notifications already exist, skipping...");
            return;
        }

        var notifications = new List<Notification>();
        var random = new Random(42);

        foreach (var user in users.Take(3)) // Add notifications for first 3 users
        {
            notifications.Add(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = user.UserId,
                Type = NotificationType.BirdLoved,
                Title = "Someone loved your bird!",
                Message = "Alice Johnson loved Sunny. You now have 42 loves!",
                Priority = NotificationPriority.Medium,
                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                IsRead = random.Next(100) < 30,
                CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 72))
            });

            notifications.Add(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = user.UserId,
                Type = NotificationType.BirdSupported,
                Title = "New support received!",
                Message = "Bob Smith contributed $25.00 to support Sunny!",
                Priority = NotificationPriority.High,
                Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                IsRead = random.Next(100) < 50,
                CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 48))
            });
        }

        await context.Notifications.AddRangeAsync(notifications);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {notifications.Count} notifications");
    }

    private static async Task SeedInvoicesAsync(AppDbContext context, ILogger logger, List<User> users)
    {
        // Create invoice sequence if it doesn't exist
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'wihngo_invoice_seq') THEN
                    CREATE SEQUENCE wihngo_invoice_seq START 1;
                END IF;
            END $$;
        ");

        if (await context.Invoices.AnyAsync())
        {
            logger.LogInformation("??  Invoices already exist, skipping...");
            return;
        }

        var invoices = new List<Invoice>();
        var random = new Random(42);

        for (int i = 0; i < 5; i++)
        {
            var user = users[random.Next(users.Count)];
            var amount = random.Next(10, 201); // $10 to $200

            invoices.Add(new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"WIH-{1000 + i}",
                UserId = user.UserId,
                AmountFiat = amount,
                FiatCurrency = "USD",
                State = random.Next(100) < 70 ? "PAID" : "CREATED",
                ExpiresAt = DateTime.UtcNow.AddDays(random.Next(-10, 10)),
                IssuedAt = random.Next(100) < 70 ? DateTime.UtcNow.AddDays(-random.Next(0, 5)) : null,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 15)),
                UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 15))
            });
        }

        await context.Invoices.AddRangeAsync(invoices);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {invoices.Count} invoices");
    }

    private static async Task SeedCryptoPaymentRequestsAsync(AppDbContext context, ILogger logger, List<User> users)
    {
        if (await context.CryptoPaymentRequests.AnyAsync())
        {
            logger.LogInformation("??  Crypto payment requests already exist, skipping...");
            return;
        }

        var requests = new List<CryptoPaymentRequest>();
        var random = new Random(42);
        var tokens = await context.SupportedTokens.ToListAsync();

        if (!tokens.Any())
        {
            logger.LogWarning("??  No supported tokens found, skipping crypto payment requests");
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            var user = users[random.Next(users.Count)];
            var token = tokens[random.Next(tokens.Count)];
            var amountUsd = random.Next(10, 101);
            var status = random.Next(100) < 60 ? "completed" : random.Next(100) < 70 ? "pending" : "expired";

            // Generate proper wallet address based on chain
            string walletAddress;
            if (token.Chain == "solana")
            {
                // Solana addresses are base58 encoded, roughly 44 characters
                walletAddress = $"{Guid.NewGuid():N}{Guid.NewGuid():N}".Substring(0, 44);
            }
            else
            {
                // EVM addresses are 42 characters (0x + 40 hex chars)
                walletAddress = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}".Substring(0, 42);
            }

            requests.Add(new CryptoPaymentRequest
            {
                Id = Guid.NewGuid(),
                UserId = user.UserId,
                AmountUsd = amountUsd,
                AmountCrypto = amountUsd, // Simplified - would be calculated based on exchange rate
                Currency = token.TokenSymbol,
                Network = token.Chain,
                ExchangeRate = 1.0m,
                WalletAddress = walletAddress,
                QrCodeData = $"data:image/png;base64,example{i}",
                PaymentUri = $"{token.Chain}:{walletAddress}?amount={amountUsd}",
                RequiredConfirmations = 12,
                Status = status,
                Purpose = "support",
                ExpiresAt = DateTime.UtcNow.AddMinutes(random.Next(-10, 30)),
                CompletedAt = status == "completed" ? DateTime.UtcNow.AddMinutes(-random.Next(0, 20)) : null,
                CreatedAt = DateTime.UtcNow.AddHours(-random.Next(0, 48)),
                UpdatedAt = DateTime.UtcNow.AddHours(-random.Next(0, 48)),
                Confirmations = status == "completed" ? random.Next(12, 100) : 0
            });
        }

        await context.CryptoPaymentRequests.AddRangeAsync(requests);
        await context.SaveChangesAsync();
        logger.LogInformation($"? Seeded {requests.Count} crypto payment requests");
    }
}
