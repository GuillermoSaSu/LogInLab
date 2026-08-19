using LogInLab.Application.DTOs;

namespace LogInLab.Application.Interfaces
{
    public interface IMfaService
    {
        Task<MfaSetupResult> BeingSetupAsync(Guid userId, string userEmail);
        Task<MfaActivationResult> ConfirmSetupAsync(Guid userId, string code);
        Task<bool> ValidateCodeOrBackupAsync(Guid userId, string code);
        Task DisableAsync(Guid userId);
    }
}
