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
    }
}
