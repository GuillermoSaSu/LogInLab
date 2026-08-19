using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface IMfaSecretRepository
    {
        Task<MfaSecret?> GetByUserIdAsync(Guid userId);
        Task AddAsync(MfaSecret mfaSecret);
        Task UpdateAsync(MfaSecret mfaSecret);
        Task DeleteAsync(Guid userId);
    }
}
