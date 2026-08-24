using LogInLab.Domain.Enums;
namespace LogInLab.Application.Interfaces
{
    public interface IAuthEventLogger
    {
        Task LogAsync(
            AuthEventType authEventType,
            string ipAddress,
            string userAgent,
            Guid? userId = null,
            string? email = null);
    }
}
