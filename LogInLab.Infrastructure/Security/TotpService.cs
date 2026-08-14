using LogInLab.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace LogInLab.Infrastructure.Security
{
    public class TotpService : ITotpService
    {
        private const int SecretLengthBytes = 20; // 160 bits for TOTP secret
        private const int CodeDigits = 6;
        private const int TimeStepSeconds = 30;
        private const string Issuer = "LogInLab";

        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public string GenerateQrCodeUri(string secretKey, string userEmail)
        {
            var label = Uri.EscapeDataString($"{Issuer}:{userEmail}");
            var issuer = Uri.EscapeDataString(Issuer);

            return $"otpauth://totp/{label}?secret={secretKey}&issuer={issuer}&digits={CodeDigits}&period={TimeStepSeconds}";
        }

        public string GenerateSecretKey()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(SecretLengthBytes);
            return Base32Encode(bytes);
        }

        public bool ValidateCode(string secretKey, string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits)
            {
                return false;
            }

            var secretBytes = Base32Decode(secretKey);
            var currentTimeStep = GetCurrentTimeStep();

            // We allow a window of ±1 time step to account for potential clock skew between the server and the client device.
            for (int offset = -1; offset <= 1; offset++)
            {
                var expectedCode = ComputeTotp(secretBytes, currentTimeStep + offset);
                if (CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedCode),
                        Encoding.UTF8.GetBytes(code)))
                {
                    return true;
                }
            }

            return false;
        }

        private static long GetCurrentTimeStep()
        {
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return unixTimestamp / TimeStepSeconds;
        }

        private static string ComputeTotp(byte[] secretBytes, long timeStep)
        {
            var timeStepBytes = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timeStepBytes); // RFC 6238 exige big-endian
            }

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(timeStepBytes);

            // "Dynamic truncation" según RFC 4226
            int offset = hash[^1] & 0x0F;
            int binaryCode =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);

            int code = binaryCode % (int)Math.Pow(10, CodeDigits);
            return code.ToString().PadLeft(CodeDigits, '0');
        }

        private static string Base32Encode(byte[] data)
        {
            var result = new StringBuilder();
            int bits = 0, value = 0;

            foreach (var b in data)
            {
                value = (value << 8) | b;
                bits += 8;

                while (bits >= 5)
                {
                    result.Append(alphabet[(value >> (bits - 5)) & 0x1F]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                result.Append(alphabet[(value << (5 - bits)) & 0x1F]);
            }

            return result.ToString();
        }


        private static byte[] Base32Decode(string base32)
        {
            base32 = base32.TrimEnd('=').ToUpperInvariant();

            List<byte> result = new List<byte>();
            int bits = 0;
            int value = 0;

            foreach (char c in base32)
            {
                int index = alphabet.IndexOf(c);
                if (index < 0)
                {
                    continue;
                }

                value = (value << 5) | index;
                bits += 5;

                if (bits >= 8)
                {
                    result.Add((byte)((value >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }
            return result.ToArray();
        }
    }
}
