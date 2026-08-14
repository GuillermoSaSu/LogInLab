namespace LogInLab.Application.Interfaces
{
    public interface ITotpService
    {
        string GenerateSecretKey();
        string GenerateQrCodeUri(string secretKey, string userEmail);
        bool ValidateCode(string secretKey, string code);
    }
}
