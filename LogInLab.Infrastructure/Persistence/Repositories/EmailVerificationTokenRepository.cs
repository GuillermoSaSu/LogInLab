using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
    {
        private readonly LogInLabDbContext _context;

        public EmailVerificationTokenRepository(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EmailVerificationToken token)
        {
            await _context.EmailVerificationTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash.Equals(tokenHash));
        }

        public async Task MarkAsUsedAsync(Guid tokenId)
        {
            EmailVerificationToken? token = _context.EmailVerificationTokens.FirstOrDefault(t => t.Id == tokenId);
            if (token is not null)
            {
                token.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<EmailVerificationToken> GetLastestByUserIdAsync(Guid userId)
        {
            return await _context.EmailVerificationTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync() ?? throw new InvalidOperationException("No email verification token found for the specified user.");
        }

    }
}
