using Dapper;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for managing the "birds need support" feature.
///
/// How it works:
/// - Bird owners can mark their bird as "needs support"
/// - Users see a list of birds needing support
/// - When a bird receives support, it's removed from the list for that round
/// - When all birds have been supported once, they all appear again (Round 2)
/// - After all birds have been supported 2 times, show thank you message
/// - The cycle resets every week (Sunday)
/// </summary>
public class NeedsSupportService : INeedsSupportService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IS3Service _s3Service;
    private readonly ILogger<NeedsSupportService> _logger;

    private const int MaxRoundsPerWeek = 1;

    private const string HowItWorksMessage = @"Wihngo's Weekly Support System:

Every week, you can help support birds in need.

All birds marked as 'needs support' are shown. Each bird can receive support once per week.

When all birds have been supported, you'll see a thank you message. The cycle resets every Sunday!

Your support goes directly to bird owners to help care for their feathered friends.";

    private const string ThankYouMessageTemplate = @"Thank you for your amazing support!

All {0} birds have received their weekly support!

The community has come together to help every bird in need this week. Your generosity makes a real difference in the lives of these birds and their caretakers.

The support cycle will reset on Sunday, and you'll be able to help again!";

    public NeedsSupportService(
        IDbConnectionFactory dbFactory,
        IS3Service s3Service,
        ILogger<NeedsSupportService> logger)
    {
        _dbFactory = dbFactory;
        _s3Service = s3Service;
        _logger = logger;
    }

    public async Task<BirdsNeedSupportResponse> GetBirdsNeedingSupportAsync()
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var weekStart = GetWeekStart(DateTime.UtcNow);
        var weekEnd = weekStart.AddDays(7);

        _logger.LogInformation("NeedsSupport: Using weekStart={WeekStart}, UTC now={UtcNow}",
            weekStart.Date.ToString("yyyy-MM-dd"), DateTime.UtcNow);

        // Get all birds marked as needing support (and public + support enabled)
        var birdsData = await conn.QueryAsync<dynamic>(@"
            SELECT b.bird_id, b.name, b.species, b.tagline, b.image_url, b.location,
                   b.supported_count, u.name as owner_name, u.user_id as owner_id,
                   COALESCE(r.times_supported, 0) as times_supported,
                   r.last_supported_at
            FROM birds b
            JOIN users u ON b.owner_id = u.user_id
            LEFT JOIN weekly_bird_support_rounds r
                ON b.bird_id = r.bird_id AND r.week_start_date = @WeekStart::date
            WHERE b.needs_support = true
              AND b.is_public = true
              AND b.support_enabled = true
              AND b.is_memorial = false
            ORDER BY COALESCE(r.times_supported, 0) ASC, b.created_at ASC",
            new { WeekStart = weekStart.ToString("yyyy-MM-dd") });

        var allBirds = birdsData.ToList();
        var totalBirds = allBirds.Count;

        foreach (var bird in allBirds)
        {
            _logger.LogInformation("NeedsSupport: Bird {Name} has times_supported={TimesSupported}",
                (string)bird.name, (int)bird.times_supported);
        }

        if (totalBirds == 0)
        {
            return new BirdsNeedSupportResponse
            {
                CurrentRound = null,
                AllRoundsComplete = false,
                HowItWorks = HowItWorksMessage,
                Birds = new List<BirdNeedsSupportDto>(),
                TotalBirdsParticipating = 0,
                BirdsSupportedThisRound = 0,
                BirdsRemainingThisRound = 0,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd
            };
        }

        // Calculate current round based on minimum times_supported across all birds
        int minSupported = allBirds.Min(b => (int)b.times_supported);
        int currentRound = minSupported + 1;

        // Check if all rounds complete
        bool allComplete = minSupported >= MaxRoundsPerWeek;

        if (allComplete)
        {
            return new BirdsNeedSupportResponse
            {
                CurrentRound = null,
                AllRoundsComplete = true,
                ThankYouMessage = string.Format(ThankYouMessageTemplate, totalBirds),
                HowItWorks = HowItWorksMessage,
                Birds = new List<BirdNeedsSupportDto>(),
                TotalBirdsParticipating = totalBirds,
                BirdsSupportedThisRound = totalBirds,
                BirdsRemainingThisRound = 0,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd
            };
        }

        // Filter birds that haven't been supported enough for current round
        // Birds with times_supported < currentRound are shown
        var filteredBirds = allBirds.Where(b => (int)b.times_supported < currentRound).ToList();

        var birdsToShow = new List<BirdNeedsSupportDto>();
        foreach (var b in filteredBirds)
        {
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace((string?)b.image_url))
            {
                try
                {
                    imageUrl = await _s3Service.GenerateDownloadUrlAsync(b.image_url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate image URL for bird {BirdId}", (Guid)b.bird_id);
                }
            }

            birdsToShow.Add(new BirdNeedsSupportDto
            {
                BirdId = b.bird_id,
                Name = b.name,
                Species = b.species,
                Tagline = b.tagline,
                ImageUrl = imageUrl,
                Location = b.location,
                OwnerName = b.owner_name,
                OwnerId = b.owner_id,
                TimesSupportedThisWeek = b.times_supported,
                LastSupportedAt = b.last_supported_at,
                TotalSupportCount = b.supported_count
            });
        }

        int supportedThisRound = totalBirds - birdsToShow.Count;

        return new BirdsNeedSupportResponse
        {
            CurrentRound = currentRound,
            AllRoundsComplete = false,
            HowItWorks = HowItWorksMessage,
            Birds = birdsToShow,
            TotalBirdsParticipating = totalBirds,
            BirdsSupportedThisRound = supportedThisRound,
            BirdsRemainingThisRound = birdsToShow.Count,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd
        };
    }

    public async Task<bool> SetBirdNeedsSupportAsync(Guid birdId, Guid ownerId, bool needsSupport)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var updated = await conn.ExecuteAsync(@"
            UPDATE birds
            SET needs_support = @NeedsSupport
            WHERE bird_id = @BirdId AND owner_id = @OwnerId",
            new { BirdId = birdId, OwnerId = ownerId, NeedsSupport = needsSupport });

        if (updated > 0)
        {
            _logger.LogInformation(
                "Bird {BirdId} needs_support set to {NeedsSupport} by owner {OwnerId}",
                birdId, needsSupport, ownerId);
            return true;
        }

        return false;
    }

    public async Task RecordSupportReceivedAsync(Guid birdId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var weekStart = GetWeekStart(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // Check if bird is marked as needs_support
        var needsSupport = await conn.QueryFirstOrDefaultAsync<bool>(
            "SELECT needs_support FROM birds WHERE bird_id = @BirdId",
            new { BirdId = birdId });

        if (!needsSupport)
        {
            // Bird not in needs_support program, nothing to track
            return;
        }

        // Upsert the weekly support record
        await conn.ExecuteAsync(@"
            INSERT INTO weekly_bird_support_rounds (id, bird_id, week_start_date, times_supported, last_supported_at, created_at, updated_at)
            VALUES (gen_random_uuid(), @BirdId, @WeekStart, 1, @Now, @Now, @Now)
            ON CONFLICT (bird_id, week_start_date)
            DO UPDATE SET
                times_supported = LEAST(weekly_bird_support_rounds.times_supported + 1, @MaxRounds),
                last_supported_at = @Now,
                updated_at = @Now",
            new { BirdId = birdId, WeekStart = weekStart.Date, Now = now, MaxRounds = MaxRoundsPerWeek });

        _logger.LogInformation("Recorded support for bird {BirdId} in weekly tracking", birdId);
    }

    public async Task<BirdWeeklySupportProgressDto?> GetBirdWeeklyProgressAsync(Guid birdId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var weekStart = GetWeekStart(DateTime.UtcNow);
        var weekEnd = weekStart.AddDays(7);

        var data = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT b.bird_id, b.name,
                   COALESCE(r.times_supported, 0) as times_supported,
                   r.last_supported_at
            FROM birds b
            LEFT JOIN weekly_bird_support_rounds r
                ON b.bird_id = r.bird_id AND r.week_start_date = @WeekStart
            WHERE b.bird_id = @BirdId",
            new { BirdId = birdId, WeekStart = weekStart.Date });

        if (data == null) return null;

        int timesSupported = (int)data.times_supported;

        return new BirdWeeklySupportProgressDto
        {
            BirdId = data.bird_id,
            BirdName = data.name,
            TimesSupportedThisWeek = timesSupported,
            MaxTimesPerWeek = MaxRoundsPerWeek,
            FullySupportedThisWeek = timesSupported >= MaxRoundsPerWeek,
            LastSupportedAt = data.last_supported_at,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd
        };
    }

    public async Task<WeeklySupportStatsDto> GetWeeklyStatsAsync()
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var weekStart = GetWeekStart(DateTime.UtcNow);
        var weekEnd = weekStart.AddDays(7);

        var stats = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            WITH bird_stats AS (
                SELECT
                    b.bird_id,
                    COALESCE(r.times_supported, 0) as times_supported
                FROM birds b
                LEFT JOIN weekly_bird_support_rounds r
                    ON b.bird_id = r.bird_id AND r.week_start_date = @WeekStart
                WHERE b.needs_support = true
                  AND b.is_public = true
                  AND b.support_enabled = true
            )
            SELECT
                COUNT(*) as total_birds,
                COUNT(*) FILTER (WHERE times_supported >= @MaxRounds) as fully_supported,
                COALESCE(MIN(times_supported), 0) as min_supported,
                COALESCE(SUM(times_supported), 0) as total_supports
            FROM bird_stats",
            new { WeekStart = weekStart.Date, MaxRounds = MaxRoundsPerWeek });

        int totalBirds = (int)(stats?.total_birds ?? 0);
        int minSupported = (int)(stats?.min_supported ?? 0);
        int currentRound = minSupported + 1;
        bool allComplete = totalBirds > 0 && minSupported >= MaxRoundsPerWeek;

        return new WeeklySupportStatsDto
        {
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            CurrentRound = allComplete ? MaxRoundsPerWeek + 1 : currentRound,
            TotalBirdsNeedingSupport = totalBirds,
            BirdsFullySupported = (int)(stats?.fully_supported ?? 0),
            TotalSupportsThisWeek = (int)(stats?.total_supports ?? 0),
            AllRoundsComplete = allComplete
        };
    }

    public async Task<bool> CanBirdReceiveSupportAsync(Guid birdId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var weekStart = GetWeekStart(DateTime.UtcNow);

        // Check if bird is in needs_support program and hasn't hit max rounds
        var data = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT b.needs_support,
                   COALESCE(r.times_supported, 0) as times_supported
            FROM birds b
            LEFT JOIN weekly_bird_support_rounds r
                ON b.bird_id = r.bird_id AND r.week_start_date = @WeekStart
            WHERE b.bird_id = @BirdId",
            new { BirdId = birdId, WeekStart = weekStart.Date });

        if (data == null) return true; // Bird not found, let other validation handle it

        // If not in needs_support program, always allow support
        if (!(bool)data.needs_support) return true;

        // If in program, check if under max rounds
        return (int)data.times_supported < MaxRoundsPerWeek;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        // Week starts on Sunday
        var diff = date.DayOfWeek - DayOfWeek.Sunday;
        return date.Date.AddDays(-diff);
    }
}
