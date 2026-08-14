namespace LogInLab.Application.DTOs
{
    public class MfaActivationResult
    {
        public bool Success { get; }
        public string? ErrorMessage { get; }
        public List<string> BackupCodes { get; }

        private MfaActivationResult(bool success, string? errorMessage, List<string> backupCodes)
        {
            Success = success;
            ErrorMessage = errorMessage;
            BackupCodes = backupCodes;
        }

        public static MfaActivationResult Ok(List<string> backupCodes) => new MfaActivationResult(true, null, backupCodes);
        public static MfaActivationResult Fail(string errorMessage) => new MfaActivationResult(false, errorMessage, new List<string>());

    }
}
