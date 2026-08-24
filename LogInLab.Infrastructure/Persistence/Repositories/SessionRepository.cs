using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        public readonly LogInLabDbContext _dbContext;

        public SessionRepository(LogInLabDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Session?> GetByIdAsync(Guid sessionId)
        {
            return await _dbContext.Sessions.FindAsync(sessionId);
        }

        public async Task AddAsync(Session session)
        {
            await _dbContext.Sessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RevokeAsync(Guid id)
        {
            Session? session = await GetByIdAsync(id);
            if(session != null)
            {
                session.RevokedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task RevokeAllForUserAsync(Guid userId)
        {
            var activeSessions = _dbContext.Sessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync();

            foreach (var session in activeSessions.Result)
            {
                session.RevokedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<Session>> GetActiveByUserIdAsync(Guid id)
        {
            return await _dbContext.Sessions.Where(s => s.UserId == id && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedAt).ToListAsync();
        }
    }
}
