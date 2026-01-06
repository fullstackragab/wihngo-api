using System.ComponentModel.DataAnnotations;

namespace Wihngo.Dtos;

#region Request DTOs

/// <summary>
/// Request to toggle needs_support status for a bird.
/// </summary>
public class SetNeedsSupportRequest
{
    [Required]
    public bool NeedsSupport { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Response for the "birds need support" list with round information.
/// Explains how the 3-round weekly support system works.
/// </summary>
public class BirdsNeedSupportResponse
{
    /// <summary>
    /// Current round (1, 2, or 3). Null if all rounds complete.
    /// </summary>
    public int? CurrentRound { get; set; }

    /// <summary>
    /// Total rounds per week (always 3).
    /// </summary>
    public int TotalRounds { get; set; } = 3;

    /// <summary>
    /// Whether all 3 rounds are complete for this week.
    /// </summary>
    public bool AllRoundsComplete { get; set; }

    /// <summary>
    /// Thank you message shown when all rounds are complete.
    /// </summary>
    public string? ThankYouMessage { get; set; }

    /// <summary>
    /// Explanation of how the system works for users.
    /// </summary>
    public string HowItWorks { get; set; } = string.Empty;

    /// <summary>
    /// Birds needing support in the current round.
    /// Empty when AllRoundsComplete is true.
    /// </summary>
    public List<BirdNeedsSupportDto> Birds { get; set; } = new();

    /// <summary>
    /// Total birds participating in "needs support" this week.
    /// </summary>
    public int TotalBirdsParticipating { get; set; }

    /// <summary>
    /// Birds already supported in the current round.
    /// </summary>
    public int BirdsSupportedThisRound { get; set; }

    /// <summary>
    /// Birds remaining to be supported in the current round.
    /// </summary>
    public int BirdsRemainingThisRound { get; set; }

    /// <summary>
    /// When this week started (Sunday).
    /// </summary>
    public DateTime WeekStartDate { get; set; }

    /// <summary>
    /// When this week ends.
    /// </summary>
    public DateTime WeekEndDate { get; set; }
}

/// <summary>
/// A bird that needs support, shown in the list.
/// </summary>
public class BirdNeedsSupportDto
{
    public Guid BirdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public string? Tagline { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Location { get; set; }

    /// <summary>
    /// The owner of this bird.
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }

    /// <summary>
    /// How many times this bird has been supported this week (0-3).
    /// </summary>
    public int TimesSupportedThisWeek { get; set; }

    /// <summary>
    /// When this bird was last supported.
    /// </summary>
    public DateTime? LastSupportedAt { get; set; }

    /// <summary>
    /// Total support received all-time.
    /// </summary>
    public int TotalSupportCount { get; set; }
}

/// <summary>
/// Summary of weekly support progress for a specific bird (for owners).
/// </summary>
public class BirdWeeklySupportProgressDto
{
    public Guid BirdId { get; set; }
    public string BirdName { get; set; } = string.Empty;

    /// <summary>
    /// How many times supported this week (0-3).
    /// </summary>
    public int TimesSupportedThisWeek { get; set; }

    /// <summary>
    /// Max times a bird can be supported per week.
    /// </summary>
    public int MaxTimesPerWeek { get; set; } = 3;

    /// <summary>
    /// Whether the bird has received all 3 supports this week.
    /// </summary>
    public bool FullySupportedThisWeek { get; set; }

    /// <summary>
    /// When last supported.
    /// </summary>
    public DateTime? LastSupportedAt { get; set; }

    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
}

/// <summary>
/// Overall weekly support statistics.
/// </summary>
public class WeeklySupportStatsDto
{
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }

    /// <summary>
    /// Current round (1, 2, or 3). 4 means all complete.
    /// </summary>
    public int CurrentRound { get; set; }

    /// <summary>
    /// Total birds marked as needing support.
    /// </summary>
    public int TotalBirdsNeedingSupport { get; set; }

    /// <summary>
    /// Birds fully supported (all 3 rounds).
    /// </summary>
    public int BirdsFullySupported { get; set; }

    /// <summary>
    /// Total support transactions this week.
    /// </summary>
    public int TotalSupportsThisWeek { get; set; }

    /// <summary>
    /// Whether all birds have completed all 3 rounds.
    /// </summary>
    public bool AllRoundsComplete { get; set; }
}

#endregion
