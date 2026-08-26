using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface IMagicLinkService
    {
        Task<AuthResult> RequestMagicLinkAsync(string email, string ipAddress, string userAgent);
        Task<LoginResult> ConsumeMagicLinkAsync(string rawToken, string ipAddress, string userAgent);
    }
}
