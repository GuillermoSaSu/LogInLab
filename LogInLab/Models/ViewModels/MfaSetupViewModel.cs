namespace LogInLab.Models.ViewModels
{
    public class MfaSetupViewModel
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeBase64Image { get; set; } = string.Empty;
    }
}
