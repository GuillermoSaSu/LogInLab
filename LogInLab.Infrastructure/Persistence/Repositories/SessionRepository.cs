using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
