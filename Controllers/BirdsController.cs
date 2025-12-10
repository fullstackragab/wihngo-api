namespace Wihngo.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Models.Enums;
    using Wihngo.Services.Interfaces;
    using System.Text.Json;

    [Route("api/[controller]")]
    [ApiController]
    public class BirdsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public BirdsController(AppDbContext db, IMapper mapper, INotificationService notificationService)
        {
            _db = db;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        private Guid? GetUserIdClaim()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return null;
            return userId;
        }

        private async Task<bool> EnsureOwner(Guid birdId)
        {
            var userId = GetUserIdClaim();
            if (userId == null) return false;
            var bird = await _db.Birds.FindAsync(birdId);
            if (bird == null) return false;
            return bird.OwnerId == userId.Value;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BirdSummaryDto>>> Get()
        {
            var userId = GetUserIdClaim();
            
            // Get all bird IDs this user has loved (if authenticated)
            HashSet<Guid> lovedBirdIds = new HashSet<Guid>();
            if (userId.HasValue)
            {
                var lovedIds = await _db.Loves
                    .Where(l => l.UserId == userId.Value)
                    .Select(l => l.BirdId)
                    .ToListAsync();
                lovedBirdIds = new HashSet<Guid>(lovedIds);
            }

            // Project only necessary fields and counts to avoid loading all support transaction details
            var birds = await _db.Birds
                .AsNoTracking()
                .Select(b => new BirdSummaryDto
                {
                    BirdId = b.BirdId,
                    Name = b.Name,
                    Species = b.Species,
                    ImageUrl = b.ImageUrl,
                    Tagline = b.Tagline,
                    LovedBy = b.LovedCount,
                    SupportedBy = b.SupportTransactions.Count(),
                    OwnerId = b.OwnerId,
                    IsLoved = false // Will be set after query
                })
                .ToListAsync();

            // Set IsLoved status for each bird
            foreach (var bird in birds)
            {
                bird.IsLoved = lovedBirdIds.Contains(bird.BirdId);
            }

            return Ok(birds);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BirdProfileDto>> Get(Guid id)
        {
            var bird = await _db.Birds
                .AsNoTracking()
                .Include(b => b.Owner)
                .Include(b => b.Stories)
                .Include(b => b.SupportTransactions)
                .FirstOrDefaultAsync(b => b.BirdId == id);

            if (bird == null) return NotFound();

            var dto = _mapper.Map<BirdProfileDto>(bird);

            // Fill counts explicitly
            dto.LovedBy = bird.LovedCount;
            dto.SupportedBy = bird.SupportTransactions?.Count ?? 0;
            dto.ImageUrl = bird.ImageUrl;

            // Check if current user has loved this bird
            var userId = GetUserIdClaim();
            if (userId.HasValue)
            {
                dto.IsLoved = await _db.Loves.AnyAsync(l => l.UserId == userId.Value && l.BirdId == id);
            }
            else
            {
                dto.IsLoved = false;
            }

            // Map owner summary
            if (bird.Owner != null)
            {
                dto.Owner = new UserSummaryDto { UserId = bird.Owner.UserId, Name = bird.Owner.Name };
            }

            // Populate personality/fun facts/conservation if separate tables exist
            // (left as placeholders if not present)
            dto.Personality = dto.Personality ?? new System.Collections.Generic.List<string>
            {
                "Fearless and territorial",
                "Incredibly vocal for their size",
                "Early risers who sing before dawn",
                "Devoted parents"
            };

            dto.FunFacts = dto.FunFacts ?? new System.Collections.Generic.List<string>
            {
                "Males perform spectacular dive displays, reaching speeds of 60 mph",
                "They can remember every flower they've visited",
                "Their heart beats up to 1,260 times per minute",
                "They're one of the few hummingbirds that sing"
            };

            dto.Conservation ??= new ConservationDto
            {
                Status = "Least Concern",
                Needs = "Native plant gardens, year-round nectar sources, pesticide-free habitats"
            };

            return Ok(dto);
        }

        [HttpGet("{id}/profile")]
        public async Task<ActionResult<BirdProfileDto>> Profile(Guid id)
        {
            // Reuse existing profile logic
            return await Get(id);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<BirdSummaryDto>> Post([FromBody] BirdCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdClaim();
            if (userId == null) return Unauthorized();

            var bird = _mapper.Map<Bird>(dto);
            bird.BirdId = Guid.NewGuid();
            bird.OwnerId = userId.Value;
            bird.CreatedAt = DateTime.UtcNow;

            _db.Birds.Add(bird);
            await _db.SaveChangesAsync();

            var summary = _mapper.Map<BirdSummaryDto>(bird);

            return CreatedAtAction(nameof(Get), new { id = bird.BirdId }, summary);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] Bird updated)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            bird.Name = updated.Name;
            bird.Species = updated.Species;
            bird.Description = updated.Description;
            bird.ImageUrl = updated.ImageUrl;
            bird.Tagline = updated.Tagline;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            _db.Birds.Remove(bird);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/love")]
        public async Task<IActionResult> Love(Guid id)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized("User not found");

            // Check existing love
            var exists = await _db.Loves.FindAsync(userId, id);
            if (exists != null) return BadRequest("Already loved");

            var love = new Love { UserId = userId, BirdId = id };
            _db.Loves.Add(love);

            // Atomic increment
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = loved_count + 1 WHERE bird_id = {id}");
            await _db.SaveChangesAsync();

            // Send notification to bird owner
            if (bird.OwnerId != userId) // Don't notify if user loves their own bird
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var updatedBird = await _db.Birds.AsNoTracking().FirstOrDefaultAsync(b => b.BirdId == id);
                        var loveCount = updatedBird?.LovedCount ?? 0;

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = bird.OwnerId,
                            Type = NotificationType.BirdLoved,
                            Title = "Heart " + user.Name + " loved " + bird.Name + "!",
                            Message = $"{user.Name} loved your {bird.Species ?? "bird"}. You now have {loveCount} loves!",
                            Priority = NotificationPriority.Medium,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push,
                            DeepLink = $"/birds/{id}",
                            BirdId = id,
                            ActorUserId = userId
                        });

                        // Check for milestone
                        if (loveCount > 0 && IsMilestone(loveCount))
                        {
                            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                            {
                                UserId = bird.OwnerId,
                                Type = NotificationType.MilestoneAchieved,
                                Title = "Celebration " + bird.Name + " reached " + loveCount + " loves!",
                                Message = $"Congratulations! Your bird is loved by {loveCount} people.",
                                Priority = NotificationPriority.High,
                                Channels = NotificationChannel.InApp | NotificationChannel.Push,
                                DeepLink = $"/birds/{id}",
                                BirdId = id
                            });
                        }
                    }
                    catch
                    {
                        // Ignore notification errors
                    }
                });
            }

            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/unlove")]
        public async Task<IActionResult> Unlove(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var love = await _db.Loves.FindAsync(userId, id);
            if (love == null) return NotFound();

            // Atomic decrement but not below zero
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = GREATEST(loved_count - 1, 0) WHERE bird_id = {id}");
            _db.Loves.Remove(love);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}/love")]
        public async Task<IActionResult> UnloveDelete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var love = await _db.Loves.FindAsync(userId, id);
            if (love == null) return NotFound("Love record not found");

            // Verify bird exists
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            // Atomic decrement but not below zero
            await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET loved_count = GREATEST(loved_count - 1, 0) WHERE bird_id = {id}");
            _db.Loves.Remove(love);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id}/lovers")]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> Lovers(Guid id)
        {
            var lovers = await _db.Loves
                .Where(l => l.BirdId == id)
                .Include(l => l.User)
                .Select(l => new UserSummaryDto { UserId = l.UserId, Name = l.User != null ? l.User.Name : string.Empty })
                .ToListAsync();

            return Ok(lovers);
        }

        [Authorize]
        [HttpPost("{id}/support-usage")]
        public async Task<ActionResult<SupportUsageDto>> ReportSupportUsage(Guid id, [FromBody] SupportUsageDto dto)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var callerId)) return Unauthorized();

            // Only owner can report support usage
            if (bird.OwnerId != callerId) return Forbid();

            var usage = new SupportUsage
            {
                UsageId = Guid.NewGuid(),
                BirdId = id,
                ReportedBy = callerId,
                Amount = dto.Amount,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _db.Add(usage);
            await _db.SaveChangesAsync();

            dto.UsageId = usage.UsageId;
            dto.BirdId = usage.BirdId;
            dto.ReportedBy = usage.ReportedBy;
            dto.CreatedAt = usage.CreatedAt;

            return CreatedAtAction(nameof(ReportSupportUsage), new { id = id, usageId = usage.UsageId }, dto);
        }

        [Authorize]
        [HttpPost("{id}/premium/subscribe")]
        public async Task<IActionResult> Subscribe(Guid id)
        {
            if (!await EnsureOwner(id)) return Forbid();

            // For simplicity we create a local subscription record with status active
            var existing = await _db.BirdPremiumSubscriptions.FirstOrDefaultAsync(s => s.BirdId == id && s.Status == "active");
            if (existing != null) return BadRequest("Already subscribed");

            var userId = GetUserIdClaim().Value;

            var subscription = new BirdPremiumSubscription
            {
                BirdId = id,
                OwnerId = userId,
                Status = "active",
                Plan = "monthly",
                Provider = "local",
                ProviderSubscriptionId = Guid.NewGuid().ToString(),
                PriceCents = 300,
                DurationDays = 30,
                StartedAt = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.BirdPremiumSubscriptions.Add(subscription);

            var bird = await _db.Birds.FindAsync(id);
            if (bird != null)
            {
                bird.IsPremium = true;
                bird.PremiumPlan = subscription.Plan;
                bird.PremiumExpiresAt = subscription.CurrentPeriodEnd;
                bird.MaxMediaCount = 20; // premium allows more media
            }

            await _db.SaveChangesAsync();
            return Ok(new { subscriptionId = subscription.Id, expiry = subscription.CurrentPeriodEnd });
        }

        [Authorize]
        [HttpPost("{id}/premium/subscribe/lifetime")]
        public async Task<IActionResult> PurchaseLifetime(Guid id)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var existing = await _db.BirdPremiumSubscriptions.FirstOrDefaultAsync(s => s.BirdId == id && s.Status == "active");
            if (existing != null) return BadRequest("Already subscribed");

            var userId = GetUserIdClaim().Value;

            var subscription = new BirdPremiumSubscription
            {
                BirdId = id,
                OwnerId = userId,
                Status = "active",
                Plan = "lifetime",
                Provider = "local",
                ProviderSubscriptionId = Guid.NewGuid().ToString(),
                PriceCents = 7000, // $70 one-time
                DurationDays = int.MaxValue,
                StartedAt = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.MaxValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.BirdPremiumSubscriptions.Add(subscription);

            var bird = await _db.Birds.FindAsync(id);
            if (bird != null)
            {
                bird.IsPremium = true;
                bird.PremiumPlan = subscription.Plan;
                bird.PremiumExpiresAt = null;
                bird.MaxMediaCount = 50; // lifetime premium allows most media
            }

            await _db.SaveChangesAsync();
            return Ok(new { subscriptionId = subscription.Id, plan = subscription.Plan });
        }

        [Authorize]
        [HttpPatch("{id}/premium/style")]
        public async Task<IActionResult> UpdateStyle(Guid id, [FromBody] PremiumStyleDto dto)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            // Ensure active subscription
            var active = await _db.BirdPremiumSubscriptions.FirstOrDefaultAsync(s => s.BirdId == id && s.Status == "active");
            if (active == null) return Forbid("No active subscription");

            var json = JsonSerializer.Serialize(dto);
            bird.PremiumStyleJson = json;

            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpPatch("{id}/premium/qr")]
        public async Task<IActionResult> UpdateQr(Guid id, [FromBody] string qrUrl)
        {
            if (!await EnsureOwner(id)) return Forbid();

            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound();

            // Allow qr to be set only if premium
            if (!bird.IsPremium) return Forbid("Only premium birds can have QR codes");

            bird.QrCodeUrl = qrUrl;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/donate")]
        public async Task<IActionResult> DonateToBird(Guid id, [FromBody] long cents)
        {
            var bird = await _db.Birds.FindAsync(id);
            if (bird == null) return NotFound("Bird not found");

            // Record a simple support transaction (not handling external payments here)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var supporterId)) return Unauthorized();

            var tx = new SupportTransaction
            {
                TransactionId = Guid.NewGuid(),
                BirdId = id,
                SupporterId = supporterId,
                Amount = cents / 100m,
                Message = null,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportTransactions.Add(tx);
            bird.DonationCents += cents;
            bird.SupportedCount = await _db.SupportTransactions.CountAsync(t => t.BirdId == id);

            await _db.SaveChangesAsync();

            // Send notification to bird owner
            if (bird.OwnerId != supporterId)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var supporter = await _db.Users.FindAsync(supporterId);
                        var amountDisplay = (cents / 100m).ToString("F2");

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = bird.OwnerId,
                            Type = NotificationType.BirdSupported,
                            Title = "Corn " + (supporter?.Name ?? "Someone") + " supported " + bird.Name + "!",
                            Message = $"{supporter?.Name ?? "Someone"} contributed ${amountDisplay} to support {bird.Name}. Total: ${bird.DonationCents / 100m:F2}",
                            Priority = NotificationPriority.High,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                            DeepLink = $"/birds/{id}",
                            BirdId = id,
                            TransactionId = tx.TransactionId,
                            ActorUserId = supporterId
                        });

                        // Also notify supporter
                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = supporterId,
                            Type = NotificationType.PaymentReceived,
                            Title = "Checkmark Payment confirmed",
                            Message = $"Your ${amountDisplay} support for {bird.Name} was processed!",
                            Priority = NotificationPriority.High,
                            Channels = NotificationChannel.InApp | NotificationChannel.Push | NotificationChannel.Email,
                            DeepLink = $"/support/{tx.TransactionId}",
                            BirdId = id,
                            TransactionId = tx.TransactionId
                        });
                    }
                    catch
                    {
                        // Ignore notification errors
                    }
                });
            }

            return Ok(new { totalDonated = bird.DonationCents });
        }

        private bool IsMilestone(int count)
        {
            int[] milestones = { 10, 50, 100, 500, 1000, 5000 };
            return milestones.Contains(count);
        }
    }
}
