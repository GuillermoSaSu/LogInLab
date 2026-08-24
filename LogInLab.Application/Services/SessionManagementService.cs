using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Application.Services
{
    public class SessionManagementService : ISessionManagementSerivce
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IAuthEventLogger _authEventLogger;

        public SessionManagementService(ISessionRepository sessionRepository, IAuthEventLogger authEventLogger)
        {
            _sessionRepository = sessionRepository;
            _authEventLogger = authEventLogger;
        }

        public async Task<List<SessionInfo>> GetActiveSessionAsync(Guid userId)
        {
            List<Session> sessions = await _sessionRepository.GetActiveByUserIdAsync(userId);

            return sessions.Select(s => new SessionInfo(
                    s.Id,
                    s.IpAddress,
                    s.UserAgent,
                    s.CreatedAt,
                    s.ExpiresAt,
                    IsCurrentSession : false
                )).ToList();
        }

        public async Task RevokeAllOtherSessionsAsync(Guid userId, Guid currentSessionId)
        {
            List<Session> sessions = await _sessionRepository.GetActiveByUserIdAsync(userId);

            foreach (var session in sessions.Where(s => s.Id != currentSessionId)) 
            { 
                await _sessionRepository.RevokeAsync(session.Id);
            }
        }

        public async Task<AuthResult> RevokeSessionAsync(Guid userId, Guid sessionId, Guid currentSession)
        {
            Session? session = await _sessionRepository.GetByIdAsync(sessionId);

            if(session is null || session.UserId != userId)
            {
                return AuthResult.FailureResult("The current session could not be closed.");
            }

            await _sessionRepository.RevokeAsync(sessionId);

            return AuthResult.SuccessResult();
        }
    }
}
