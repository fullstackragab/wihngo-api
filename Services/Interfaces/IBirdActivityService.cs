using System;
using System.Threading.Tasks;
using Wihngo.Models.Enums;

namespace Wihngo.Services.Interfaces
{
    /// <summary>
    /// Service for managing bird activity status and support availability
    /// </summary>
    public interface IBirdActivityService
    {
        /// <summary>
        /// Get the activity status for a bird based on last activity timestamp and memorial status
        /// </summary>
        BirdActivityStatus GetActivityStatus(DateTime? lastActivityAt, bool isMemorial);

        /// <summary>
        /// Check if a bird can receive support based on its activity status
        /// </summary>
        bool CanReceiveSupport(BirdActivityStatus status);

        /// <summary>
        /// Get human-readable last seen text (e.g., "Recently active", "Last seen: 2 months ago")
        /// </summary>
        string GetLastSeenText(DateTime? lastActivityAt, bool isMemorial);

        /// <summary>
        /// Get message explaining why support is unavailable (null if support is available)
        /// </summary>
        string? GetSupportUnavailableMessage(BirdActivityStatus status);

        /// <summary>
        /// Update bird's last activity timestamp
        /// </summary>
        Task UpdateLastActivityAsync(Guid birdId);
    }
}
