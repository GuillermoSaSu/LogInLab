using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace LogInLab.Application.Services
{
    public class MfaService : IMfaService
    {
        private const int BackupCodeCount = 10;

        private readonly IMfaSecretRepository _mfaSecretRepository;
        private readonly IBackupCodeRepository _backupCodeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITotpService _totpService;
        private readonly IEncryptionService _encryptionService; 

        public MfaService(
            IMfaSecretRepository mfaSecretRepository,
            IBackupCodeRepository backupCodeRepository,
            IUserRepository userRepository,
            ITotpService totpService,
            IEncryptionService encryptionService)
        {
            _mfaSecretRepository = mfaSecretRepository;
            _backupCodeRepository = backupCodeRepository;
            _userRepository = userRepository;
            _totpService = totpService;
            _encryptionService = encryptionService;
        }

        public async Task<MfaSetupResult> BeingSetupAsync(Guid userId, string userEmail)
        {
            MfaSecret? existingSecret = await _mfaSecretRepository.GetByUserIdAsync(userId);
            if (existingSecret is not null && !existingSecret.IsActive)
            {
                await _mfaSecretRepository.DeleteAsync(userId);
            }

            string secretKey = _totpService.GenerateSecretKey();
            string encrptedSecretKey = _encryptionService.Encrypt(secretKey);

            MfaSecret mfaSecret = new MfaSecret
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SecretKeyEncripted = encrptedSecretKey,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            await _mfaSecretRepository.AddAsync(mfaSecret);

            string qeCodeUri = _totpService.GenerateQrCodeUri(secretKey, userEmail);

            return new MfaSetupResult(secretKey, qeCodeUri);
        }

        public async Task<MfaActivationResult> ConfirmSetupAsync(Guid userId, string code)
        {
            MfaSecret? mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(userId);

            if (mfaSecret is null || mfaSecret.IsActive)
            {
                return MfaActivationResult.Fail("There is not pending MFA config to confirm.");
            }

            string decryptedSecretKey = _encryptionService.Decrypt(mfaSecret.SecretKeyEncripted);
            bool isValid = _totpService.ValidateCode(decryptedSecretKey, code);

            if (!isValid)
            {
                return MfaActivationResult.Fail("The provided code is invalid.");
            }

            mfaSecret.IsActive = true;
            mfaSecret.ActivatedAt = DateTime.UtcNow;
            await _mfaSecretRepository.UpdateAsync(mfaSecret);

            User? user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.MfaEnabled = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            List<string> backupCodes = await GenerateAndStoreBackupCodesAsync(userId);

            return MfaActivationResult.Ok(backupCodes);
        }

        private async Task<List<string>> GenerateAndStoreBackupCodesAsync(Guid userId)
        {
            //In case user repeat the activation.
            await _backupCodeRepository.DeleteAllForUserAsync(userId);

            List<string> rawBackupCodes = new List<string>();
            List<BackupCode> entities = new List<BackupCode>();

            for(int i = 0; i < BackupCodeCount; i++)
            {
                string rawCode = GenerateBackupCode();
                rawBackupCodes.Add(rawCode);

                entities.Add(new BackupCode
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CodeHash = HashBackupCode(rawCode),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _backupCodeRepository.AddRangeAsync(entities);

            return rawBackupCodes;
        }

        private string HashBackupCode(string code)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(code);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }

        private string GenerateBackupCode()
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            byte[] bytes = RandomNumberGenerator.GetBytes(8);

            char[] chars = new char[8];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            }

            return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}";
        }

        public async Task DisableAsync(Guid userId)
        {
            await _mfaSecretRepository.DeleteAsync(userId);
            await _backupCodeRepository.DeleteAllForUserAsync(userId);

            User? user =await _userRepository.GetByIdAsync(userId);
            if (user is not null)
            {
                user.MfaEnabled = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

        }

        public async Task<bool> ValidateCodeOrBackupAsync(Guid userId, string code)
        {
            MfaSecret? mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(userId);
            if (mfaSecret is null || !mfaSecret.IsActive)
            {
                return false;
            }

            string decryptedSecret = _encryptionService.Decrypt(mfaSecret.SecretKeyEncripted);

            if(_totpService.ValidateCode(decryptedSecret, code))
            {
                return true;
            }

            return await TryConsumeBackupCodeAsync(userId, code);
        }

        private async Task<bool> TryConsumeBackupCodeAsync(Guid userId, string code)
        {
            var normalizedCode = code.Replace("-", "").Trim().ToUpperInvariant();
            var codeHash = HashBackupCode(normalizedCode);

            var unusedCodes = await _backupCodeRepository.GetUnusedCodesByUserIdAsync(userId);
            var matchingCode = unusedCodes.FirstOrDefault(c =>
                CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(c.CodeHash),
                    Convert.FromHexString(codeHash)));

            if (matchingCode is null)
            {
                return false;
            }

            await _backupCodeRepository.MaskAsUsedAsync(matchingCode.Id);
            return true;
        }
    }
}
