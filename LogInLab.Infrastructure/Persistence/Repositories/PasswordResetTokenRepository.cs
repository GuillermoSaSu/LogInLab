using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly LogInLabDbContext _context;

        public PasswordResetTokenRepository(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PasswordResetToken token)
        {
            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash.Equals(tokenHash));
        }

        public async Task MarkAsUsedAsync(Guid tokenId)
        {
            PasswordResetToken? token = await _context.PasswordResetTokens.FindAsync(tokenId);
            if(token is not null)
            {
                token.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task InvalidateAllForUserAsync(Guid userId)
        {
            var activeTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == userId && t.UsedAt == null && t.ExipresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
