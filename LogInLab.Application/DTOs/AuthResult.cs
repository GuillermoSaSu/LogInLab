namespace LogInLab.Application.DTOs
{
    public class AuthResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }

        private AuthResult(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static AuthResult SuccessResult()
        {
            return new AuthResult(true);
        }

        public static AuthResult FailureResult(string errorMessage)
        {
            return new AuthResult(false, errorMessage);
        }
    }
}
