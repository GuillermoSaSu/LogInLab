using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class MfaSecretRepository : IMfaSecretRepository
    {
        private readonly LogInLabDbContext _context;

        public MfaSecretRepository(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(MfaSecret mfaSecret)
        {
            await _context.MfaSecrets.AddAsync(mfaSecret);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid userId)
        {
            MfaSecret? mfaSecret = GetByUserIdAsync(userId).Result;
            if(mfaSecret is not null)
            {
                _context.MfaSecrets.Remove(mfaSecret);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<MfaSecret?> GetByUserIdAsync(Guid userId)
        {
            return await _context.MfaSecrets.FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task UpdateAsync(MfaSecret mfaSecret)
        {
            _context.MfaSecrets.Update(mfaSecret);
            await _context.SaveChangesAsync();
        }
    }
}
