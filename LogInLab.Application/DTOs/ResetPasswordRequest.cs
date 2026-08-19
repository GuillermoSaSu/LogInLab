namespace LogInLab.Application.DTOs
{
    public record ResetPasswordRequest(string Token, string NewPassword);
}
