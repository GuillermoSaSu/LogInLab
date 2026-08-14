using LogInLab.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Infrastructure.Security
{
    public class AesEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public AesEncryptionService(IConfiguration configuration)
        {
            string base64key = configuration["Encryption:Key"] 
                ?? throw new InvalidOperationException("Encryption key is not configured.");

            _key = Convert.FromBase64String(base64key);

            if(_key.Length != 32) // AES-256 requires a 32-byte key
            {
                throw new InvalidOperationException("Encryption key must be 32 bytes.");
            }
        }

        public string Encrypt(string plainText)
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];

            using AesGcm aesGcm = new AesGcm(_key, tag.Length);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Combine nonce, tag, and cipher text into a single byte array for storage/transmission
            byte[] result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);    
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            byte[] data = Convert.FromBase64String(cipherText);

            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;
            int cipherSize = data.Length - nonceSize - tagSize;

            byte[] nonce = data[..nonceSize];
            byte[] tag = data[nonceSize..(nonceSize + tagSize)];
            byte[] cipherBytes = data[(nonceSize + tagSize)..];

            byte[] plainBytes = new byte[cipherSize];

            using AesGcm aesGcm = new AesGcm(_key, tagSize);
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
