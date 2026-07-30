using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;

namespace LogInLab.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            string normalizedEmail = request.Email.Trim().ToLowerInvariant();

            User? existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (existingUser is not null)
            {
                return AuthResult.FailureResult("Registration could not be completed with the provided data.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.Hash(request.Password),
                EmailVerified = false,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _userRepository.AddAsync(user);
                return AuthResult.SuccessResult();
            }
            catch (Exception)
            {
                return AuthResult.FailureResult("An error occurred while registering the user.");
            }
        }
    }
}
