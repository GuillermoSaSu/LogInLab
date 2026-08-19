using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface IPasswordResetService
    {
        Task<AuthResult> RequestPasswordResetAsync(string email);
        Task<AuthResult> ResetPasswordAsync(string rawToken, string newPassword);
    }
}
