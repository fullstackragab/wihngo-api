namespace Wihngo.Services.Interfaces
{
    using Wihngo.Dtos;

    public interface ICharityService
    {
        Task<CharityImpactDto> GetBirdCharityImpactAsync(Guid birdId);
        Task<GlobalCharityImpactDto> GetGlobalCharityImpactAsync();
        Task RecordCharityAllocationAsync(Guid subscriptionId, decimal amount, decimal percentage);
        Task UpdateGlobalCharityStatsAsync();
    }
}
