using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface IEmailVerificationTokenRepository
    {
        Task AddAsync(EmailVerificationToken token);
        Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash);
        Task MarkAsUsedAsync(Guid tokenId);
        Task<EmailVerificationToken> GetLastestByUserIdAsync(Guid userId);
    }
}
