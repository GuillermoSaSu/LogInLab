using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface ISessionManagementSerivce
    {
        Task<List<SessionInfo>> GetActiveSessionAsync(Guid userId);
        Task<AuthResult> RevokeSessionAsync(Guid userId, Guid sessionId, Guid currentSession);
        Task RevokeAllOtherSessionsAsync(Guid userId, Guid currentSessionId);
    }
}
