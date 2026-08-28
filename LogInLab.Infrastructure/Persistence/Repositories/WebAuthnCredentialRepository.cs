using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.CompilerServices;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class WebAuthnCredentialRepository : IWebAuthnCredentialRepository
    {
        private readonly LogInLabDbContext _context;

        public WebAuthnCredentialRepository(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(WebAuthnCredential credential)
        {
            await _context.WebAuthnCredentials.AddAsync(credential);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid userId)
        {
            WebAuthnCredential? credential = await _context.WebAuthnCredentials
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if(credential is not null)
            {
                _context.WebAuthnCredentials.Remove(credential);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<WebAuthnCredential?> GetByCredentialAsync(byte[] credentialId)
        {
            //A byte-by - byte comparison of arrays requires loading the candidates into memory.
            //In a real case it will require a index calculated column. But for this lab it is enough.
            List<WebAuthnCredential> all = await _context.WebAuthnCredentials.ToListAsync();
            return all.FirstOrDefault(c => c.CredentialId.SequenceEqual(credentialId));
        }

        public async Task<List<WebAuthnCredential>> GetByUserIdAsync(Guid userId)
        {
            return await _context.WebAuthnCredentials
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(WebAuthnCredential credential)
        {
            _context.WebAuthnCredentials.Update(credential);
            await _context.SaveChangesAsync();
        }
    }
}
