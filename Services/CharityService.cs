namespace Wihngo.Services
{
    using System;
    using System.Collections.Generic;
    using Wihngo.Data;
    using Wihngo.Dtos;
    using Wihngo.Models;
    using Wihngo.Services.Interfaces;

    public class CharityService : ICharityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CharityService> _logger;

        public CharityService(AppDbContext context, ILogger<CharityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CharityImpactDto> GetBirdCharityImpactAsync(Guid birdId)
        {
            var allocations = await _context.Set<CharityAllocation>()
                .Include(a => a.Subscription)
                .Where(a => a.Subscription != null && a.Subscription.BirdId == birdId)
                .ToListAsync();

            var totalContributed = allocations.Sum(a => a.Amount);
            var birdsHelped = allocations.Any() ? 1 : 0;
            var sheltersSupported = allocations.Count > 0 ? 3 : 0;
            var conservationProjects = allocations.Count > 0 ? 2 : 0;

            return new CharityImpactDto
            {
                TotalContributed = totalContributed,
                BirdsHelped = birdsHelped,
                SheltersSupported = sheltersSupported,
                ConservationProjects = conservationProjects
            };
        }

        public async Task<GlobalCharityImpactDto> GetGlobalCharityImpactAsync()
        {
            var stats = await _context.Set<CharityImpactStats>()
                .OrderByDescending(s => s.LastUpdated)
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                await UpdateGlobalCharityStatsAsync();
                stats = await _context.Set<CharityImpactStats>()
                    .OrderByDescending(s => s.LastUpdated)
                    .FirstOrDefaultAsync();
            }

            if (stats == null)
            {
                return new GlobalCharityImpactDto
                {
                    TotalContributed = 0,
                    TotalSubscribers = 0,
                    BirdsHelped = 0,
                    SheltersSupported = 0,
                    ConservationProjects = 0
                };
            }

            var totalSubscribers = await _context.BirdPremiumSubscriptions
                .Where(s => s.Status == "active")
                .CountAsync();

            return new GlobalCharityImpactDto
            {
                TotalContributed = stats.TotalContributed,
                TotalSubscribers = totalSubscribers,
                BirdsHelped = stats.BirdsHelped,
                SheltersSupported = stats.SheltersSupported,
                ConservationProjects = stats.ConservationProjects
            };
        }

        public async Task RecordCharityAllocationAsync(Guid subscriptionId, decimal amount, decimal percentage)
        {
            var allocation = new CharityAllocation
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                CharityName = "Local Bird Shelter Network",
                Percentage = percentage,
                Amount = amount,
                AllocatedAt = DateTime.UtcNow
            };

            _context.Set<CharityAllocation>().Add(allocation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Charity allocation recorded: {Amount} for subscription {SubscriptionId}", amount, subscriptionId);

            await UpdateGlobalCharityStatsAsync();
        }

        public async Task UpdateGlobalCharityStatsAsync()
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

            _logger.LogInformation("Global charity stats updated: Total contributed {Total}", totalContributed);
        }
    }
}
