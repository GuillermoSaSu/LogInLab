namespace LogInLab.Application.DTOs
{
    public class MfaSetupResult
    {
        public string SecretKey { get; }
        public string QrCodeUri { get; }

        public MfaSetupResult(string secretKey, string qrCodeUri)
        {
            SecretKey = secretKey;
            QrCodeUri = qrCodeUri;
        }
    }
}
