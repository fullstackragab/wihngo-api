using System;
using Microsoft.Extensions.DependencyInjection;
using Wihngo.Data;
using Wihngo.Models;
using Wihngo.Models.Entities;
using Wihngo.Models.Enums;
using Wihngo.Models.Payout;
using Wihngo.Extensions;
using Dapper;

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

            // 8. Seed Payout System Data
            await SeedPayoutDataAsync(context, logger);

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
            ("Sunny", "Anna's Hummingbird", "A vibrant backyard regular who guards her favorite feeder", "https://example.com/sunny.jpg"),
            ("Flash", "Ruby-throated Hummingbird", "Named for his incredible speed and iridescent throat", "https://example.com/flash.jpg"),
            ("Bella", "Black-chinned Hummingbird", "A gentle soul who shares feeders with everyone", "https://example.com/bella.jpg"),
            ("Spike", "Allen's Hummingbird", "Territorial and fierce, but beautiful to watch", "https://example.com/spike.jpg"),
            ("Luna", "Calliope Hummingbird", "Our smallest visitor with the biggest personality", "https://example.com/luna.jpg"),
            ("Zippy", "Rufous Hummingbird", "Travels 3,000 miles for migration - a true warrior", "https://example.com/zippy.jpg"),
            ("Jewel", "Costa's Hummingbird", "Desert beauty with a purple crown", "https://example.com/jewel.jpg"),
            ("Blaze", "Broad-tailed Hummingbird", "His wings whistle like a cricket", "https://example.com/blaze.jpg"),
            ("Misty", "Buff-bellied Hummingbird", "Rare visitor from Mexico who stole our hearts", "https://example.com/misty.jpg"),
            ("Emerald", "Magnificent Hummingbird", "Living up to his name every single day", "https://example.com/emerald.jpg")
        };

        var birds = new List<Bird>();
        var random = new Random(42); // Seed for consistent results

        for (int i = 0; i < birdData.Length; i++)
        {
            var (name, species, tagline, image) = birdData[i];
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
        var random = new Random(42);

        foreach (var bird in birds.Take(7)) // Add stories for first 7 birds
        {
            var storyCount = random.Next(1, 4); // 1-3 stories per bird
            for (int i = 0; i < storyCount; i++)
            {
                var template = storyTemplates[random.Next(storyTemplates.Length)];
                
                // Create story with bird_id foreign key (one bird per story)
                var story = new Story
                {
                    StoryId = Guid.NewGuid(),
                    AuthorId = bird.OwnerId,
                    BirdId = bird.BirdId, // Direct foreign key relationship
                    Content = template.Item2.Replace("{name}", bird.Name),
                    Mode = random.Next(100) < 80 ? template.Item3 : null, // 80% have mood, 20% don't
                    ImageUrl = random.Next(100) < 50 ? $"https://example.com/story_{Guid.NewGuid()}.jpg" : null,
                    VideoUrl = random.Next(100) < 20 ? $"https://example.com/story_{Guid.NewGuid()}.mp4" : null,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                };
                
                stories.Add(story);
            }
        }

        await context.Stories.AddRangeAsync(stories);
        await context.SaveChangesAsync();
        
        logger.LogInformation($"?? Seeded {stories.Count} stories (one bird per story)");
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

    private static async Task SeedPayoutDataAsync(AppDbContext context, ILogger logger)
    {
        if (await context.PayoutMethods.AnyAsync())
        {
            logger.LogInformation("??  Payout data already exists, skipping...");
            return;
        }

        var random = new Random(42);
        var dbFactory = context.GetDbFactory();
        using var connection = await dbFactory.CreateOpenConnectionAsync();

        // Get bird owners (users who own birds)
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT owner_id FROM birds LIMIT 5";
        var ownerIds = new List<Guid>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ownerIds.Add(reader.GetGuid(0));
        }
        await reader.CloseAsync();

        if (!ownerIds.Any())
        {
            logger.LogWarning("??  No bird owners found, skipping payout data");
            return;
        }

        var methodCount = 0;
        var transactionCount = 0;

        foreach (var ownerId in ownerIds)
        {
            // Create 1-2 payout methods per owner
            var methodTypes = new[] { "iban", "paypal", "usdc-solana" };
            var methodsToCreate = random.Next(1, 3);

            for (int i = 0; i < methodsToCreate; i++)
            {
                var methodType = methodTypes[random.Next(methodTypes.Length)];
                var isDefault = i == 0; // First method is default

                using var insertMethod = connection.CreateCommand();
                insertMethod.CommandText = @"
                    INSERT INTO payout_methods (
                        id, user_id, method_type, is_default, is_verified,
                        account_holder_name, iban, bic, bank_name,
                        paypal_email, wallet_address, network, currency,
                        created_at, updated_at
                    ) VALUES (
                        @id, @user_id, @method_type, @is_default, @is_verified,
                        @account_holder_name, @iban, @bic, @bank_name,
                        @paypal_email, @wallet_address, @network, @currency,
                        @created_at, @updated_at
                    ) RETURNING id";

                var methodId = Guid.NewGuid();
                insertMethod.Parameters.AddWithValue("id", methodId);
                insertMethod.Parameters.AddWithValue("user_id", ownerId);
                insertMethod.Parameters.AddWithValue("method_type", methodType);
                insertMethod.Parameters.AddWithValue("is_default", isDefault);
                insertMethod.Parameters.AddWithValue("is_verified", true);
                
                // Method-specific fields
                if (methodType == "iban")
                {
                    insertMethod.Parameters.AddWithValue("account_holder_name", "Test User");
                    insertMethod.Parameters.AddWithValue("iban", $"DE89370400440532{random.Next(100000, 999999)}");
                    insertMethod.Parameters.AddWithValue("bic", "COBADEFFXXX");
                    insertMethod.Parameters.AddWithValue("bank_name", "Test Bank");
                    insertMethod.Parameters.AddWithValue("paypal_email", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("wallet_address", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("network", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("currency", DBNull.Value);
                }
                else if (methodType == "paypal")
                {
                    insertMethod.Parameters.AddWithValue("account_holder_name", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("iban", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("bic", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("bank_name", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("paypal_email", $"test{random.Next(1000, 9999)}@paypal.com");
                    insertMethod.Parameters.AddWithValue("wallet_address", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("network", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("currency", DBNull.Value);
                }
                else // usdc-solana
                {
                    insertMethod.Parameters.AddWithValue("account_holder_name", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("iban", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("bic", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("bank_name", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("paypal_email", DBNull.Value);
                    insertMethod.Parameters.AddWithValue("wallet_address", $"{Guid.NewGuid():N}{Guid.NewGuid():N}".Substring(0, 44));
                    insertMethod.Parameters.AddWithValue("network", "solana");
                    insertMethod.Parameters.AddWithValue("currency", "usdc");
                }

                insertMethod.Parameters.AddWithValue("created_at", DateTime.UtcNow.AddDays(-random.Next(5, 30)));
                insertMethod.Parameters.AddWithValue("updated_at", DateTime.UtcNow.AddDays(-random.Next(0, 5)));

                await insertMethod.ExecuteNonQueryAsync();
                methodCount++;

                // Create 0-2 payout transactions for this method
                if (isDefault && random.Next(100) < 70) // 70% chance for default method
                {
                    var txCount = random.Next(0, 3);
                    for (int j = 0; j < txCount; j++)
                    {
                        var amount = random.Next(20, 101); // �20-�100
                        var platformFee = amount * 0.05m;
                        var providerFee = methodType == "iban" ? 1.00m : amount * 0.02m;
                        var netAmount = amount - platformFee - providerFee;
                        var status = j == 0 ? "completed" : (random.Next(100) < 80 ? "completed" : "failed");

                        using var insertTx = connection.CreateCommand();
                        insertTx.CommandText = @"
                            INSERT INTO payout_transactions (
                                id, user_id, payout_method_id, amount, currency, status,
                                platform_fee, provider_fee, net_amount,
                                scheduled_at, processed_at, completed_at, transaction_id,
                                created_at, updated_at
                            ) VALUES (
                                @id, @user_id, @payout_method_id, @amount, @currency, @status,
                                @platform_fee, @provider_fee, @net_amount,
                                @scheduled_at, @processed_at, @completed_at, @transaction_id,
                                @created_at, @updated_at
                            )";

                        var scheduledDate = DateTime.UtcNow.AddDays(-random.Next(1, 15));
                        insertTx.Parameters.AddWithValue("id", Guid.NewGuid());
                        insertTx.Parameters.AddWithValue("user_id", ownerId);
                        insertTx.Parameters.AddWithValue("payout_method_id", methodId);
                        insertTx.Parameters.AddWithValue("amount", amount);
                        insertTx.Parameters.AddWithValue("currency", "EUR");
                        insertTx.Parameters.AddWithValue("status", status);
                        insertTx.Parameters.AddWithValue("platform_fee", platformFee);
                        insertTx.Parameters.AddWithValue("provider_fee", providerFee);
                        insertTx.Parameters.AddWithValue("net_amount", netAmount);
                        insertTx.Parameters.AddWithValue("scheduled_at", scheduledDate);
                        insertTx.Parameters.AddWithValue("processed_at", status != "pending" ? scheduledDate.AddMinutes(5) : DBNull.Value);
                        insertTx.Parameters.AddWithValue("completed_at", status == "completed" ? scheduledDate.AddMinutes(30) : DBNull.Value);
                        insertTx.Parameters.AddWithValue("transaction_id", status == "completed" ? $"TXN_{Guid.NewGuid():N}".Substring(0, 20) : DBNull.Value);
                        insertTx.Parameters.AddWithValue("created_at", scheduledDate);
                        insertTx.Parameters.AddWithValue("updated_at", scheduledDate);

                        await insertTx.ExecuteNonQueryAsync();
                        transactionCount++;
                    }
                }
            }
        }

        logger.LogInformation($"?? Seeded {methodCount} payout methods and {transactionCount} payout transactions");

        // ===============================================
        // Memorial Test Data
        // ===============================================

        // Special case: create two specific users for memorial testing
        var memorialUserId1 = Guid.NewGuid();
        var memorialUserId2 = Guid.NewGuid();

        await connection.ExecuteAsync(@"
            INSERT INTO users (user_id, name, email, password_hash, email_confirmed, created_at, last_login_at, last_password_change_at)
            VALUES 
                (@Id1, 'Memorial User One', 'memorial1@example.com', 'passwordhash1', true, @Now, @Now, @Now),
                (@Id2, 'Memorial User Two', 'memorial2@example.com', 'passwordhash2', true, @Now, @Now, @Now)
            ON CONFLICT (user_id) DO NOTHING",
            new { Id1 = memorialUserId1, Id2 = memorialUserId2, Now = DateTime.UtcNow });

        Console.WriteLine($"? Memorial users seeded: {memorialUserId1}, {memorialUserId2}");

        // =========================================================================
        // Memorial Payout Balances
        // =========================================================================

        // Insert or update memorial payout balances
        await connection.ExecuteAsync(@"
            INSERT INTO payout_balances (user_id, available_balance, pending_balance, currency, next_payout_date, updated_at)
            VALUES 
                (@User1, 50.00, 15.00, 'EUR', @NextMonth, @Now),
                (@User2, 120.50, 30.00, 'EUR', @NextMonth, @Now)
            ON CONFLICT (user_id) DO UPDATE SET
                available_balance = EXCLUDED.available_balance,
                pending_balance = EXCLUDED.pending_balance,
                updated_at = EXCLUDED.updated_at",
            new { User1 = memorialUserId1, User2 = memorialUserId2, NextMonth = DateTime.UtcNow.AddMonths(1), Now = DateTime.UtcNow });

        Console.WriteLine("? Payout balances seeded");

        // =========================================================================
        // Memorial Birds and Messages
        // ============================================================================

        // Create a memorial bird
        var memorialBirdId = Guid.NewGuid();
        await connection.ExecuteAsync(@"
            INSERT INTO birds (bird_id, owner_id, name, species, tagline, description, image_url, loved_count, supported_count, created_at, is_memorial, memorial_date, memorial_reason, funds_redirection_choice)
            VALUES (@BirdId, @OwnerId, @Name, @Species, @Tagline, @Description, @ImageUrl, @LovedCount, @SupportedCount, @CreatedAt, true, @MemorialDate, @MemorialReason, @FundsRedirection)
            ON CONFLICT (bird_id) DO NOTHING",
            new
            {
                BirdId = memorialBirdId,
                OwnerId = memorialUserId1,
                Name = "Tweety",
                Species = "Canary",
                Tagline = "A beautiful yellow canary who brought joy to many",
                Description = "Thank you to everyone who supported Tweety. Your love made a real difference in their life. Tweety passed away peacefully after a wonderful life full of song and sunshine.",
                ImageUrl = "birds/memorial-bird.jpg",
                LovedCount = 523,
                SupportedCount = 142,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                MemorialDate = DateTime.UtcNow.AddDays(-7),
                MemorialReason = "Passed away peacefully after a wonderful life",
                FundsRedirection = "emergency_fund"
            });

        Console.WriteLine($"? Memorial bird seeded: {memorialBirdId}");

        // Add memorial messages
        var memorialMessages = new[]
        {
            new { MessageId = Guid.NewGuid(), UserId = memorialUserId2, Message = "Tweety was such a beautiful bird. Sending love to the owner. ??" },
            new { MessageId = Guid.NewGuid(), UserId = memorialUserId1, Message = "I will miss seeing Tweety's happy chirps. Rest in peace little one. ???" },
            new { MessageId = Guid.NewGuid(), UserId = memorialUserId2, Message = "Thank you for sharing Tweety with us. Their memory will live on forever." }
        };

        foreach (var msg in memorialMessages)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO memorial_messages (message_id, bird_id, user_id, message, is_approved, created_at, updated_at)
                VALUES (@MessageId, @BirdId, @UserId, @Message, true, @Now, @Now)
                ON CONFLICT (message_id) DO NOTHING",
                new
                {
                    MessageId = msg.MessageId,
                    BirdId = memorialBirdId,
                    UserId = msg.UserId,
                    Message = msg.Message,
                    Now = DateTime.UtcNow
                });
        }

        Console.WriteLine($"? {memorialMessages.Length} memorial messages seeded");

        // Add fund redirection record
        var redirectionId = Guid.NewGuid();
        await connection.ExecuteAsync(@"
            INSERT INTO memorial_fund_redirections 
            (redirection_id, bird_id, previous_owner_id, remaining_balance, redirection_type, status, processed_at, transaction_id, notes, created_at, updated_at)
            VALUES (@RedirectionId, @BirdId, @OwnerId, @Balance, @Type, @Status, @ProcessedAt, @TxId, @Notes, @Now, @Now)
            ON CONFLICT (redirection_id) DO NOTHING",
            new
            {
                RedirectionId = redirectionId,
                BirdId = memorialBirdId,
                OwnerId = memorialUserId1,
                Balance = 50.00m,
                Type = "emergency_fund",
                Status = "completed",
                ProcessedAt = DateTime.UtcNow.AddDays(-6),
                TxId = "EMERG-12345678",
                Notes = "Transferred $50.00 to platform emergency fund",
                Now = DateTime.UtcNow
            });

        Console.WriteLine($"? Memorial fund redirection seeded: {redirectionId}");

        Console.WriteLine("==============================================");
    }
}
