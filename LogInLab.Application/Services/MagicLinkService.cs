using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Application.Services
{
    public class MagicLinkService : IMagicLinkService
    {
        private const int TokenExpirationMinutes = 10;

        private readonly IUserRepository _userRepository;
        private readonly IMagicLinkToken _magicLinkToken;
        private readonly IEmailSender _emailSender;
        private readonly IAuthEventLogger _authEventLogger;
        private IConfiguration _configuration;

        private readonly IAuthService _authService;

        public MagicLinkService(IUserRepository userRepository, IMagicLinkToken magicLinkToken, IEmailSender emailSender, IAuthEventLogger authEventLogger, IConfiguration configuration, IAuthService authService)
        {
            _userRepository = userRepository;
            _magicLinkToken = magicLinkToken;
            _emailSender = emailSender;
            _authEventLogger = authEventLogger;
            _configuration = configuration;
            _authService = authService;
        }

        public async Task<AuthResult> RequestMagicLinkAsync(string email, string ipAddress, string userAgent)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            User? user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user is null || !user.EmailVerified)
            {
                return AuthResult.SuccessResult();
            }

            await _magicLinkToken.InvalidateAllForUserAsync(user.Id);

            string rawToken = GenerateRawToken();
            string tokenHash = HashToken(rawToken);

            MagicLinkToken token = new MagicLinkToken()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            };

            await _magicLinkToken.AddAsync(token);

            string appUrl = _configuration["AppUrl"];
            string magicLink = $"{appUrl}/Account/MagicLinkLogin?token={rawToken}";

            string htmlBody = $"""
                <h2>Your link to LogInLab<h2>
                <p>Click on the likn to login without password.</p>
                <p><a href="{magicLink}">Login</a></p>
                <p>This link expires in {TokenExpirationMinutes} minutes and can only be used once.</p>
                """;

            await _emailSender.SendAsync(user.Email, "Your link to LogInLab", htmlBody);

            await _authEventLogger.LogAsync(Domain.Enums.AuthEventType.MagicLinkRequested, ipAddress, userAgent, user.Id, user.Email);

            return AuthResult.SuccessResult();
        }

        public async Task<LoginResult> ConsumeMagicLinkAsync(string rawToken, string ipAddress, string userAgent)
        {
            string tokenHash = HashToken(rawToken);
            MagicLinkToken? token = await _magicLinkToken.GetByTokenHashAsync(tokenHash);

            bool isValid = token is not null
                && token.UsedAt is null
                && token.ExpiresAt > DateTime.UtcNow;

            if (!isValid)
            {
                return LoginResult.FailureResult("The access link is not valid or is expired");
            }

            User? user = await _userRepository.GetByIdAsync(token!.UserId);
            if (user is null)
            {
                return LoginResult.FailureResult("The access link is not valid or is expired");
            }

            await _magicLinkToken.MarkAsUsedAsync(token.Id);

            await _authEventLogger.LogAsync(Domain.Enums.AuthEventType.MagicLinkConsumed, ipAddress, userAgent, user.Id, user.Email);

            return await _authService.CompleteMagicLinkLoginAsync(user.Id, ipAddress, userAgent);
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
            byte[] bytes = Encoding.UTF8.GetBytes(rawToken);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}
