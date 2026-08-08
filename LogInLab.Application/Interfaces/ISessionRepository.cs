using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface ISessionRepository
    {
        Task<Session?> GetByIdAsync(Guid sessionId);
        Task AddAsync(Session session);
        Task RevokeAsync(Guid id);
        Task RevokeAllForUserAsync(Guid userId);
    }
}
