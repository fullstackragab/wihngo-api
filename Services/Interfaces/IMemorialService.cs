using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wihngo.Dtos;

namespace Wihngo.Services.Interfaces
{
    public interface IMemorialService
    {
        /// <summary>
        /// Mark a bird as memorial (deceased) and initiate fund redirection
        /// </summary>
        Task<MarkMemorialResponseDto> MarkBirdAsMemorialAsync(Guid birdId, Guid ownerId, MemorialRequestDto request);

        /// <summary>
        /// Get memorial details for a bird
        /// </summary>
        Task<MemorialBirdDto?> GetMemorialDetailsAsync(Guid birdId);

        /// <summary>
        /// Add a memorial message (condolence/tribute)
        /// </summary>
        Task<MemorialMessageDto> AddMemorialMessageAsync(Guid birdId, Guid userId, CreateMemorialMessageDto message);

        /// <summary>
        /// Get paginated memorial messages for a bird
        /// </summary>
        Task<MemorialMessagePageDto> GetMemorialMessagesAsync(Guid birdId, int page = 1, int pageSize = 20, string sortBy = "recent");

        /// <summary>
        /// Delete a memorial message (by author or bird owner)
        /// </summary>
        Task<bool> DeleteMemorialMessageAsync(Guid messageId, Guid requesterId);

        /// <summary>
        /// Check if a bird is memorial
        /// </summary>
        Task<bool> IsMemorialBirdAsync(Guid birdId);

        /// <summary>
        /// Process fund redirection for a memorial bird (admin operation)
        /// </summary>
        Task<MemorialFundRedirectionDto> ProcessFundRedirectionAsync(Guid birdId);

        /// <summary>
        /// Check rate limit for memorial messages (3 per user per bird per day)
        /// </summary>
        Task<bool> CheckMessageRateLimitAsync(Guid userId, Guid birdId);
    }
}
