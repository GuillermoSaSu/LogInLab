using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface IEmailVerificationService
    {
        Task SendVerificationEmailAsync(Guid userId, string userEmail);
        Task<bool> VerifyAsync(string rawToken);
        Task<AuthResult> ResendVerificationEmailAsync(string email);
    }
}
