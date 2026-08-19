using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<LoginResult> LoginAsync(LoginRequest request);
        Task LogoutAsync(Guid sessionId);
        Task<LoginResult> CompleteMfaLoginAsync(Guid userId, string ipAddress, string userAgent);
    }
}
