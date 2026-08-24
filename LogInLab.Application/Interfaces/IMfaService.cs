using LogInLab.Application.DTOs;
using System.Net;

namespace LogInLab.Application.Interfaces
{
    public interface IMfaService
    {
        Task<MfaSetupResult> BeingSetupAsync(Guid userId, string userEmail);
        Task<MfaActivationResult> ConfirmSetupAsync(string ipAddress, string userAgent, Guid userId, string code);
        Task<bool> ValidateCodeOrBackupAsync(Guid userId, string code);
        Task DisableAsync(string ipAddress, string userAgent, Guid userId);
    }
}
