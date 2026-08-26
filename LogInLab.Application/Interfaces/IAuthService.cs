using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<LoginResult> LoginAsync(LoginRequest request);
        Task LogoutAsync(Guid sessionId, string ipAddress, string userAgent, Guid? userId);
        Task<LoginResult> CompleteMfaLoginAsync(Guid userId, string ipAddress, string userAgent);
        Task<LoginResult> CompleteMagicLinkLoginAsync(Guid userId, string ipAddress, string userAgent);
    }
}
