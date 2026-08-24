using FluentValidation;
using LogInLab.Application.DTOs;
using LogInLab.Application.Exceptions;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using LogInLab.Domain.Enums;

namespace LogInLab.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISessionRepository _sessionRepository;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IAuthEventLogger _authEventLogger;

        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ISessionRepository sessionRepository, IValidator<RegisterRequest> registerValidator, IEmailVerificationService emailVerificationService, IAuthEventLogger authEventLogger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _sessionRepository = sessionRepository;
            _registerValidator = registerValidator;
            _emailVerificationService = emailVerificationService;
            _authEventLogger = authEventLogger;
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
            await _authEventLogger.LogAsync(AuthEventType.RegisterSuccess, request.IpAddress, request.UserAgent, user.Id, user.Email);

            return AuthResult.SuccessResult();
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            string normalizedEmail = request.Email.Trim().ToLowerInvariant();
            User? user = await _userRepository.GetByEmailAsync(normalizedEmail);

            const string genericError = "Email or password is incorrect.";

            if (user is null)
            {
                await _authEventLogger.LogAsync(AuthEventType.LoginFailedInvalidCredentials, request.IpAddress, request.UserAgent, email: normalizedEmail);
                return LoginResult.FailureResult(genericError);
            }

            if (user.LockedUntil is not null && user.LockedUntil > DateTime.UtcNow) 
            {
                await _authEventLogger.LogAsync(AuthEventType.LoginFailedAccountLocked, request.IpAddress, request.UserAgent, user.Id, user.Email);
                double remainingMinutes = Math.Ceiling((user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                return LoginResult.FailureResult($"Account blocked after several tries. Try again in {remainingMinutes} minutes.");
            }

            bool isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                bool wasJustLocked = await RegisterFailedAttemptAsync(user);

                await _authEventLogger.LogAsync(AuthEventType.LoginFailedInvalidCredentials, request.IpAddress, request.UserAgent, user.Id, user.Email);

                if (wasJustLocked)
                {
                    await _authEventLogger.LogAsync(AuthEventType.AccountLocked, request.IpAddress, request.UserAgent, user.Id, user.Email);
                }

                return LoginResult.FailureResult(genericError);
            }

            if(user.FailedLoginAttempts > 0 || user.LockedUntil is not null)
            {
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            if(!user.EmailVerified)
            {
                await _authEventLogger.LogAsync(AuthEventType.LoginFailedEmailNotVerified, request.IpAddress, request.UserAgent, user.Id, user.Email);
                return LoginResult.FailureResult("You should verify your email before login. Check your inbox.");
            }

            if (user.MfaEnabled)
            {
                return LoginResult.MfaRequired(user.Id);
            }

            await _authEventLogger.LogAsync(AuthEventType.LoginSuccess, request.IpAddress, request.UserAgent, user.Id, user.Email);

            return await CreateSessionAndCompleteLoginAsync(user, request.IpAddress, request.UserAgent);
        }

        private async Task<bool> RegisterFailedAttemptAsync(User user)
        {
            user.FailedLoginAttempts++;
            bool justLocked = false;

            if(user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                justLocked = true;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return justLocked;
        }

        public async Task<LoginResult> CompleteMfaLoginAsync(Guid userId, string ipAddress, string userAgent)
        {
            User? user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                return LoginResult.FailureResult("Login could not be completed.");
            }

            await _authEventLogger.LogAsync(AuthEventType.LoginSuccess, ipAddress, userAgent, user.Id, user.Email);

            return await CreateSessionAndCompleteLoginAsync(user, ipAddress, userAgent);
        }

        private async Task<LoginResult> CreateSessionAndCompleteLoginAsync(User user, string ipAddress, string userAgent)
        {
            Session session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };

            await _sessionRepository.AddAsync(session);
            return LoginResult.SuccessResult(user.Id, session.Id);
        }

        public async Task LogoutAsync(Guid sessionId, string ipAddress, string userAgent, Guid? userId)
        {
            await _sessionRepository.RevokeAsync(sessionId);
            await _authEventLogger.LogAsync(AuthEventType.Logout, ipAddress, userAgent, userId);
        }
    }
}
