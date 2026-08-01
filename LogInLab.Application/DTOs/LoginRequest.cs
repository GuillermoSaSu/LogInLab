namespace LogInLab.Application.DTOs
{
    public record LoginRequest(string Email, string Password, string IpAddress, string UserAgent);
}
