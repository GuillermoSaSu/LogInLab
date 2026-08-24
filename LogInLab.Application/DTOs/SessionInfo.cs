namespace LogInLab.Application.DTOs
{
    public record SessionInfo
    (
        Guid Id,
        string IpAddress,
        string UserAgent,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        bool IsCurrentSession
    );
}
