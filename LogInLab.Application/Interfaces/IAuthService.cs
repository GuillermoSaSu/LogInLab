using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
    }
}
