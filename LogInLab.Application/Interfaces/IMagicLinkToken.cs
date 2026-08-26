using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface IMagicLinkToken
    {
        Task AddAsync(MagicLinkToken token);
        Task<MagicLinkToken?> GetByTokenHashAsync(string tokenHash);
        Task MarkAsUsedAsync(Guid tokenId);
        Task InvalidateAllForUserAsync(Guid userId);
    }
}
