using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class MagicLinkTokenRepository : IMagicLinkToken
    {
        private readonly LogInLabDbContext _context;

        public MagicLinkTokenRepository(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(MagicLinkToken token)
        {
            await _context.MagicLinkTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task<MagicLinkToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.MagicLinkTokens.FirstOrDefaultAsync(t => tokenHash.Equals(t.TokenHash));
        }

        public async Task InvalidateAllForUserAsync(Guid userId)
        {
            List<MagicLinkToken> activeTokens = await _context.MagicLinkTokens
                .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (MagicLinkToken token in activeTokens)
            {
                token.UsedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsUsedAsync(Guid tokenId)
        {
            MagicLinkToken? token = await _context.MagicLinkTokens.FindAsync(tokenId);
            if (token is not null)
            {
                token.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
