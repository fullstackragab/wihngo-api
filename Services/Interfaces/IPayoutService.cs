using System;
using System.Threading.Tasks;
using Wihngo.Dtos;
using Wihngo.Models.Payout;

namespace Wihngo.Services.Interfaces
{
    public interface IPayoutService
    {
        // Balance operations
        Task<PayoutBalanceDto?> GetBalanceAsync(Guid userId);

        // Payout method operations
        Task<List<PayoutMethodDto>> GetPayoutMethodsAsync(Guid userId);
        Task<PayoutMethodDto?> GetPayoutMethodByIdAsync(Guid methodId, Guid userId);
        Task<PayoutMethodDto> AddPayoutMethodAsync(Guid userId, PayoutMethodCreateDto dto);
        Task<PayoutMethodDto?> UpdatePayoutMethodAsync(Guid methodId, Guid userId, PayoutMethodUpdateDto dto);
        Task<bool> DeletePayoutMethodAsync(Guid methodId, Guid userId);

        // Payout history
        Task<PayoutHistoryResponseDto> GetPayoutHistoryAsync(
            Guid userId, 
            int page = 1, 
            int pageSize = 20, 
            string? status = null);

        // Process payouts (admin/background job)
        Task<ProcessPayoutResponseDto> ProcessPayoutsAsync(Guid? userId = null);
    }
}
