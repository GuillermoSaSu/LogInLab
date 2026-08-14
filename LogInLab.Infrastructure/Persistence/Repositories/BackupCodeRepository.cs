using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class BackupCodeRepository : IBackupCodeRepository
    {
        private readonly LogInLabDbContext _context;

        public BackupCodeRepository(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<BackupCode> codes)
        {
            await _context.BackupCodes.AddRangeAsync(codes);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllForUserAsync(Guid userId)
        {
            List<BackupCode> codesToDelete = await _context.BackupCodes
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (codesToDelete.Count != 0)
            {
                _context.BackupCodes.RemoveRange(codesToDelete);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<BackupCode>> GetUnusedCodesByUserIdAsync(Guid userId)
        {
            return await _context.BackupCodes
                .Where(c => c.UserId == userId && c.UsedAt == null)
                .ToListAsync();
        }

        public async Task MaskAsUsedAsync(Guid codeId)
        {
            BackupCode? code = await _context.BackupCodes.FindAsync(codeId);
            if(code is not null)
            {
                code.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
