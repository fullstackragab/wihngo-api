namespace Wihngo.BackgroundJobs
{
    using System;
    using System.Threading.Tasks;
    using Coravel.Invocable;
    using Hangfire;
    using Wihngo.Data;
    using Wihngo.Models;

    public class CharityAllocationJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CharityAllocationJob> _logger;

        public CharityAllocationJob(AppDbContext context, ILogger<CharityAllocationJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Process charity allocations for active subscriptions
        /// Runs monthly
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessMonthlyAllocationsAsync()
        {
            _logger.LogInformation("Starting monthly charity allocation processing");

            var activeSubscriptions = await _context.BirdPremiumSubscriptions
                .Where(s => s.Status == "active" && s.Plan != "lifetime")
                .ToListAsync();

            var allocationsProcessed = 0;
            var errors = 0;

            foreach (var subscription in activeSubscriptions)
            {
                try
                {
                    // Check if allocation already exists for current period
                    var currentPeriodStart = DateTime.UtcNow.Date.AddDays(-30);
                    var existingAllocation = await _context.Set<CharityAllocation>()
                        .FirstOrDefaultAsync(a => 
                            a.SubscriptionId == subscription.Id && 
                            a.AllocatedAt >= currentPeriodStart);

                    if (existingAllocation != null)
                    {
                        _logger.LogInformation("Allocation already exists for subscription {SubscriptionId}", subscription.Id);
                        continue;
                    }

                    var price = GetPlanPrice(subscription.Plan);
                    var percentage = GetCharityPercentage(subscription.Plan);
                    var amount = price * (percentage / 100m);

                    var allocation = new CharityAllocation
                    {
                        Id = Guid.NewGuid(),
                        SubscriptionId = subscription.Id,
                        CharityName = "Local Bird Shelter Network",
                        Percentage = percentage,
                        Amount = amount,
                        AllocatedAt = DateTime.UtcNow
                    };

                    _context.Set<CharityAllocation>().Add(allocation);
                    allocationsProcessed++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error allocating charity for subscription {SubscriptionId}", subscription.Id);
                }
            }

            if (allocationsProcessed > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Processed {Count} charity allocations successfully", allocationsProcessed);
            }

            if (errors > 0)
            {
                _logger.LogWarning("Encountered {ErrorCount} errors during charity allocation processing", errors);
            }

            // Update global stats after processing allocations
            await UpdateGlobalStatsAsync();
        }

        private async Task UpdateGlobalStatsAsync()
        {
            try
            {
                var totalAllocations = await _context.Set<CharityAllocation>().ToListAsync();
                var totalContributed = totalAllocations.Sum(a => a.Amount);
                var activeBirds = await _context.BirdPremiumSubscriptions
                    .Where(s => s.Status == "active")
                    .CountAsync();

                var stats = await _context.Set<CharityImpactStats>()
                    .OrderByDescending(s => s.LastUpdated)
                    .FirstOrDefaultAsync();

                if (stats == null)
                {
                    stats = new CharityImpactStats
                    {
                        Id = Guid.NewGuid()
                    };
                    _context.Set<CharityImpactStats>().Add(stats);
                }

                stats.TotalContributed = totalContributed;
                stats.BirdsHelped = activeBirds;
                stats.SheltersSupported = Math.Min(3, (int)(totalContributed / 100));
                stats.ConservationProjects = Math.Min(5, (int)(totalContributed / 200));
                stats.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Global charity stats updated: Total contributed ${Total}", totalContributed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating global charity stats");
            }
        }

        private decimal GetPlanPrice(string plan) => plan switch
        {
            "monthly" => 3.99m,
            "yearly" => 39.99m,
            _ => 0
        };

        private decimal GetCharityPercentage(string plan) => plan switch
        {
            "monthly" => 10,
            "yearly" => 15,
            "lifetime" => 20,
            _ => 0
        };
    }
}
