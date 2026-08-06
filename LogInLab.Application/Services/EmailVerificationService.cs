using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace LogInLab.Application.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private const int TokenExpirationMinutes = 30;
        private const int ResendCooldownMinutes = 2;

        private readonly IEmailVerificationTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public EmailVerificationService(
            IEmailVerificationTokenRepository tokenRepository,
            IUserRepository userRepository,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _configuration = configuration;
        }   

        public async Task SendVerificationEmailAsync(Guid userId, string userEmail)
        {
            string rawToken = GenerateRawToken();
            string tokenHash = HashToken(rawToken);

            EmailVerificationToken verificationToken = new EmailVerificationToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            };

            await _tokenRepository.AddAsync(verificationToken);

            string? appUrl = _configuration["AppUrl"];
            string verificationLink = $"{appUrl}/Account/VerifyEmail?token={rawToken}";

            string htmlBody = $"<p>Please verify your email by clicking the link below:</p><p><a href=\"{verificationLink}\">Verify Email</a></p>";

            await _emailSender.SendAsync(userEmail, "Email Verification in LogInLab", htmlBody);
        }

        public async Task<bool> VerifyAsync(string rawToken)
        {
            string tokenHash = HashToken(rawToken);
            EmailVerificationToken? token = await _tokenRepository.GetByTokenHashAsync(tokenHash);

            bool isValid = token is not null 
                && token.UsedAt is null
                && token.ExpiresAt > DateTime.UtcNow;

            if(!isValid)
            {
                return false;
            }

            User? user = await _userRepository.GetByIdAsync(token!.UserId);
            if(user is null)
            {
                return false;
            }

            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            await _tokenRepository.MarkAsUsedAsync(token.Id);

            return true;
        }

        private static string GenerateRawToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string HashToken(string rawToken)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }

        public async Task<AuthResult> ResendVerificationEmailAsync(string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            User? user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user is null || user.EmailVerified)
            {
                return AuthResult.SuccessResult();
            }

            EmailVerificationToken lastestToken = await _tokenRepository.GetLastestByUserIdAsync(user.Id);
            if(lastestToken is not null && lastestToken.CreatedAt.AddMinutes(ResendCooldownMinutes) > DateTime.UtcNow)
            {
                //Not enough time has passed since the last token was created, so we don't send a new email.
                return AuthResult.SuccessResult();
            }

            await SendVerificationEmailAsync(user.Id, user.Email);
            return AuthResult.SuccessResult();
        }
    }
}
