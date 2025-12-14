using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wihngo.Data;

namespace Wihngo.Controllers
{
    /// <summary>
    /// Development-only endpoints for testing and debugging
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DevController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DevController> _logger;

        public DevController(AppDbContext db, IWebHostEnvironment env, ILogger<DevController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with their statistics (Development only)
        /// </summary>
        [HttpGet("users")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUsersWithStats()
        {
            // Only allow in development
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var users = await _db.Users
                .Select(u => new
                {
                    userId = u.UserId,
                    name = u.Name,
                    email = u.Email,
                    testPassword = "Password123!", // All seeded users have this password
                    emailConfirmed = u.EmailConfirmed,
                    createdAt = u.CreatedAt,
                    lastLoginAt = u.LastLoginAt,
                    
                    // Statistics
                    stats = new
                    {
                        birdsOwned = u.Birds.Count,
                        storiesWritten = u.Stories.Count,
                        birdsLoved = u.Loves.Count,
                        supportGiven = u.SupportTransactions.Count,
                        totalDonated = u.SupportTransactions.Sum(t => (decimal?)t.Amount) ?? 0,
                        notificationsReceived = _db.Notifications.Count(n => n.UserId == u.UserId),
                        unreadNotifications = _db.Notifications.Count(n => n.UserId == u.UserId && !n.IsRead)
                    },
                    
                    // Birds owned details
                    birds = u.Birds.Select(b => new
                    {
                        birdId = b.BirdId,
                        name = b.Name,
                        species = b.Species,
                        lovedCount = b.LovedCount,
                        donationCents = b.DonationCents,
                        donationDollars = b.DonationCents / 100.0,
                        storiesCount = b.Stories.Count
                    }).ToList()
                })
                .OrderByDescending(u => u.stats.birdsOwned)
                .ToListAsync();

            var summary = new
            {
                totalUsers = users.Count,
                totalBirds = await _db.Birds.CountAsync(),
                totalStories = await _db.Stories.CountAsync(),
                totalLoves = await _db.Loves.CountAsync(),
                totalTransactions = await _db.SupportTransactions.CountAsync(),
                message = "?? Use email and testPassword to login. All test users have password: Password123!",
                users = users
            };

            return Ok(summary);
        }

        /// <summary>
        /// Get detailed stats for a specific user by email
        /// </summary>
        /*
        // TEMPORARILY DISABLED: This endpoint uses complex EF Core Include/ThenInclude chains
        // that need to be migrated to raw SQL queries. See MIGRATION_STATUS_FINAL.md
        [HttpGet("users/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserDetailsByEmail(string email)
        {
            // Only allow in development
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var user = await _db.Users
                .Include(u => u.Birds)
                    .ThenInclude(b => b.Stories)
                .Include(u => u.Birds)
                    .ThenInclude(b => b.SupportTransactions)
                .Include(u => u.Loves)
                    .ThenInclude(l => l.Bird)
                .Include(u => u.SupportTransactions)
                    .ThenInclude(t => t.Bird)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var result = new
            {
                // Login credentials
                credentials = new
                {
                    email = user.Email,
                    password = "Password123!",
                    note = "Use these credentials to login via POST /api/auth/login"
                },

                // User info
                userInfo = new
                {
                    userId = user.UserId,
                    name = user.Name,
                    email = user.Email,
                    emailConfirmed = user.EmailConfirmed,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
                },

                // Statistics
                statistics = new
                {
                    birdsOwned = user.Birds.Count,
                    totalStoriesWritten = user.Stories.Count,
                    birdsLovedByUser = user.Loves.Count,
                    supportTransactionsGiven = user.SupportTransactions.Count,
                    totalAmountDonated = user.SupportTransactions.Sum(t => (decimal?)t.Amount) ?? 0
                },

                // Birds owned with details
                birdsOwned = user.Birds.Select(b => new
                {
                    birdId = b.BirdId,
                    name = b.Name,
                    species = b.Species,
                    tagline = b.Tagline,
                    lovedCount = b.LovedCount,
                    donationCents = b.DonationCents,
                    donationDollars = b.DonationCents / 100.0,
                    storiesCount = b.Stories.Count,
                    supportTransactionsReceived = b.SupportTransactions.Count,
                    stories = b.Stories.Select(s => new
                    {
                        storyId = s.StoryId,
                        content = s.Content.Length > 100 ? s.Content.Substring(0, 100) + "..." : s.Content,
                        createdAt = s.CreatedAt
                    }).ToList()
                }).ToList(),

                // Birds loved by this user
                birdsLoved = user.Loves.Select(l => new
                {
                    birdId = l.Bird.BirdId,
                    birdName = l.Bird.Name,
                    species = l.Bird.Species,
                    ownerName = _db.Users.Where(u => u.UserId == l.Bird.OwnerId).Select(u => u.Name).FirstOrDefault()
                }).ToList(),

                // Support transactions given
                supportGiven = user.SupportTransactions.Select(t => new
                {
                    transactionId = t.TransactionId,
                    amount = t.Amount,
                    message = t.Message,
                    birdName = t.Bird.Name,
                    createdAt = t.CreatedAt
                }).ToList()
            };

            return Ok(result);
        }
        */

        /// <summary>
        /// Get quick test credentials summary
        /// </summary>
        [HttpGet("test-credentials")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTestCredentials()
        {
            // Only allow in development
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            // Use raw SQL with aggregations
            var sql = @"
                SELECT 
                    u.name,
                    u.email,
                    'Password123!' as password,
                    COUNT(DISTINCT b.bird_id) as birds_count,
                    COUNT(DISTINCT s.story_id) as stories_count,
                    CASE WHEN COUNT(DISTINCT b.bird_id) > 0 OR 
                              COUNT(DISTINCT s.story_id) > 0 OR 
                              COUNT(DISTINCT l.bird_id) > 0 
                         THEN true ELSE false END as has_data
                FROM users u
                LEFT JOIN birds b ON u.user_id = b.owner_id
                LEFT JOIN stories s ON u.user_id = s.author_id
                LEFT JOIN loves l ON u.user_id = l.user_id
                GROUP BY u.user_id, u.name, u.email
                ORDER BY birds_count DESC, stories_count DESC";

            var connection = await _db.GetDbFactory().CreateOpenConnectionAsync();
            try
            {
                var users = await connection.QueryAsync<dynamic>(sql);
                var usersList = users.ToList();
                var recommended = usersList.FirstOrDefault(u => u.has_data);

                return Ok(new
                {
                    message = "?? Test User Credentials (Development Only)",
                    note = "All users have the same password: Password123!",
                    recommendedUser = recommended != null ? new
                    {
                        name = (string)recommended.name,
                        email = (string)recommended.email,
                        password = (string)recommended.password,
                        reason = $"Has {recommended.birds_count} birds and {recommended.stories_count} stories"
                    } : null,
                    allUsers = usersList,
                    loginEndpoint = "POST /api/auth/login",
                    exampleRequest = new
                    {
                        email = recommended?.email ?? "alice@example.com",
                        password = "Password123!"
                    }
                });
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
