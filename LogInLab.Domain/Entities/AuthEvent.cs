using LogInLab.Domain.Enums;

namespace LogInLab.Domain.Entities
{
    public class AuthEvent
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public AuthEventType EventType { get; set; }
        public string? Email { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
