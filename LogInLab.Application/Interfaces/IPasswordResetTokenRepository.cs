using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task AddAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
        Task MarkAsUsedAsync(Guid tokenId);
        Task InvalidateAllForUserAsync(Guid userId);
    }
}
