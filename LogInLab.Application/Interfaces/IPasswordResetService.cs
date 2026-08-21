using LogInLab.Application.DTOs;
using LogInLab.Domain.Entities;
using System.Net;

namespace LogInLab.Application.Interfaces
{
    public interface IPasswordResetService
    {
        Task<AuthResult> RequestPasswordResetAsync(string ipAddress, string userAgent, Guid userId, string email);
        Task<AuthResult> ResetPasswordAsync(string rawToken, string newPassword);
    }
}
