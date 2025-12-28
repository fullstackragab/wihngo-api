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
            // With "All birds are equal", charity impact is tracked globally, not per bird
            // Return platform-wide charity impact
            var stats = await _context.Set<CharityImpactStats>()
                .OrderByDescending(s => s.LastUpdated)
                .FirstOrDefaultAsync();

            return new CharityImpactDto
            {
                TotalContributed = stats?.TotalContributed ?? 0,
                BirdsHelped = stats?.BirdsHelped ?? 0,
                SheltersSupported = stats?.SheltersSupported ?? 0,
                ConservationProjects = stats?.ConservationProjects ?? 0
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

            // Count all birds as supporters now (all birds are equal)
            var totalBirds = await _context.Birds.CountAsync();

            return new GlobalCharityImpactDto
            {
                TotalContributed = stats.TotalContributed,
                TotalSubscribers = totalBirds,
                BirdsHelped = stats.BirdsHelped,
                SheltersSupported = stats.SheltersSupported,
                ConservationProjects = stats.ConservationProjects
            };
        }

        public async Task RecordCharityAllocationAsync(Guid sourceId, decimal amount, decimal percentage)
        {
            var allocation = new CharityAllocation
            {
                Id = Guid.NewGuid(),
                SourceId = sourceId,
                CharityName = "Local Bird Shelter Network",
                Percentage = percentage,
                Amount = amount,
                AllocatedAt = DateTime.UtcNow
            };

            _context.Set<CharityAllocation>().Add(allocation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Charity allocation recorded: {Amount} from source {SourceId}", amount, sourceId);

            await UpdateGlobalCharityStatsAsync();
        }

        public async Task UpdateGlobalCharityStatsAsync()
        {
            var totalAllocations = await _context.Set<CharityAllocation>().ToListAsync();
            var totalContributed = totalAllocations.Sum(a => a.Amount);

            // Count all birds (all birds are equal)
            var totalBirds = await _context.Birds.CountAsync();

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
            stats.BirdsHelped = totalBirds;
            stats.SheltersSupported = Math.Min(3, (int)(totalContributed / 100));
            stats.ConservationProjects = Math.Min(5, (int)(totalContributed / 200));
            stats.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Global charity stats updated: Total contributed {Total}", totalContributed);
        }
    }
}
