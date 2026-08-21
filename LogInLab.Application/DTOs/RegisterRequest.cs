namespace LogInLab.Application.DTOs
{
    public record RegisterRequest(string Email, string Password, string IpAddress, string UserAgent);
}
