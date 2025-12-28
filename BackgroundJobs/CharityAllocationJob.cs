namespace Wihngo.BackgroundJobs
{
    using System;
    using System.Threading.Tasks;
    using Hangfire;
    using Wihngo.Data;
    using Wihngo.Models;

    /// <summary>
    /// Background job to update global charity impact statistics
    /// "All birds are equal" - charity allocations are now based on platform donations, not premium
    /// </summary>
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
        /// Update global charity impact statistics
        /// Runs periodically to keep stats fresh
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task UpdateGlobalCharityStatsAsync()
        {
            _logger.LogInformation("Updating global charity statistics");

            try
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

                _logger.LogInformation("Global charity stats updated: Total contributed ${Total}, Birds helped {Birds}",
                    totalContributed, totalBirds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating global charity stats");
                throw;
            }
        }
    }
}
