using Konscious.Security.Cryptography;
using LogInLab.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace LogInLab.Infrastructure.Security
{
    public class Argon2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 4; // Number of iterations for Argon2
        private const int MemorySize = 65536; // 64 MB
        private const int DegreeOfParallelism = 2; // Number of threads for Argon2

        public string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = ComputeHash(password, salt, Iterations, MemorySize, DegreeOfParallelism);

            return string.Join(
                '.',
                Iterations,
                MemorySize,
                DegreeOfParallelism,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        public bool verify(string password, string hashedPassword)
        {
            string[] parts = hashedPassword.Split('.');
            if(parts.Length != 5)
            {
                throw new FormatException("Invalid hashed password format.");
            }

            int iterations = int.Parse(parts[0]);
            int memorySize = int.Parse(parts[1]);
            int parallelism = int.Parse(parts[2]);
            byte[] salt = Convert.FromBase64String(parts[3]);
            byte[] expectedHash = Convert.FromBase64String(parts[4]);

            byte[] actualHash = ComputeHash(password, salt, iterations, memorySize, parallelism);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static byte[] ComputeHash(string password, byte[] salt, int iterations, int memorySize, int parallelism)
        {
            using Argon2id argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                Iterations = iterations,
                MemorySize = memorySize,
                DegreeOfParallelism = parallelism
            };
            return argon2.GetBytes(HashSize);
        }
    }
}
