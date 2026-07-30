using LogInLab.Domain.Entities;

namespace LogInLab.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User ?> GetByIdAsync(Guid id);
        Task<User ?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
