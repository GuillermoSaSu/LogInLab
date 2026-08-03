using LogInLab.Application.DTOs;
using LogInLab.Application.Exceptions;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;

namespace LogInLab.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISessionRepository _sessionRepository;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ISessionRepository sessionRepository)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _sessionRepository = sessionRepository;
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
            }
            catch (DuplicateEmailException)
            {
                return AuthResult.FailureResult("An error occurred while registering the user.");
            }
            return AuthResult.SuccessResult();
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            string normalizedEmail = request.Email.Trim().ToLowerInvariant();
            User? user = await _userRepository.GetByEmailAsync(normalizedEmail);

            const string genericError = "Email or password is incorrect.";

            if (user is null)
            {
                return LoginResult.FailureResult(genericError);
            }

            bool isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return LoginResult.FailureResult(genericError);
            }

            Session session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };

            await _sessionRepository.AddAsync(session);
            return LoginResult.SuccessResult(user.Id, session.Id);
        }

        public async Task LogoutAsync(Guid sessionId)
        {
            await _sessionRepository.RevokeAsync(sessionId);
        }
    }
}
