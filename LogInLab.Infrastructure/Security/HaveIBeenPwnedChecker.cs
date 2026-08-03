using LogInLab.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Infrastructure.Security
{
    public class HaveIBeenPwnedChecker : IPasswordBreachChecker
    {
        private readonly HttpClient _httpClient;

        public HaveIBeenPwnedChecker(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsBreachedAsync(string password)
        {
            string sha1Hash = ComputeSha1Hash(password);
            string prefix = sha1Hash[..5];
            string suffix = sha1Hash[5..];

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"range/{prefix}");

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                string body = await response.Content.ReadAsStringAsync();
                string[] lines = body.Split('\n');

                foreach (string line in lines)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2 && parts[0].Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        private static string ComputeSha1Hash(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA1.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}
