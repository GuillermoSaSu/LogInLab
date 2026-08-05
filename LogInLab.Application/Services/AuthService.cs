using FluentValidation;
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
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IEmailVerificationService _emailVerificationService;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ISessionRepository sessionRepository, IValidator<RegisterRequest> registerValidator, IEmailVerificationService emailVerificationService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _sessionRepository = sessionRepository;
            _registerValidator = registerValidator;
            _emailVerificationService = emailVerificationService;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            FluentValidation.Results.ValidationResult validationResult = await _registerValidator.ValidateAsync(request);
            if(!validationResult.IsValid)
            {
                string firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid registration data.";
                return AuthResult.FailureResult(firstError);
            }

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

            await _emailVerificationService.SendVerificationEmailAsync(user.Id, user.Email);

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

            if(!user.EmailVerified)
            {
                return LoginResult.FailureResult("You should verify your email before login. Check your inbox.");
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
