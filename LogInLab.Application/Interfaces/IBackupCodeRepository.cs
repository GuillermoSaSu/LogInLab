using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface IBackupCodeRepository
    {
        Task AddRangeAsync(IEnumerable<BackupCode> codes);
        Task<List<BackupCode>> GetUnusedCodesByUserIdAsync(Guid userId);
        Task MaskAsUsedAsync(Guid codeId);
        Task DeleteAllForUserAsync(Guid userId);
    }
}
