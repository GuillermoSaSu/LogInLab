using FluentValidation;
using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using LogInLab.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace LogInLab.Application.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private const int TokenExpirationMinutes = 15;

        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly IValidator<ResetPasswordRequest> _resetValidator;
        private readonly IConfiguration _configuration;
        private readonly IAuthEventLogger _authEventLogger;

        public PasswordResetService(
            IUserRepository userRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            ISessionRepository sessionRepository,
            IPasswordHasher passwordHasher,
            IEmailSender emailSender,
            IValidator<ResetPasswordRequest> resetValidator,
            IConfiguration configuration,
            IAuthEventLogger authEventLogger)
        {
            _userRepository = userRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _sessionRepository = sessionRepository;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
            _resetValidator = resetValidator;
            _configuration = configuration;
            _authEventLogger = authEventLogger;
        }

        public async Task<AuthResult> RequestPasswordResetAsync(string ipAddress, string userAgent, Guid userId, string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            User? user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if(user is null)
            {
                return AuthResult.SuccessResult();
            }

            await _passwordResetTokenRepository.InvalidateAllForUserAsync(user.Id);

            string rawToken = GenerateRawToken();
            string tokenHash = HashToken(rawToken);

            PasswordResetToken resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExipresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes)
            };

            await _passwordResetTokenRepository.AddAsync(resetToken);

            string? appUrl = _configuration["AppUrl"];
            string resetLink = $"{appUrl}/Account/ResetPassword?token={rawToken}";

            string htmlBody = $"<p>You requested a password reset. Click the link below to reset your password:</p><p><a href=\"{resetLink}\">Reset Password</a></p>";

            await _emailSender.SendAsync(user.Email, "Password Reset Request in LogInLab", htmlBody);
            await _authEventLogger.LogAsync(AuthEventType.PasswordResetCompleted, ipAddress, userAgent, userId, email);
            return AuthResult.SuccessResult();

        }

        public async Task<AuthResult> ResetPasswordAsync(string rawToken, string newPassword)
        {
            FluentValidation.Results.ValidationResult validationResult = await _resetValidator.ValidateAsync(new ResetPasswordRequest(rawToken, newPassword));

            if (!validationResult.IsValid)
            {
                string firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid password reset data.";
                return AuthResult.FailureResult(firstError);
            }

            string tokenHash = HashToken(rawToken);
            PasswordResetToken? token = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash);

            bool isValid = token is not null
                && token.UsedAt is null
                && token.ExipresAt > DateTime.UtcNow;

            if (!isValid)
            {
                return AuthResult.FailureResult("The reset link is invalid or has expired. ");
            }

            User? user = await _userRepository.GetByIdAsync(token!.UserId);
            if (user == null)
            {
                return AuthResult.FailureResult("The reset link is invalid or has expired.");
            }

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            await _passwordResetTokenRepository.MarkAsUsedAsync(token.Id);

            //Important, close all active sessions for the user after password reset to prevent unauthorized access.
            await _sessionRepository.RevokeAllForUserAsync(user.Id);

            return AuthResult.SuccessResult();
        }

        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string HashToken(string rawToken)
        {
            var bytes = Encoding.UTF8.GetBytes(rawToken);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}
