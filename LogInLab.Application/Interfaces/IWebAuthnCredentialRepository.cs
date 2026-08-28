using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface IWebAuthnCredentialRepository
    {
        Task AddAsync(WebAuthnCredential credential);
        Task<List<WebAuthnCredential>> GetByUserIdAsync(Guid userId);
        Task<WebAuthnCredential?> GetByCredentialAsync(byte[] credentialId);
        Task UpdateAsync(WebAuthnCredential credential);
        Task DeleteAsync(Guid id, Guid userId);
    }
}
