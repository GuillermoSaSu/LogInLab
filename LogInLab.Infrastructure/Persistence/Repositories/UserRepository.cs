using LogInLab.Application.Exceptions;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LogInLabDbContext _context;

        public UserRepository(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email.ToLowerInvariant()));
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);

            try
            {
                await _context.SaveChangesAsync();
            } catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                throw new DuplicateEmailException();
            }
            }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            // Código SQLSTATE 23505 = unique_violation in PostgreSQL
            return ex.InnerException is PostgresException { SqlState: "23505" };
        }
    }
}
