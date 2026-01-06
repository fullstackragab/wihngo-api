using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for managing the "birds need support" feature with 3-round weekly system.
/// </summary>
public interface INeedsSupportService
{
    /// <summary>
    /// Get all birds needing support with current round information.
    /// Implements the 3-round weekly support system.
    /// </summary>
    Task<BirdsNeedSupportResponse> GetBirdsNeedingSupportAsync();

    /// <summary>
    /// Set whether a bird needs support (owner only).
    /// </summary>
    Task<bool> SetBirdNeedsSupportAsync(Guid birdId, Guid ownerId, bool needsSupport);

    /// <summary>
    /// Record that a bird received support (called when payment completes).
    /// </summary>
    Task RecordSupportReceivedAsync(Guid birdId);

    /// <summary>
    /// Get weekly support progress for a specific bird (for owners).
    /// </summary>
    Task<BirdWeeklySupportProgressDto?> GetBirdWeeklyProgressAsync(Guid birdId);

    /// <summary>
    /// Get overall weekly support statistics.
    /// </summary>
    Task<WeeklySupportStatsDto> GetWeeklyStatsAsync();

    /// <summary>
    /// Check if a bird can receive support in the current round.
    /// </summary>
    Task<bool> CanBirdReceiveSupportAsync(Guid birdId);
}
